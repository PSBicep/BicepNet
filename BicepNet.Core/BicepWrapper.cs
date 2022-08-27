using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using BicepNet.Core.Configuration;
using BicepNet.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core;

public static partial class BicepWrapper
{
    public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion;
    public static string OciCachePath { get; private set; }
    public static string TemplateSpecsCachePath { get; private set; }

    // Services shared between commands
    private static IApiVersionProvider apiVersionProvider;
    private static RootConfiguration configuration;
    private static IFileSystem fileSystem;
    private static IModuleDispatcher moduleDispatcher;
    private static IFileResolver fileResolver;
    private static IFeatureProvider featureProvider;
    private static IModuleRegistryProvider moduleRegistryProvider;
    private static IReadOnlyWorkspace workspace;
    private static INamespaceProvider namespaceProvider;
    private static BicepNetConfigurationManager configurationManager;
    private static IContainerRegistryClientFactory clientFactory;
    private static ILogger logger;

    public static void Initialize(ILogger bicepLogger)
    {
        logger = bicepLogger;

        apiVersionProvider = new ApiVersionProvider();
        workspace = new Workspace();
        fileSystem = new FileSystem();
        fileResolver = new FileResolver();
        featureProvider = new FeatureProvider();
        configurationManager = new BicepNetConfigurationManager(fileSystem);

        configuration = configurationManager.GetBuiltInConfiguration();

        namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);

        var tokenCredentialFactory = new TokenCredentialFactory();
        clientFactory = new ContainerRegistryClientFactory(tokenCredentialFactory);
        moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
            clientFactory,
            new TemplateSpecRepositoryFactory(tokenCredentialFactory),
            featureProvider);
        moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);

        OciCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.Oci);
        TemplateSpecsCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.TemplateSpecs);
    }

    public static BicepConfigInfo GetBicepConfigInfo(BicepConfigScope scope, string path)
    {
        switch (scope)
        {
            case BicepConfigScope.Default:
                return configurationManager.GetConfigurationInfo();
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
}
