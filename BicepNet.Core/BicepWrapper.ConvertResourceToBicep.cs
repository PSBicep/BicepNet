using Bicep.Core.Diagnostics;
using Bicep.Core.Navigation;
using Bicep.Core.PrettyPrint;
using Bicep.Core.PrettyPrint.Options;
using Bicep.Core.Registry;
using Bicep.Core.Rewriters;
using Bicep.Core.Semantics;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using BicepNet.Core.Azure;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public string ConvertResourceToBicep(string resourceId, string resourceBody)
    {
        var id = AzureHelpers.ValidateResourceId(resourceId);
        var matchedType = BicepHelper.ResolveBicepTypeDefinition(id.FullyQualifiedType, azResourceTypeLoader, logger);
        JsonElement resource = JsonSerializer.Deserialize<JsonElement>(resourceBody);

        var template = AzureResourceProvider.GenerateBicepTemplate(id, matchedType, resource, includeTargetScope: true);
        return RewriteBicepTemplate(template);
    }

    public string RewriteBicepTemplate(string template)
    {
        BicepFile virtualBicepFile = SourceFileFactory.CreateBicepFile(new Uri($"inmemory:///generated.bicep"), template);

        // SourceFileGroupingBuilder.Build doesn't work with fake files (because of some internal recursive shenanigans?)
        // So we build the grouping manually à la hack
        var sourceFileGrouping = new SourceFileGrouping(
            fileResolver,
            virtualBicepFile.FileUri,
            new Dictionary<Uri, ResultWithDiagnostic<ISourceFile>>
            {
                new KeyValuePair<Uri, ResultWithDiagnostic<ISourceFile>>(virtualBicepFile.FileUri, new ResultWithDiagnostic<ISourceFile>(virtualBicepFile))
            }.ToImmutableDictionary(),
            new Dictionary<ISourceFile, ImmutableDictionary<IArtifactReferenceSyntax, Result<Uri, UriResolutionError>>>().ToImmutableDictionary(),
            new Dictionary<ISourceFile, ImmutableHashSet<ISourceFile>>().ToImmutableDictionary()
        );
        var compilation = new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, bicepAnalyzer, moduleDispatcher);

        var bicepFile = RewriterHelper.RewriteMultiple(
                compilation,
                virtualBicepFile,
                rewritePasses: 1,
                model => new TypeCasingFixerRewriter(model),
                model => new ReadOnlyPropertyRemovalRewriter(model));

        var printOptions = new PrettyPrintOptions(NewlineOption.LF, IndentKindOption.Space, 2, false);
        template = PrettyPrinter.PrintValidProgram(bicepFile.ProgramSyntax, printOptions);

        return template;
    }
}
