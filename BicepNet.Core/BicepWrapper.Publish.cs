using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Exceptions;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Semantics;
using Bicep.Core.Workspaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public void Publish(string inputFilePath, string targetModuleReference) => 
        joinableTaskFactory.Run(() => PublishAsync(inputFilePath, targetModuleReference));

    public async Task PublishAsync(string inputFilePath, string targetModuleReference)
    {
        var inputPath = PathHelper.ResolvePath(inputFilePath);
        var inputUri = PathHelper.FilePathToFileUrl(inputPath);
        var moduleReference = ValidateReference(targetModuleReference, inputUri);

        if (PathHelper.HasArmTemplateLikeExtension(inputUri))
        {
            // Publishing an ARM template file.
            using var armTemplateStream = fileSystem.FileStream.New(inputPath, FileMode.Open, FileAccess.Read);
            await PublishModuleAsync(moduleReference, armTemplateStream);
            return;
        }

        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri);
        var compilation = new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, apiVersionProviderFactory, bicepAnalyzer);
        if (LogDiagnostics(compilation))
        {
            var stream = new MemoryStream();
            new TemplateEmitter(compilation.GetEntrypointSemanticModel()).Emit(stream);

            stream.Position = 0;
            await PublishModuleAsync(moduleReference, stream);
            return;
        }
    }

    private ModuleReference ValidateReference(string targetModuleReference, Uri targetModuleUri)
    {
        if (!moduleDispatcher.TryGetModuleReference(targetModuleReference, targetModuleUri, out var moduleReference, out var failureBuilder))
        {
            // TODO: We should probably clean up the dispatcher contract so this sort of thing isn't necessary (unless we change how target module is set in this command)
            var message = failureBuilder(DiagnosticBuilder.ForDocumentStart()).Message;

            throw new BicepException(message);
        }

        if (!moduleDispatcher.GetRegistryCapabilities(moduleReference).HasFlag(RegistryCapabilities.Publish))
        {
            throw new BicepException($"The specified module target \"{targetModuleReference}\" is not supported.");
        }

        return moduleReference;
    }

    private async Task PublishModuleAsync(ModuleReference target, Stream stream)
    {
        try
        {
            await moduleDispatcher.PublishModule(target, stream, null);
        }
        catch (ExternalModuleException exception)
        {
            throw new BicepException($"Unable to publish module \"{target.FullyQualifiedReference}\": {exception.Message}");
        }
    }
}
