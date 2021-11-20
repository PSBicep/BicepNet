using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Json;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static string BuiltInConfigurationResourcePath { get; } = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\bicepconfig.json";
        public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion;

        // Services shared between commands
        private static readonly RootConfiguration configuration;
        private static readonly IFileSystem fileSystem;
        private static readonly IModuleDispatcher moduleDispatcher;
        private static readonly IFileResolver fileResolver;
        private static readonly IFeatureProvider featureProvider;
        private static readonly IModuleRegistryProvider moduleRegistryProvider;
        private static readonly IReadOnlyWorkspace workspace;
        private static readonly INamespaceProvider namespaceProvider;

        static BicepWrapper()
        {
            workspace = new Workspace();
            fileSystem = new FileSystem();
            fileResolver = new FileResolver();
            featureProvider = new FeatureProvider();
            
            configuration = RootConfiguration.Bind(
                JsonElementFactory.CreateElement(
                    fileSystem.FileStream.Create(BuiltInConfigurationResourcePath, FileMode.Open, FileAccess.Read))
                );

            namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);
            
            var tokenCredentialFactory = new TokenCredentialFactory();
            moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);
            moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);
        }
    }
}
