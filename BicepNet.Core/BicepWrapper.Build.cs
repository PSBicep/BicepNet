using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public IList<string> Build(string bicepPath, bool noRestore = false) => joinableTaskFactory.Run(() => BuildAsync(bicepPath, noRestore));

    public async Task<IList<string>> BuildAsync(string bicepPath, bool noRestore = false)
    {
        var inputPath = PathHelper.ResolvePath(bicepPath);
        var features = featureProviderFactory.GetFeatureProvider(PathHelper.FilePathToFileUrl(inputPath));
        var emitterSettings = new EmitterSettings(features, BicepSourceFileKind.BicepFile);

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
            throw new InvalidOperationException($"Failed to compile template: {inputPath}");
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
            throw new InvalidOperationException($"Failed to emit bicep with error: ${emitresult.Status}");
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync();
        template.Add(result);

        return template;
    }
}