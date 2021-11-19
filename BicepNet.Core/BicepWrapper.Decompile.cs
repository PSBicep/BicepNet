using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Json;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Decompiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IDictionary<string,string> Decompile(string templatePath)
        {
            return Decompile(templatePath, null, null);
        }

        public static IDictionary<string, string> Decompile(string templatePath, string? outputDir, string? outputFile)
        {
            var inputPath = PathHelper.ResolvePath(templatePath);

            static string DefaultOutputPath(string path) => PathHelper.GetDefaultDecompileOutputPath(path);
            var outputPath = PathHelper.ResolveDefaultOutputPath(inputPath, outputDir, outputFile, DefaultOutputPath);

            Uri inputUri = PathHelper.FilePathToFileUrl(inputPath);
            Uri outputUri = PathHelper.FilePathToFileUrl(outputPath);

            var fileSystem = new FileSystem();

            var configuration = RootConfiguration.Bind(
                JsonElementFactory.CreateElement(
                    fileSystem.FileStream.Create(BuiltInConfigurationResourcePath, FileMode.Open, FileAccess.Read))
                );
            var featureProvider = new FeatureProvider();
            var fileResolver = new FileResolver();
            var tokenCredentialFactory = new TokenCredentialFactory();
            var moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);

            var template = new Dictionary<string,string>();
            var templateDecompiler = new TemplateDecompiler(new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider), fileResolver, moduleRegistryProvider, new ConfigurationManager(fileSystem));
            var decompilation = templateDecompiler.DecompileFileWithModules(inputUri, outputUri);
            
            foreach (var (fileUri, bicepOutput) in decompilation.filesToSave)
            {
                template.Add(fileUri.LocalPath,bicepOutput);
            }
            return template;
        }
    }
}
