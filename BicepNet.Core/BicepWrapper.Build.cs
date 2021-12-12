using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Workspaces;
using BicepNet.Core.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static BuildResult Build(string bicepPath, bool noRestore = false)
        {
            using var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };

            var inputUri = PathHelper.FilePathToFileUrl(bicepPath);

            // Create separate configuration for the build, to account for custom rule changes
            var buildConfiguration = configurationManager.GetConfiguration(inputUri);

            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

            // If user did not specify NoRestore, restore modules and rebuild
            if (!noRestore)
            {
                if (moduleDispatcher.RestoreModules(buildConfiguration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration)).GetAwaiter().GetResult())
                {
                    sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping, buildConfiguration);
                }
            }

            var compilation = new Compilation(namespaceProvider, sourceFileGrouping, buildConfiguration);
            var template = new List<string>();

            var (success, diagnosticResult) = LogDiagnostics(compilation);
            if (success)
            {
                var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), new EmitterSettings(featureProvider));
                emitter.Emit(writer);
                template.Add(sw.ToString());
            }

            return new BuildResult(
                template,
                diagnosticResult
            );
        }
    }
}
