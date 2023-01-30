using Bicep.Cli;
using Bicep.Cli.Arguments;
using Bicep.Cli.Commands;
using Bicep.Cli.Services;
using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
namespace BicepNet.Core;

public partial class BicepWrapper
{
    public IList<string> Build(string bicepPath, bool noRestore = false) => joinableTaskFactory.Run(() => BuildAsync(bicepPath, noRestore));

    public async Task<IList<string>> BuildAsync(string bicepPath, bool noRestore = false)
    {
        var inputPath = PathHelper.ResolvePath(bicepPath);
        var features = featureProviderFactory.GetFeatureProvider(PathHelper.FilePathToFileUrl(inputPath));
        var emitterSettings = new EmitterSettings(features);

        if (emitterSettings.EnableSymbolicNames)
        {
            logger?.LogWarning("Symbolic name support in ARM is experimental, and should be enabled for testing purposes only.Do not enable this setting for any production usage, or you may be unexpectedly broken at any time!");
        }

        if (features.ResourceTypedParamsAndOutputsEnabled)
        {
            logger?.LogWarning("Resource-typed parameters and outputs in ARM are experimental, and should be enabled for testing purposes only. Do not enable this setting for any production usage, or you may be unexpectedly broken at any time!");
        }
        var template = new List<string>();
        var compilation = await compilationService.CompileAsync(inputPath, noRestore);
        if (diagnosticLogger is not null && diagnosticLogger.ErrorCount > 0)
        {
            throw new Exception($"Failed to compile template: {inputPath}");
        }

        var stream = new MemoryStream();
        var fileKind = compilation.SourceFileGrouping.EntryPoint.FileKind;

        EmitResult emitresult = fileKind switch
        {
            BicepSourceFileKind.BicepFile => new TemplateEmitter(compilation.GetEntrypointSemanticModel()).Emit(stream),
            BicepSourceFileKind.ParamsFile => new ParametersEmitter(compilation.GetEntrypointSemanticModel()).Emit(stream),
            _ => throw new NotImplementedException($"Unexpected file kind '{fileKind}'"),
        };

        if (emitresult.Status != EmitStatus.Succeeded)
        {
            throw new Exception($"Failed to emit bicep with error: ${emitresult.Status}");
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync();
        template.Add(result);

        //var compilation = await CreateCompilationAsync(inputUri, noRestore, workspace);
        //var compileWorkspace = new Workspace();
        //var compilation = await compiler.CreateCompilation(inputUri, noRestore, compileWorkspace);
        //bool success = LogDiagnostics(compilation);

        //if (diagnosticLogger is null || diagnosticLogger.ErrorCount < 1)
        //{
        //    var fileKind = compilation.SourceFileGrouping.EntryPoint.FileKind;
        //    var semanticModel = compilation.GetEntrypointSemanticModel();
        //    switch (fileKind)
        //    {
        //        case BicepSourceFileKind.BicepFile:
        //            {
        //                var sourceFileToTrack = semanticModel.Features.SourceMappingEnabled ? semanticModel.SourceFile : default;
        //                using var writer = new SourceAwareJsonTextWriter(semanticModel.FileResolver, sw, sourceFileToTrack)
        //                {
        //                    Formatting = Formatting.Indented
        //                };

        //                var emitter = new TemplateEmitter(semanticModel);

        //                var result = emitter.Emit(writer);
        //                //if(result.Status != EmitStatus.Succeeded)
        //                //{
        //                //    throw new Exception($"Failed to emit template with error: ${result.Status}");
        //                //}
        //                break;
        //            }

        //        case BicepSourceFileKind.ParamsFile:
        //            {
        //                using var writer = new JsonTextWriter(sw)
        //                {
        //                    Formatting = Formatting.Indented
        //                };

        //                var result = new ParametersEmitter(semanticModel).EmitParamsFile(writer);
        //                if (result.Status != EmitStatus.Succeeded)
        //                {
        //                    throw new Exception($"Failed to emit params files with error: ${result.Status}");
        //                }
        //                break;
        //            }

        //        default:
        //            throw new NotImplementedException($"Unexpected file kind '{fileKind}'");
        //    }
        //    template.Add(sw.ToString());
        //}
        return template;
    }

    //public async Task<Compilation> CreateCompilationAsync(Uri bicepUri, bool skipRestore = false, IReadOnlyWorkspace? workspace = null)
    //{
    //    workspace ??= new Workspace();
    //    var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, bicepUri, false);

    //    if (!skipRestore)
    //    {
    //        // module references in the file may be malformed
    //        // however we still want to surface as many errors as we can for the module refs that are valid
    //        // so we will try to restore modules with valid refs and skip everything else
    //        // (the diagnostics will be collected during compilation)
    //        if (await moduleDispatcher.RestoreModules(moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore())))
    //        {
    //            // modules had to be restored - recompile
    //            sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping);
    //        }
    //    }
    //    return new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, apiVersionProviderFactory, bicepAnalyzer);
    //}

}