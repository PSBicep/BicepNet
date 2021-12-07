using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Workspaces;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static void Restore(string inputFilePath)
        {
            var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);
            
            // Create separate configuration for the build, to account for custom rule changes
            var buildConfiguration = configurationManager.GetConfiguration(inputUri);

            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

            // Restore valid references, don't log any errors
            moduleDispatcher.RestoreModules(buildConfiguration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration));
        }
    }
}
