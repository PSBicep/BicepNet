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
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, configuration);

            // Restore valid references, don't log any errors
            moduleDispatcher.RestoreModules(configuration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, configuration));
        }
    }
}
