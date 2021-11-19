using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Exceptions;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Json;
using Bicep.Core.Parsing;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static void Publish(string inputFilePath, string targetModuleReference, bool noRestore = true)
        {
            var fileResolver = new FileResolver();
            var tokenCredentialFactory = new TokenCredentialFactory();
            var featureProvider = new FeatureProvider();
            var moduleRegistryProvider = new DefaultModuleRegistryProvider(fileResolver,
                new ContainerRegistryClientFactory(tokenCredentialFactory),
                new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                featureProvider);

            var fileSystem = new FileSystem();

            var moduleDispatcher = new ModuleDispatcher(moduleRegistryProvider);
            var moduleReference = moduleDispatcher.TryGetModuleReference(targetModuleReference,
                new ConfigurationManager(fileSystem).GetBuiltInConfiguration(),
                out var failureBuilder);

            if (moduleReference is null)
            {
                failureBuilder = failureBuilder ?? throw new InvalidOperationException($"{nameof(moduleDispatcher.TryGetModuleReference)} did not provide an error.");

                // From Bicep project:
                // TODO: We should probably clean up the dispatcher contract so this sort of thing isn't necessary (unless we change how target module is set in this command)
                var message = failureBuilder(new DiagnosticBuilder.DiagnosticBuilderInternal(new TextSpan(0, 0))).Message;

                // Changed from BicepException
                throw new Exception(message);
            }

            if (!moduleDispatcher.GetRegistryCapabilities(moduleReference).HasFlag(RegistryCapabilities.Publish))
            {
                throw new BicepException($"The specified module target \"{targetModuleReference}\" is not supported.");
            }

            var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

            var configuration = RootConfiguration.Bind(
                JsonElementFactory.CreateElement(
                    fileSystem.FileStream.Create(BuiltInConfigurationResourcePath, FileMode.Open, FileAccess.Read))
                );

            var dispatcher = new ModuleDispatcher(moduleRegistryProvider);
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, dispatcher, new Workspace(), inputUri, configuration);
            var compilation = new Compilation(new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider), sourceFileGrouping, configuration);
            var template = new List<string>();

            var (success, dignosticResult) = LogDiagnostics(compilation);
            if (!success)
            {
                throw new Exception("The template was not valid, please fix the template before publishing!");
            }

            var stream = new MemoryStream();
            new TemplateEmitter(compilation.GetEntrypointSemanticModel(), new EmitterSettings(featureProvider)).Emit(stream);

            stream.Position = 0;
            moduleDispatcher.PublishModule(compilation.Configuration, moduleReference, stream);
        }
    }
}
