using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Workspaces;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public static IList<string> Build(string bicepPath, bool noRestore = false) => joinableTaskFactory.Run(() => BuildAsync(bicepPath, noRestore));

    public static async Task<IList<string>> BuildAsync(string bicepPath, bool noRestore = false)
    {
        using var sw = new StringWriter();

        var inputPath = PathHelper.ResolvePath(bicepPath);
        var inputUri = PathHelper.FilePathToFileUrl(inputPath);

        var compilation = await CompileAsync(inputUri, noRestore);

        bool success = LogDiagnostics(compilation);
        var template = new List<string>();
        if (success)
        {
            var fileKind = compilation.SourceFileGrouping.EntryPoint.FileKind;
            var semanticModel = compilation.GetEntrypointSemanticModel();
            switch (fileKind)
            {
                case BicepSourceFileKind.BicepFile:
                    {
                        var sourceFileToTrack = semanticModel.Features.SourceMappingEnabled ? semanticModel.SourceFile : default;
                        using var writer = new SourceAwareJsonTextWriter(semanticModel.FileResolver, sw, sourceFileToTrack)
                        {
                            Formatting = Formatting.Indented
                        };

                        var emitter = new TemplateEmitter(semanticModel);

                        var result = emitter.Emit(writer);
                        if(result.Status != EmitStatus.Succeeded)
                        {
                            throw new Exception($"Failed to emit template with error: ${result.Status}");
                        }
                        break;
                    }

                case BicepSourceFileKind.ParamsFile:
                    {
                        using var writer = new JsonTextWriter(sw)
                        {
                            Formatting = Formatting.Indented
                        };

                        var result = new ParametersEmitter(semanticModel).EmitParamsFile(writer);
                        if (result.Status != EmitStatus.Succeeded)
                        {
                            throw new Exception($"Failed to emit params files with error: ${result.Status}");
                        }
                        break;
                    }

                default:
                    throw new NotImplementedException($"Unexpected file kind '{fileKind}'");
            }
            template.Add(sw.ToString());
        }
        return template;
    }

    internal static async Task<Compilation> CompileAsync(Uri inputUri, bool skipRestore)
    {
        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri);
        if (!skipRestore)
        {
            // module references in the file may be malformed
            // however we still want to surface as many errors as we can for the module refs that are valid
            // so we will try to restore modules with valid refs and skip everything else
            // (the diagnostics will be collected during compilation)
            if (await moduleDispatcher.RestoreModules(moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore())))
            {
                // modules had to be restored - recompile
                sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping);
            }
        }
        var compilation = new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, apiVersionProviderFactory, bicepAnalyzer);
        
        LogDiagnostics(compilation);

        return compilation;
    }

    internal static async Task<Compilation> CompileAsync(string template, bool skipRestore)
    {
        BicepFile virtualBicepFile = SourceFileFactory.CreateBicepFile(new Uri($"inmemory://generated.bicep"), template);
        var workspace = new Workspace();
        workspace.UpsertSourceFiles(virtualBicepFile.AsEnumerable());

        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, virtualBicepFile.FileUri);
        if (!skipRestore)
        {
            // module references in the file may be malformed
            // however we still want to surface as many errors as we can for the module refs that are valid
            // so we will try to restore modules with valid refs and skip everything else
            // (the diagnostics will be collected during compilation)
            if (await moduleDispatcher.RestoreModules(moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore())))
            {
                // modules had to be restored - recompile
                sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping);
            }
        }
        var compilation = new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, apiVersionProviderFactory, bicepAnalyzer);
        LogDiagnostics(compilation);

        return compilation;
    }
}