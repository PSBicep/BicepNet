using Bicep.Cli;
using Bicep.Cli.Logging;
using Bicep.Cli.Services;
using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Bicep.Decompiler;
using Bicep.LanguageServer.Providers;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using BicepNet.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using IOFileSystem = System.IO.Abstractions.FileSystem;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public string BicepVersion { get; }
    public string OciCachePath { get; }
    public string TemplateSpecsCachePath { get; }

    private ILogger logger;
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
    //private readonly IConfigurationManager configurationManager;
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
        //services = ConfigureServices()
        services = new ServiceCollection()
            .AddSingleton<INamespaceProvider, DefaultNamespaceProvider>()
            .AddSingleton<IAzResourceTypeLoader, AzResourceTypeLoader>()
            .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
            .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
            .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
            .AddSingleton<IModuleRegistryProvider, DefaultModuleRegistryProvider>()
            .AddSingleton<ITokenCredentialFactory, TokenCredentialFactory>()
            .AddSingleton<IFileResolver, FileResolver>()
            .AddSingleton<IFileSystem, IOFileSystem>()
            .AddSingleton<IConfigurationManager, ConfigurationManager>()
            .AddSingleton<IApiVersionProviderFactory, ApiVersionProviderFactory>()
            .AddSingleton<IBicepAnalyzer, LinterAnalyzer>()
            .AddSingleton<IFeatureProviderFactory, FeatureProviderFactory>()
            .AddSingleton<ILinterRulesProvider, LinterRulesProvider>()
            
            
            .AddSingleton<BicepCompiler>()
            .AddSingleton<BicepDecompiler>()

            .AddSingleton<AzureResourceProvider>()
            .AddSingleton<IAzResourceProvider>(s => s.GetRequiredService<AzureResourceProvider>())
            .AddSingleton<Workspace>()
            
            .AddSingleton(bicepLogger)
            .AddSingleton<IDiagnosticLogger, BicepDiagnosticLogger>()
            .AddSingleton<CompilationService>()

            .AddSingleton<BicepNetTokenCredentialFactory>()
            .AddSingleton<BicepNetConfigurationManager>()
            .Replace(ServiceDescriptor.Singleton<ITokenCredentialFactory>(s => s.GetRequiredService<BicepNetTokenCredentialFactory>()))
            .BuildServiceProvider();

        joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
        diagnosticLogger = (BicepDiagnosticLogger)services.GetRequiredService<IDiagnosticLogger>();
        namespaceProvider = services.GetRequiredService<INamespaceProvider>();
        azResourceTypeLoader = services.GetRequiredService<IAzResourceTypeLoader>();
        clientFactory = services.GetRequiredService<IContainerRegistryClientFactory>();
        moduleDispatcher = services.GetRequiredService<IModuleDispatcher>();
        moduleRegistryProvider = services.GetRequiredService<IModuleRegistryProvider>();
        tokenCredentialFactory = services.GetRequiredService<BicepNetTokenCredentialFactory>();
        tokenCredentialFactory.logger = bicepLogger;
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

    public static void Initialize(ILogger bicepLogger)
    {
        
        //logger = bicepLogger;
        //diagnosticLogger = new BicepDiagnosticLogger(bicepLogger);

        //tokenCredentialFactory.logger = bicepLogger;
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
            logger?.LogWarning("No access token currently stored!");
            return null;
        }

        var tokenValue = token.Value;
        return new BicepAccessToken(tokenValue.Token, tokenValue.ExpiresOn);
    }

    public BicepConfigInfo GetBicepConfigInfo(BicepConfigScope scope, string path)
    {
        switch (scope)
        {
            case BicepConfigScope.Default:
                return BicepNetConfigurationManager.GetConfigurationInfo();
            // Merged and Local uses the same logic
            case BicepConfigScope.Merged:
            case BicepConfigScope.Local:
                if (path == null)
                {
                    throw new ArgumentException("Path must be provided for this Scope!");
                }
                return configurationManager.GetConfigurationInfo(scope, PathHelper.FilePathToFileUrl(path));
            default:
                throw new ArgumentException("BicepConfigMode not valid!");
        }
    }

    private bool LogDiagnostics(Compilation compilation)
    {
        if (compilation is null)
        {
            throw new Exception("Compilation is null. A compilation must exist before logging the diagnostics.");
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
    //private static string GetDiagnosticsOutput(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
    //{
    //    var localPath = fileUri.LocalPath;
    //    var position = TextCoordinateConverter.GetPosition(lineStarts, diagnostic.Span.Position);
    //    var line = position.line;
    //    var character = position.character;
    //    var level = diagnostic.Level;
    //    var code = diagnostic.Code;
    //    var message = diagnostic.Message;

    //    var codeDescription = diagnostic.Uri is null ? string.Empty : $" [{diagnostic.Uri.AbsoluteUri}]";

    //    return $"{localPath}({line},{character}) : {level} {code}: {message}{codeDescription}";
    //}
    //private static void LogDiagnostic(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
    //{
    //    var message = GetDiagnosticsOutput(fileUri, diagnostic, lineStarts);

    //    switch (diagnostic.Level)
    //    {
    //        case DiagnosticLevel.Off:
    //            break;
    //        case DiagnosticLevel.Info:
    //            logger?.LogInformation("{message}", message);
    //            break;
    //        case DiagnosticLevel.Warning:
    //            logger?.LogWarning("{message}", message);
    //            break;
    //        case DiagnosticLevel.Error:
    //            logger?.LogError("{message}", message);
    //            break;
    //        default:
    //            break;
    //    }

    //    // Increment counters
    //    if (diagnostic.Level == DiagnosticLevel.Warning) { WarningCount++; }
    //    if (diagnostic.Level == DiagnosticLevel.Error) { ErrorCount++; }
    //}
}
