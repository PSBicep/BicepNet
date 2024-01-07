using Bicep.Core;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Navigation;
using Bicep.Core.Registry;
using Bicep.Core.Syntax;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public void Restore(string inputFilePath, bool forceModulesRestore = false) => joinableTaskFactory.Run(() => RestoreAsync(inputFilePath, forceModulesRestore));

    public async Task RestoreAsync(string inputFilePath, bool forceModulesRestore = false)
    {
        logger?.LogInformation("Restoring external modules to local cache for file {inputFilePath}", inputFilePath);
        var inputPath = PathHelper.ResolvePath(inputFilePath);
        var inputUri = PathHelper.FilePathToFileUrl(inputPath);

        var bicepCompiler = new BicepCompiler(featureProviderFactory, environment, namespaceProvider, configurationManager, bicepAnalyzer, fileResolver, moduleDispatcher);
        var compilation = await bicepCompiler.CreateCompilation(inputUri, workspace, true, forceModulesRestore);

        var originalModulesToRestore = compilation.SourceFileGrouping.GetArtifactsToRestore().ToImmutableHashSet();

        // RestoreModules() does a distinct but we'll do it also to prevent duplicates in processing and logging
        var modulesToRestoreReferences = moduleDispatcher.GetValidModuleReferences(originalModulesToRestore)
            .Distinct()
            .OrderBy(key => key.FullyQualifiedReference);

        // restore is supposed to only restore the module references that are syntactically valid
        await moduleDispatcher.RestoreModules(modulesToRestoreReferences, forceModulesRestore);

        // update the errors based on restore status
        var sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(featureProviderFactory, this.moduleDispatcher, this.workspace, compilation.SourceFileGrouping);

        LogDiagnostics(compilation);

        if (modulesToRestoreReferences.Any())
        {
            logger?.LogInformation("Successfully restored modules in {inputFilePath}", inputFilePath);
        }
        else
        {
            logger?.LogInformation("No new modules to restore in {inputFilePath}", inputFilePath);
        }
    }

    private static ImmutableDictionary<BicepSourceFile, ImmutableArray<IDiagnostic>> GetModuleRestoreDiagnosticsByBicepFile(SourceFileGrouping sourceFileGrouping, ImmutableHashSet<ArtifactResolutionInfo> originalModulesToRestore, bool forceModulesRestore)
    {
        static IDiagnostic? DiagnosticForModule(SourceFileGrouping grouping, IArtifactReferenceSyntax moduleDeclaration)
            => grouping.TryGetSourceFile(moduleDeclaration).IsSuccess(out _, out var errorBuilder) ? null : errorBuilder(DiagnosticBuilder.ForPosition(moduleDeclaration.SourceSyntax));

        static IEnumerable<(BicepFile, IDiagnostic)> GetDiagnosticsForModulesToRestore(SourceFileGrouping grouping, ImmutableHashSet<ArtifactResolutionInfo> originalArtifactsToRestore)
        {
            var originalModulesToRestore = originalArtifactsToRestore.OfType<ArtifactResolutionInfo>();
            foreach (var (module, sourceFile) in originalModulesToRestore)
            {
                if (sourceFile is BicepFile bicepFile &&
                    DiagnosticForModule(grouping, module) is { } diagnostic)
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
            .ToImmutableDictionary(g => (BicepSourceFile)g.Key, g => g.ToImmutableArray());
    }
}
