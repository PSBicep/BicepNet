using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Syntax;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public static void Restore(string inputFilePath, bool forceModulesRestore = false) => joinableTaskFactory.Run(() => RestoreAsync(inputFilePath, forceModulesRestore));

    public static async Task RestoreAsync(string inputFilePath, bool forceModulesRestore = false)
    {
        ErrorCount = 0;
        logger?.LogInformation("Restoring external modules to local cache for file {inputFilePath}", inputFilePath);
        var inputPath = PathHelper.ResolvePath(inputFilePath);
        var inputUri = PathHelper.FilePathToFileUrl(inputPath);

        // Create separate configuration for the build, to account for custom rule changes
        var buildConfiguration = configurationManager.GetConfiguration(inputUri);

        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, forceModulesRestore);
        var originalModulesToRestore = sourceFileGrouping.GetModulesToRestore().ToImmutableHashSet();
        // Restore valid references, don't log any errors
        // RestoreModules() does a distinct but we'll do it also to prevent duplicates in processing and logging
        var modulesToRestoreReferences = moduleDispatcher.GetValidModuleReferences(originalModulesToRestore)
            .Distinct()
            .OrderBy(key => key.FullyQualifiedReference);

        // restore is supposed to only restore the module references that are syntactically valid
        await moduleDispatcher.RestoreModules(modulesToRestoreReferences, forceModulesRestore);

        // update the errors based on if the restore was successful
        sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping);

        LogDiagnostics(GetModuleRestoreDiagnosticsByBicepFile(sourceFileGrouping, originalModulesToRestore, forceModulesRestore));
        
        if (ErrorCount == 0)
        {
            if (modulesToRestoreReferences.Any())
            {
                logger?.LogInformation("Successfully restored modules in {inputFilePath}", inputFilePath);
            }
            else
            {
                logger?.LogInformation("No new modules to restore in {inputFilePath}", inputFilePath);
            }
        }
        else
        {
            logger?.LogError("Failed to restore {ErrorCount} out of {TotalCount} new modules in {inputFilePath}", ErrorCount, modulesToRestoreReferences.Count(), inputFilePath);
        }
    }

    private static ImmutableDictionary<BicepFile, ImmutableArray<IDiagnostic>> GetModuleRestoreDiagnosticsByBicepFile(SourceFileGrouping sourceFileGrouping, ImmutableHashSet<ModuleSourceResolutionInfo> originalModulesToRestore, bool forceModulesRestore)
    {
        static IDiagnostic? DiagnosticForModule(SourceFileGrouping grouping, ModuleDeclarationSyntax module)
                => grouping.TryGetErrorDiagnostic(module) is { } errorBuilder ? errorBuilder(DiagnosticBuilder.ForPosition(module.Path)) : null;

        static IEnumerable<(BicepFile, IDiagnostic)> GetDiagnosticsForModulesToRestore(SourceFileGrouping grouping, ImmutableHashSet<ModuleSourceResolutionInfo> originalModulesToRestore)
        {
            foreach (var (module, sourceFile) in originalModulesToRestore)
            {
                if (sourceFile is BicepFile bicepFile && DiagnosticForModule(grouping, module) is { } diagnostic)
                {
                    yield return (bicepFile, diagnostic);
                }
            }
        }

        static IEnumerable<(BicepFile, IDiagnostic)> GetDiagnosticsForAllModules(SourceFileGrouping grouping)
        {
            foreach (var bicepFile in grouping.SourceFiles.OfType<BicepFile>())
            {
                foreach (var module in bicepFile.ProgramSyntax.Declarations.OfType<ModuleDeclarationSyntax>())
                {
                    if (DiagnosticForModule(grouping, module) is { } diagnostic)
                    {
                        yield return (bicepFile, diagnostic);
                    }
                }
            }
        }

        var diagnosticsByFile = forceModulesRestore ? GetDiagnosticsForAllModules(sourceFileGrouping) : GetDiagnosticsForModulesToRestore(sourceFileGrouping, originalModulesToRestore);

        return diagnosticsByFile
            .ToLookup(t => t.Item1, t => t.Item2)
            .ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray());
    }
}
