using Azure.Core;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.Parsing;
using Bicep.Core.PrettyPrint;
using Bicep.Core.PrettyPrint.Options;
using Bicep.Core.Resources;
using Bicep.Core.Rewriters;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.Providers;
using BicepNet.Core.Azure;
using System;
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

        var template = GenerateBicepTemplate(id, matchedType, resource, includeTargetScope: true);
        template = RewriteBicepTemplate(template);

        return template;
    }

    public string RewriteBicepTemplate(string template)
    {
        BicepFile virtualBicepFile = SourceFileFactory.CreateBicepFile(new Uri($"inmemory:///generated.bicep"), template);

        var workspace = new Workspace();
        workspace.UpsertSourceFiles(virtualBicepFile.AsEnumerable());

        var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, virtualBicepFile.FileUri, forceModulesRestore: false);
        var compilation = new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, apiVersionProviderFactory, bicepAnalyzer);

        var bicepFile = RewriterHelper.RewriteMultiple(
                compilation,
                virtualBicepFile,
                rewritePasses: 1,
                model => new TypeCasingFixerRewriter(model),
                model => new ReadOnlyPropertyRemovalRewriter(model));
        var printOptions = new PrettyPrintOptions(NewlineOption.LF, IndentKindOption.Space, 2, false);
        template = PrettyPrinter.PrintProgram(bicepFile.ProgramSyntax, printOptions);

        return template;
    }

    public static string GenerateBicepTemplate(IAzResourceProvider.AzResourceIdentifier resourceId, ResourceTypeReference resourceType, JsonElement resource, bool includeTargetScope = false)
    {
        var resourceIdentifier = new ResourceIdentifier(resourceId.FullyQualifiedId);
        string targetScope = (string?)(resourceIdentifier.Parent?.ResourceType) switch
        {
            "Microsoft.Resources/resourceGroups" => $"targetScope = 'resourceGroup'{Environment.NewLine}",
            "Microsoft.Resources/subscriptions" => $"targetScope = 'subscription'{Environment.NewLine}",
            "Microsoft.Management/managementGroups" => $"targetScope = 'managementGroup'{Environment.NewLine}",
            _ => $"targetScope = 'tenant'{Environment.NewLine}",
        };

        var resourceDeclaration = AzureHelpers.CreateResourceSyntax(resource, resourceId, resourceType);

        var printOptions = new PrettyPrintOptions(NewlineOption.LF, IndentKindOption.Space, 2, false);
        var program = new ProgramSyntax(
            new[] { resourceDeclaration },
            SyntaxFactory.CreateToken(TokenType.EndOfFile),
            ImmutableArray<IDiagnostic>.Empty);
        var template = PrettyPrinter.PrintProgram(program, printOptions);

        return includeTargetScope ? targetScope + template : template;
    }
}