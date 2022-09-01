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
using Microsoft.VisualStudio.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core;

public static partial class BicepWrapper
{
    public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion ?? "dev";
    public static string OciCachePath { get; private set; }
    public static string TemplateSpecsCachePath { get; private set; }

    // Services shared between commands

    private static readonly JoinableTaskFactory joinableTaskFactory;
    private static readonly ITokenCredentialFactory tokenCredentialFactory;
    private static readonly IApiVersionProvider apiVersionProvider;
    private static readonly RootConfiguration configuration;
    private static readonly IFileSystem fileSystem;
    private static readonly IModuleDispatcher moduleDispatcher;
    private static readonly IFileResolver fileResolver;
    private static readonly IFeatureProvider featureProvider;
    private static readonly IModuleRegistryProvider moduleRegistryProvider;
    private static readonly IReadOnlyWorkspace workspace;
    private static readonly INamespaceProvider namespaceProvider;
    private static readonly BicepNetConfigurationManager configurationManager;
    private static readonly IContainerRegistryClientFactory clientFactory;
    private static ILogger? logger;

    static BicepWrapper()
    {
        joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
        tokenCredentialFactory = new TokenCredentialFactory();
        apiVersionProvider = new ApiVersionProvider();
        workspace = new Workspace();
        fileSystem = new FileSystem();
        fileResolver = new FileResolver();
        featureProvider = new FeatureProvider();
        configurationManager = new BicepNetConfigurationManager(fileSystem);

        configuration = configurationManager.GetBuiltInConfiguration();

        namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);

        clientFactory = new ContainerRegistryClientFactory(tokenCredentialFactory);
        moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
            clientFactory,
            new TemplateSpecRepositoryFactory(tokenCredentialFactory),
            featureProvider);
        moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);

        OciCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.Oci);
        TemplateSpecsCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.TemplateSpecs);
    }

    public static void Initialize(ILogger bicepLogger)
    {
        logger = bicepLogger;
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
}
