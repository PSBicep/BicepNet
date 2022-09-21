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
    private static int WarningCount = 0;
    private static int ErrorCount = 0;

    public static void Restore(string inputFilePath) => joinableTaskFactory.Run(() => RestoreAsync(inputFilePath));

    public static async Task RestoreAsync(string inputFilePath)
    {
        logger?.LogInformation("Restoring external modules to local cache for file {inputFilePath}", inputFilePath);

        var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

        // Create separate configuration for the build, to account for custom rule changes
        var buildConfiguration = configurationManager.GetConfiguration(inputUri);

        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

        // Restore valid references, don't log any errors
        var moduleReferences = moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore(), buildConfiguration);
        await moduleDispatcher.RestoreModules(buildConfiguration, moduleReferences);

        foreach (var module in moduleReferences)
        {
            var status = moduleDispatcher.GetModuleRestoreStatus(module, configuration, out _);
            switch (status)
            {
                case ModuleRestoreStatus.Failed:
                    logger?.LogError($"Failed to restore {module.FullyQualifiedReference}");
                    ErrorCount++;
                    break;
                case ModuleRestoreStatus.Succeeded:
                    logger?.LogInformation($"Successfully restored {module.FullyQualifiedReference}");
                    break;
            }
        }

        // update the errors based on if the restore was successful
        sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping, configuration);

        LogDiagnostics(GetModuleRestoreDiagnosticsByBicepFile(sourceFileGrouping, sourceFileGrouping.GetModulesToRestore().ToImmutableHashSet()));
        
        if (ErrorCount == 0)
        {
            if (moduleReferences.Any())
            {
                logger?.LogInformation($"Successfully restored modules in {inputFilePath}");
            }
            else
            {
                logger?.LogInformation($"No new modules to restore in {inputFilePath}");
            }
        }
        else
        {
            logger?.LogError($"Failed to restore {ErrorCount} out of {moduleReferences.Count()} new modules in {inputFilePath}");
        }
    }

    private static ImmutableDictionary<BicepFile, ImmutableArray<IDiagnostic>> GetModuleRestoreDiagnosticsByBicepFile(SourceFileGrouping sourceFileGrouping, ImmutableHashSet<ModuleDeclarationSyntax> originalModulesToRestore)
    {
        static IEnumerable<IDiagnostic> GetModuleDiagnosticsPerFile(SourceFileGrouping grouping, BicepFile bicepFile, ImmutableHashSet<ModuleDeclarationSyntax> originalModulesToRestore)
        {
            foreach (var module in bicepFile.ProgramSyntax.Declarations.OfType<ModuleDeclarationSyntax>())
            {
                if (!originalModulesToRestore.Contains(module))
                {
                    continue;
                }

                if (grouping.TryGetErrorDiagnostic(module) is {} errorBuilder)
                {
                    var error = errorBuilder(DiagnosticBuilder.ForPosition(module.Path));
                    yield return error;
                }
            }
        }

        return sourceFileGrouping.SourceFiles
            .OfType<BicepFile>()
            .ToImmutableDictionary(bicepFile => bicepFile, bicepFile => GetModuleDiagnosticsPerFile(sourceFileGrouping, bicepFile, originalModulesToRestore).ToImmutableArray());
    }
}
