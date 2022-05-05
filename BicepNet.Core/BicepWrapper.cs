using Azure.Core;
using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using BicepNet.Core.Authentication;
using BicepNet.Core.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core
{
    public static partial class BicepWrapper
    {
        public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion;
        public static string OciCachePath { get; private set; }
        public static string TemplateSpecsCachePath { get; private set; }

        // Services shared between commands
        private static RootConfiguration configuration;
        private static IFileSystem fileSystem;
        private static IModuleDispatcher moduleDispatcher;
        private static IFileResolver fileResolver;
        private static IFeatureProvider featureProvider;
        private static IModuleRegistryProvider moduleRegistryProvider;
        private static IReadOnlyWorkspace workspace;
        private static INamespaceProvider namespaceProvider;
        private static IConfigurationManager configurationManager;
        private static ITokenCredentialFactory tokenCredentialFactory;
        private static ILogger logger;

        internal static TokenCredential ExternalCredential;

        public static void Initialize(ILogger bicepLogger, string token)
        {
            logger = bicepLogger;
            // Reset credential between commands
            ExternalCredential = null;

            if (!string.IsNullOrEmpty(token))
            {
                logger.LogInformation("Token provided as authentication...");
                ExternalCredential = new ExternalTokenCredential(token, DateTimeOffset.Now.AddDays(1));
            }

            workspace = new Workspace();
            fileSystem = new FileSystem();
            fileResolver = new FileResolver();
            featureProvider = new FeatureProvider();
            configurationManager = new BicepNetConfigurationManager(fileSystem);

            configuration = configurationManager.GetBuiltInConfiguration();

            namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);

            tokenCredentialFactory = new BicepNetTokenCredentialFactory();
            moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);
            moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);

            OciCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.Oci);
            TemplateSpecsCachePath = Path.Combine(featureProvider.CacheRootDirectory, ModuleReferenceSchemes.TemplateSpecs);
        }
    }
}
