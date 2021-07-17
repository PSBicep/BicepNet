using Bicep.Core.FileSystem;
using Bicep.Core.TypeSystem.Az;
using Bicep.Decompiler;
using System;
using System.Collections.Generic;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IDictionary<string,string> Decompile(string templatePath)
        {
            return Decompile(templatePath, null, null);
        }

        public static IDictionary<string,string> Decompile(string templatePath, string? outputDir, string? outputFile)
        {
            var inputPath = PathHelper.ResolvePath(templatePath);

            static string DefaultOutputPath(string path) => PathHelper.GetDefaultDecompileOutputPath(path);
            var outputPath = PathHelper.ResolveDefaultOutputPath(inputPath, outputDir, outputFile, DefaultOutputPath);
            
            Uri inputUri = PathHelper.FilePathToFileUrl(inputPath);
            Uri outputUri = PathHelper.FilePathToFileUrl(outputPath);

            var template = new Dictionary<string,string>();
            var decompilation = TemplateDecompiler.DecompileFileWithModules(AzResourceTypeProvider.CreateWithAzTypes(), new FileResolver(), inputUri, outputUri);
            
            foreach (var (fileUri, bicepOutput) in decompilation.filesToSave)
            {
                template.Add(fileUri.LocalPath,bicepOutput);
            }
            return template;
        }
    }
}
