using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Syntax;
using Bicep.Core.Text;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

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
            moduleDispatcher.RestoreModules(buildConfiguration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration)).GetAwaiter().GetResult();

            // update the errors based on if the restore was successful
            sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping, configuration);

            LogDiagnostics(GetModuleRestoreDiagnosticsByBicepFile(sourceFileGrouping, sourceFileGrouping.ModulesToRestore));
        }

        private static IReadOnlyDictionary<BicepFile, IEnumerable<IDiagnostic>> GetModuleRestoreDiagnosticsByBicepFile(SourceFileGrouping sourceFileGrouping, ImmutableHashSet<ModuleDeclarationSyntax> originalModulesToRestore)
        {
            static IEnumerable<IDiagnostic> GetModuleDiagnosticsPerFile(SourceFileGrouping grouping, BicepFile bicepFile, ImmutableHashSet<ModuleDeclarationSyntax> originalModulesToRestore)
            {
                foreach (var module in bicepFile.ProgramSyntax.Declarations.OfType<ModuleDeclarationSyntax>())
                {
                    if (!originalModulesToRestore.Contains(module))
                    {
                        continue;
                    }

                    if (grouping.TryLookUpModuleErrorDiagnostic(module, out var error))
                    {
                        yield return error;
                    }
                    else
                    {
                        logger.LogTrace($"Successfully restored {SyntaxHelper.TryGetModulePath(module, out _)}");
                    }
                }
            }

            return sourceFileGrouping.SourceFiles
                .OfType<BicepFile>()
                .ToDictionary(bicepFile => bicepFile, bicepFile => GetModuleDiagnosticsPerFile(sourceFileGrouping, bicepFile, originalModulesToRestore));
        }

        private static void LogDiagnostics(IReadOnlyDictionary<BicepFile, IEnumerable<IDiagnostic>> diagnosticsByBicepFile)
        {
            foreach (var (bicepFile, diagnostics) in diagnosticsByBicepFile)
            {
                foreach (var diagnostic in diagnostics)
                {
                    LogDiagnostic(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
                }
            }
        }

        public static void LogDiagnostic(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
        {
            (int line, int character) = TextCoordinateConverter.GetPosition(lineStarts, diagnostic.Span.Position);

            // build a a code description link if the Uri is assigned
            var codeDescription = diagnostic.Uri == null ? string.Empty : $" [{diagnostic.Uri.AbsoluteUri}]";

            var message = $"{fileUri.LocalPath}({line + 1},{character + 1}) : {diagnostic.Level} {diagnostic.Code}: {diagnostic.Message}{codeDescription}";

            switch (diagnostic.Level)
            {
                case DiagnosticLevel.Off:
                    break;
                case DiagnosticLevel.Info:
                    logger.LogInformation(message);
                    break;
                case DiagnosticLevel.Warning:
                    logger.LogWarning(message);
                    break;
                case DiagnosticLevel.Error:
                    logger.LogError(message);
                    break;
                default:
                    break;
            }

            //// Increment counters
            //if (diagnostic.Level == Bicep.Core.Diagnostics.DiagnosticLevel.Warning) { this.WarningCount++; }
            //if (diagnostic.Level == Bicep.Core.Diagnostics.DiagnosticLevel.Error) { this.ErrorCount++; }
        }
    }
}
