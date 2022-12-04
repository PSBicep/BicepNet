using Bicep.Core.FileSystem;
using Bicep.Core.Workspaces;
using Bicep.Decompiler;
using System.CodeDom.Compiler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bicep.Core.Extensions;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public static IDictionary<string, string> Decompile(string templatePath, string? outputDir = null, string? outputFile = null) =>
        joinableTaskFactory.Run(() => DecompileAsync(templatePath, outputDir, outputFile));

    public static async Task<IDictionary<string, string>> DecompileAsync(string templatePath, string? outputDir = null, string? outputFile = null)
    {
        var inputPath = PathHelper.ResolvePath(templatePath);
        var inputUri = PathHelper.FilePathToFileUrl(inputPath);

        static string DefaultOutputPath(string path) => PathHelper.GetDefaultDecompileOutputPath(path);
        var outputPath = PathHelper.ResolveDefaultOutputPath(inputPath, outputDir, outputFile, DefaultOutputPath);
        var outputUri = PathHelper.FilePathToFileUrl(outputPath);


        var template = new Dictionary<string, string>();
        var decompilation = await decompiler.Decompile(inputUri, outputUri);

        foreach (var (fileUri, bicepOutput) in decompilation.FilesToSave)
        {
            template.Add(fileUri.LocalPath, bicepOutput);
        }

        return template;
    }
}
