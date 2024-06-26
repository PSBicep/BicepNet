using Bicep.Core;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Exceptions;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.SourceCode;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public void Publish(string inputFilePath, string targetModuleReference, string? documentationUri, bool overwriteIfExists = false) =>
        joinableTaskFactory.Run(() => PublishAsync(inputFilePath, targetModuleReference, documentationUri, overwriteIfExists));

    public async Task PublishAsync(string inputFilePath, string targetModuleReference, string? documentationUri, bool overwriteIfExists = false)
    {
        var inputPath = PathHelper.ResolvePath(inputFilePath);
        var inputUri = PathHelper.FilePathToFileUrl(inputPath);
        ArtifactReference? moduleReference = ValidateReference(targetModuleReference, inputUri);

        if (PathHelper.HasArmTemplateLikeExtension(inputUri))
        {
            // Publishing an ARM template file.
            using var armTemplateStream = fileSystem.FileStream.New(inputPath, FileMode.Open, FileAccess.Read);
            await this.PublishModuleAsync(moduleReference, armTemplateStream, null, documentationUri, overwriteIfExists);
            return;
        }

        var bicepCompiler = new BicepCompiler(featureProviderFactory, environment, namespaceProvider, configurationManager, bicepAnalyzer, fileResolver, moduleDispatcher);
        var compilation = await bicepCompiler.CreateCompilation(inputUri, workspace);
        
        using var sourcesStream = SourceArchive.PackSourcesIntoStream(moduleDispatcher, compilation.SourceFileGrouping, null);

        if (!LogDiagnostics(compilation).HasErrors)
        {
            var stream = new MemoryStream();
            new TemplateEmitter(compilation.GetEntrypointSemanticModel()).Emit(stream);

            stream.Position = 0;
            await PublishModuleAsync(moduleReference, stream, sourcesStream, documentationUri, overwriteIfExists);
        }
    }

    // copied from PublishCommand.cs
    private ArtifactReference ValidateReference(string targetModuleReference, Uri targetModuleUri)
    {
        if (!this.moduleDispatcher.TryGetModuleReference(targetModuleReference, targetModuleUri).IsSuccess(out var moduleReference, out var failureBuilder))
        {
            // TODO: We should probably clean up the dispatcher contract so this sort of thing isn't necessary (unless we change how target module is set in this command)
            var message = failureBuilder(DiagnosticBuilder.ForDocumentStart()).Message;

            throw new BicepException(message);
        }

        if (!this.moduleDispatcher.GetRegistryCapabilities(moduleReference).HasFlag(RegistryCapabilities.Publish))
        {
            throw new BicepException($"The specified module target \"{targetModuleReference}\" is not supported.");
        }

        return moduleReference;
    }

    // copied from PublishCommand.cs
    private async Task PublishModuleAsync(ArtifactReference target, Stream compiledArmTemplate, Stream? bicepSources, string? documentationUri, bool overwriteIfExists)
    {
        try
        {
            // If we don't want to overwrite, ensure module doesn't exist
            if (!overwriteIfExists && await this.moduleDispatcher.CheckModuleExists(target))
            {
                throw new BicepException($"The module \"{target.FullyQualifiedReference}\" already exists in registry. Use -Force to overwrite the existing module.");
            }
            var binaryBicepSources = bicepSources == null ? null : BinaryData.FromStream(bicepSources);
            await this.moduleDispatcher.PublishModule(target, BinaryData.FromStream(compiledArmTemplate), binaryBicepSources, documentationUri);
        }
        catch (ExternalArtifactException exception)
        {
            throw new BicepException($"Unable to publish module \"{target.FullyQualifiedReference}\": {exception.Message}");
        }
    }
}
