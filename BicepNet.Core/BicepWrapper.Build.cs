using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Semantics;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static BuildResult Build(string bicepPath)
        {
            using var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };

            var inputUri = PathHelper.FilePathToFileUrl(bicepPath);
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(new FileResolver(), new Workspace(), inputUri);
            var compilation = new Compilation(AzResourceTypeProvider.CreateWithAzTypes(), sourceFileGrouping);
            var template = new List<string>();

            var (success, dignosticResult) = LogDiagnostics(compilation);
            if (success)
            {
                var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), BicepVersion);
                emitter.Emit(writer);
                template.Add(sw.ToString());
            }

            return new BuildResult(
                template,
                dignosticResult
            );

        }

        private static (bool success, ICollection<DiagnosticEntry> dignosticResult) LogDiagnostics(Compilation compilation)
        {
            var diagnosticLogger = new DiagnosticLogger();
            foreach (var (bicepFile, diagnostics) in compilation.GetAllDiagnosticsByBicepFile())
            {
                foreach (var diagnostic in diagnostics)
                {
                    diagnosticLogger.LogDiagnostics(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
                }
            }
            return (diagnosticLogger.success, diagnosticLogger.diagnosticResult);
        }
    }
}
