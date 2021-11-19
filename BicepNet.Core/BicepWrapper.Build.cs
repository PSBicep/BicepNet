using Bicep.Core.Configuration;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Json;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static BuildResult Build(string bicepPath, bool noRestore = false)
        {
            using var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw)
            {
                Formatting = Formatting.Indented
            };

            var fileSystem = new FileSystem();

            var configuration = RootConfiguration.Bind(
                JsonElementFactory.CreateElement(
                    fileSystem.FileStream.Create(BuiltInConfigurationResourcePath, FileMode.Open, FileAccess.Read))
                );

            var inputUri = PathHelper.FilePathToFileUrl(bicepPath);
            var fileResolver = new FileResolver();
            var tokenCredentialFactory = new TokenCredentialFactory();
            var featureProvider = new FeatureProvider();
            var moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);
            var dispatcher = new ModuleDispatcher(moduleRegistryProvider);
            var workspace = new Workspace();
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, dispatcher, workspace, inputUri, configuration);

            var moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);

            // If user did not specify NoRestore, restore modules and rebuild
            if (!noRestore)
            {
                if (moduleDispatcher.RestoreModules(configuration, moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, configuration)).GetAwaiter().GetResult())
                {
                    sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping, configuration);
                }
            }

            var compilation = new Compilation(new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider), sourceFileGrouping, configuration);
            var template = new List<string>();

            var (success, dignosticResult) = LogDiagnostics(compilation);
            if (success)
            {
                var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), new EmitterSettings(featureProvider));
                emitter.Emit(writer);
                template.Add(sw.ToString());
            }

            return new BuildResult(
                template,
                dignosticResult
            );
        }

        private static (bool success, ICollection<DiagnosticEntry> dignosticResult) LogDiagnostics(Compilation compilation)
        {
            var diagnosticLogger = new DiagnosticLogger();
            foreach (var (bicepFile, diagnostics) in compilation.GetAllDiagnosticsByBicepFile())
            {
                foreach (var diagnostic in diagnostics)
                {
                    diagnosticLogger.LogDiagnostics(bicepFile.FileUri, diagnostic, bicepFile.LineStarts);
                }
            }
            return (diagnosticLogger.success, diagnosticLogger.diagnosticResult);
        }
    }
}
