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
using System.Linq;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        private static int WarningCount = 0;
        private static int ErrorCount = 0;

        public static void Restore(string inputFilePath)
        {
            logger.LogInformation($"Restoring external modules to local cache for file {inputFilePath}");

            var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

            // Create separate configuration for the build, to account for custom rule changes
            var buildConfiguration = configurationManager.GetConfiguration(inputUri);

            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

            // Restore valid references, don't log any errors
            var moduleReferences = moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration);
            moduleDispatcher.RestoreModules(buildConfiguration, moduleReferences).GetAwaiter().GetResult();

            foreach (var module in moduleReferences)
            {
                var status = moduleDispatcher.GetModuleRestoreStatus(module, configuration, out _);
                switch (status)
                {
                    case ModuleRestoreStatus.Failed:
                        logger.LogError($"Failed to restore {module.FullyQualifiedReference}");
                        ErrorCount++;
                        break;
                    case ModuleRestoreStatus.Succeeded:
                        logger.LogInformation($"Successfully restored {module.FullyQualifiedReference}");
                        break;
                }
            }

            // update the errors based on if the restore was successful
            sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping, configuration);

            LogDiagnostics(GetModuleRestoreDiagnosticsByBicepFile(sourceFileGrouping, sourceFileGrouping.ModulesToRestore));
            
            if (ErrorCount == 0)
            {
                if (moduleReferences.Count() > 0)
                {
                    logger.LogInformation($"Successfully restored modules in {inputFilePath}");
                }
                else
                {
                    logger.LogInformation($"No new modules to restore in {inputFilePath}");
                }
            }
            else
            {
                logger.LogError($"Failed to restore {ErrorCount} out of {moduleReferences.Count()} new modules in {inputFilePath}");
            }
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

            // Increment counters
            if (diagnostic.Level == DiagnosticLevel.Warning) { WarningCount++; }
            if (diagnostic.Level == DiagnosticLevel.Error) { ErrorCount++; }
        }
    }
}
