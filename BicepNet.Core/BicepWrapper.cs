using Bicep.Cli.Logging;
using Bicep.Cli.Services;
using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Bicep.Decompiler;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using BicepNet.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public string BicepVersion { get; }
    public string OciCachePath { get; }
    public string TemplateSpecsCachePath { get; }

    private readonly ILogger logger;
    private BicepDiagnosticLogger diagnosticLogger;
    private readonly IServiceProvider services;

    // Services shared between commands
    private readonly JoinableTaskFactory joinableTaskFactory;
    private readonly INamespaceProvider namespaceProvider;
    private readonly IContainerRegistryClientFactory clientFactory;
    private readonly IModuleDispatcher moduleDispatcher;
    private readonly IModuleRegistryProvider moduleRegistryProvider;
    private readonly BicepNetTokenCredentialFactory tokenCredentialFactory;
    private readonly IAzResourceTypeLoader azResourceTypeLoader;
    private readonly IFileResolver fileResolver;
    private readonly IFileSystem fileSystem;
    private readonly BicepNetConfigurationManager configurationManager;
    private readonly IApiVersionProviderFactory apiVersionProviderFactory;
    private readonly IBicepAnalyzer bicepAnalyzer;
    private readonly IFeatureProviderFactory featureProviderFactory;
    private readonly BicepCompiler compiler;
    private readonly CompilationService compilationService;
    private readonly BicepDecompiler decompiler;
    private readonly Workspace workspace;
    private readonly RootConfiguration configuration;
    private readonly AzureResourceProvider azResourceProvider;

    public BicepWrapper(ILogger bicepLogger)
    {
        BicepDeploymentsInterop.Initialize();
        services = new ServiceCollection()
            .AddBicepNet(bicepLogger)
            .BuildServiceProvider();

        joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
        logger = services.GetRequiredService<ILogger>();
        diagnosticLogger = (BicepDiagnosticLogger)services.GetRequiredService<IDiagnosticLogger>();
        namespaceProvider = services.GetRequiredService<INamespaceProvider>();
        azResourceTypeLoader = services.GetRequiredService<IAzResourceTypeLoader>();
        clientFactory = services.GetRequiredService<IContainerRegistryClientFactory>();
        moduleDispatcher = services.GetRequiredService<IModuleDispatcher>();
        moduleRegistryProvider = services.GetRequiredService<IModuleRegistryProvider>();
        tokenCredentialFactory = services.GetRequiredService<BicepNetTokenCredentialFactory>();
        tokenCredentialFactory.logger = services.GetRequiredService<ILogger>();
        fileResolver = services.GetRequiredService<IFileResolver>();
        fileSystem = services.GetRequiredService<IFileSystem>();
        configurationManager = services.GetRequiredService<BicepNetConfigurationManager>();
        apiVersionProviderFactory = services.GetRequiredService<IApiVersionProviderFactory>();
        bicepAnalyzer = services.GetRequiredService<IBicepAnalyzer>();
        featureProviderFactory = services.GetRequiredService<IFeatureProviderFactory>();
        compiler = services.GetRequiredService<BicepCompiler>();
        compilationService = services.GetRequiredService<CompilationService>();

        decompiler = services.GetRequiredService<BicepDecompiler>();

        workspace = services.GetRequiredService<Workspace>();
        configuration = configurationManager.GetConfiguration(new Uri("inmemory://main.bicep"));
        azResourceProvider = services.GetRequiredService<AzureResourceProvider>();

        BicepVersion = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion ?? "dev";
        OciCachePath = Path.Combine(services.GetRequiredService<IFeatureProviderFactory>().GetFeatureProvider(new Uri("inmemory:///main.bicp")).CacheRootDirectory, ModuleReferenceSchemes.Oci);
        TemplateSpecsCachePath = Path.Combine(services.GetRequiredService<IFeatureProviderFactory>().GetFeatureProvider(new Uri("inmemory:///main.bicp")).CacheRootDirectory, ModuleReferenceSchemes.TemplateSpecs);
    }

    public void ClearAuthentication() => tokenCredentialFactory.Clear();
    public void SetAuthentication(string? token = null, string? tenantId = null) =>
        tokenCredentialFactory.SetToken(configuration.Cloud.ActiveDirectoryAuthorityUri, token, tenantId);

    public BicepAccessToken? GetAccessToken()
    {
        // Gets the token using the same request context as when connecting
        var token = tokenCredentialFactory.Credential?.GetToken(tokenCredentialFactory.TokenRequestContext, System.Threading.CancellationToken.None);

        if (!token.HasValue)
        {
            logger.LogWarning("No access token currently stored!");
            return null;
        }

        var tokenValue = token.Value;
        return new BicepAccessToken(tokenValue.Token, tokenValue.ExpiresOn);
    }

    public BicepConfigInfo GetBicepConfigInfo(BicepConfigScope scope, string path) =>
        configurationManager.GetConfigurationInfo(scope, PathHelper.FilePathToFileUrl(path ?? ""));

    private bool LogDiagnostics(Compilation compilation)
    {
        if (compilation is null)
        {
            throw new InvalidOperationException("Compilation is null. A compilation must exist before logging the diagnostics.");
        }

        return LogDiagnostics(compilation.GetAllDiagnosticsByBicepFile());
    }

    private bool LogDiagnostics(ImmutableDictionary<BicepSourceFile,ImmutableArray<IDiagnostic>> diagnosticsByBicepFile)
    {
        bool success = true;
        foreach (var (bicepFile, diagnostics) in diagnosticsByBicepFile)
        {
            foreach (var diagnostic in diagnostics)
            {
                success = diagnostic.Level != DiagnosticLevel.Error;
                diagnosticLogger?.LogDiagnostic(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
            }
        }
        return success;
    }
}
