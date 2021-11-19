using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Workspaces;
using System.IO.Abstractions;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static void Restore(string inputFilePath)
        {
            var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

            var configuration = new ConfigurationManager(new FileSystem()).GetConfiguration(inputUri);

            var fileResolver = new FileResolver();
            var tokenCredentialFactory = new TokenCredentialFactory();
            var featureProvider = new FeatureProvider();
            var moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);

            var moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);
            
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, new Workspace(), inputUri, configuration);

            // Restore valid references, don't log any errors
            moduleDispatcher.RestoreModules(configuration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, configuration));
        }
    }
}
