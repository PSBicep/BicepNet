using Bicep.Core.FileSystem;
using Bicep.Core.TypeSystem.Az;
using Bicep.Decompiler;
using System.Collections.Generic;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IDictionary<string,string> Decompile(string templatePath)
        {
            var template = new Dictionary<string,string>();
            var (_, filesToSave) = TemplateDecompiler.DecompileFileWithModules(AzResourceTypeProvider.CreateWithAzTypes(), new FileResolver(), PathHelper.FilePathToFileUrl(templatePath));
            foreach (var (fileUri, bicepOutput) in filesToSave)
            {
                template.Add(fileUri.LocalPath,bicepOutput);
            }
            return template;
        }
    }
}
