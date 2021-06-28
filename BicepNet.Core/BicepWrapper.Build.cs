using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.Text;
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
            var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };

            var syntaxTreeGrouping = SyntaxTreeGroupingBuilder.Build(new FileResolver(), new Workspace(), PathHelper.FilePathToFileUrl(bicepPath));
            var compilation = new Compilation(AzResourceTypeProvider.CreateWithAzTypes(), syntaxTreeGrouping);

            var (success, dignosticResult) = LogDiagnosticsAndCheckSuccess(compilation);
            var template = new List<string>();
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

        private static (bool success, ICollection<DiagnosticEntry> dignosticResult) LogDiagnosticsAndCheckSuccess(Compilation compilation)
        {
            var success = true;
            var diagnosticResult = new List<DiagnosticEntry>();

            foreach (var (syntaxTree, diagnostics) in compilation.GetAllDiagnosticsBySyntaxTree())
            {
                foreach (var diagnostic in diagnostics)
                {

                    diagnosticResult.Add(
                    new DiagnosticEntry(
                        syntaxTree.FileUri.LocalPath,
                        TextCoordinateConverter.GetPosition(syntaxTree.LineStarts, diagnostic.Span.Position),
                        (DiagnosticLevel)diagnostic.Level,
                        diagnostic.Code,
                        diagnostic.Message
                    )
                    );
                    success &= diagnostic.Level != Bicep.Core.Diagnostics.DiagnosticLevel.Error;
                }
            }

            return (success, diagnosticResult);
        }

    }
}
