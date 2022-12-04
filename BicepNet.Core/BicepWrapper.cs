using Bicep.Core.Analyzers.Interfaces;
using Azure.Core;
using Azure.Identity;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using BicepNet.Core.Authentication;
using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using BicepNet.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Bicep.Core;
using Bicep.Decompiler;
using System.CodeDom.Compiler;

namespace BicepNet.Core;

public static partial class BicepWrapper
{
    public static string BicepVersion { get; }
    public static string OciCachePath { get; }
    public static string TemplateSpecsCachePath { get; }

    private static int WarningCount = 0;
    private static int ErrorCount = 0;

    // Services shared between commands
    private static readonly JoinableTaskFactory joinableTaskFactory;
    private static readonly IAzResourceTypeLoader azResourceTypeLoader;
    private static readonly INamespaceProvider namespaceProvider;
    private static readonly BicepNetTokenCredentialFactory tokenCredentialFactory;
    private static readonly Workspace workspace;
    private static readonly IFileSystem fileSystem;
    private static readonly IFileResolver fileResolver;
    private static readonly BicepNetConfigurationManager configurationManager;
    private static readonly RootConfiguration configuration;
    private static readonly IFeatureProviderFactory featureProviderFactory;
    private static readonly IApiVersionProviderFactory apiVersionProviderFactory;
    private static readonly IBicepAnalyzer bicepAnalyzer;
    private static readonly IContainerRegistryClientFactory clientFactory;
    private static readonly IModuleRegistryProvider moduleRegistryProvider;
    private static readonly IModuleDispatcher moduleDispatcher;
    private static readonly AzureResourceProvider azResourceProvider;
    private static readonly BicepCompiler compiler;
    private static readonly BicepDecompiler decompiler;
    private static ILogger? logger;

    static BicepWrapper()
    {
        joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
        azResourceTypeLoader = new AzResourceTypeLoader();
        namespaceProvider = new DefaultNamespaceProvider(azResourceTypeLoader);
        // Create a custom TokenCredentialFactory to allow for token input
        tokenCredentialFactory = new BicepNetTokenCredentialFactory();
        workspace = new Workspace();
        fileSystem = new FileSystem();
        fileResolver = new FileResolver(fileSystem);
        configurationManager = new BicepNetConfigurationManager(fileSystem);
        configuration = BicepNetConfigurationManager.GetBuiltInConfiguration();
        featureProviderFactory = new FeatureProviderFactory(configurationManager);
        apiVersionProviderFactory = new ApiVersionProviderFactory(featureProviderFactory, namespaceProvider);
        bicepAnalyzer = new LinterAnalyzer();
        clientFactory = new ContainerRegistryClientFactory(tokenCredentialFactory);
        moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
            clientFactory,
            new TemplateSpecRepositoryFactory(tokenCredentialFactory),
            featureProviderFactory,
            configurationManager);
        moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider, configurationManager);
        azResourceProvider = new AzureResourceProvider(tokenCredentialFactory, fileResolver, moduleDispatcher, configurationManager, featureProviderFactory, namespaceProvider, apiVersionProviderFactory, bicepAnalyzer);
        compiler = new BicepCompiler(featureProviderFactory, namespaceProvider, configurationManager, apiVersionProviderFactory, bicepAnalyzer, fileResolver, moduleDispatcher);
        decompiler = new BicepDecompiler(compiler, fileResolver);

        BicepVersion = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion ?? "dev";
        OciCachePath = Path.Combine(featureProviderFactory.GetFeatureProvider(new Uri("inmemory:///main.bicp")).CacheRootDirectory, ModuleReferenceSchemes.Oci);
        TemplateSpecsCachePath = Path.Combine(featureProviderFactory.GetFeatureProvider(new Uri("inmemory:///main.bicp")).CacheRootDirectory, ModuleReferenceSchemes.TemplateSpecs);
    }

    public static void Initialize(ILogger bicepLogger)
    {
        logger = bicepLogger;
        tokenCredentialFactory.logger = bicepLogger;
    }

    public static void ClearAuthentication() => tokenCredentialFactory.Clear();
    public static void SetAuthentication(string? token = null, string? tenantId = null) =>
        tokenCredentialFactory.SetToken(configuration.Cloud.ActiveDirectoryAuthorityUri, token, tenantId);

    public static BicepAccessToken? GetAccessToken()
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

    public static BicepConfigInfo GetBicepConfigInfo(BicepConfigScope scope, string path)
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

    private static bool LogDiagnostics(Compilation compilation)
    {
        if (compilation is null)
        {
            throw new Exception("Compilation is null. A compilation must exist before logging the diagnostics.");
        }

        return LogDiagnostics(compilation.GetAllDiagnosticsByBicepFile());
    }
    private static bool LogDiagnostics(ImmutableDictionary<BicepSourceFile,ImmutableArray<IDiagnostic>> diagnosticsByBicepFile)
    {
        bool success = true;
        foreach (var (bicepFile, diagnostics) in diagnosticsByBicepFile)
        {
            foreach (var diagnostic in diagnostics)
            {
                success = diagnostic.Level != DiagnosticLevel.Error;
                LogDiagnostic(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
            }
        }
        return success;
    }
    private static string GetDiagnosticsOutput(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
    {
        var localPath = fileUri.LocalPath;
        var position = TextCoordinateConverter.GetPosition(lineStarts, diagnostic.Span.Position);
        var line = position.line;
        var character = position.character;
        var level = diagnostic.Level;
        var code = diagnostic.Code;
        var message = diagnostic.Message;

        var codeDescription = diagnostic.Uri is null ? string.Empty : $" [{diagnostic.Uri.AbsoluteUri}]";

        return $"{localPath}({line},{character}) : {level} {code}: {message}{codeDescription}";
    }
    private static void LogDiagnostic(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
    {
        var message = GetDiagnosticsOutput(fileUri, diagnostic, lineStarts);

        switch (diagnostic.Level)
        {
            case DiagnosticLevel.Off:
                break;
            case DiagnosticLevel.Info:
                logger?.LogInformation("{message}", message);
                break;
            case DiagnosticLevel.Warning:
                logger?.LogWarning("{message}", message);
                break;
            case DiagnosticLevel.Error:
                logger?.LogError("{message}", message);
                break;
            default:
                break;
        }

        // Increment counters
        if (diagnostic.Level == DiagnosticLevel.Warning) { WarningCount++; }
        if (diagnostic.Level == DiagnosticLevel.Error) { ErrorCount++; }
    }
}
