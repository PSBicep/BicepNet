using Azure.Core;
using Azure.Deployments.Core.Comparers;
using Azure.Deployments.Core.Definitions.Identifiers;
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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IDictionary<string, string> ExportResources(string[] ids) =>
            joinableTaskFactory.Run(() => ExportResourcesAsync(ids));

        public static async Task<IDictionary<string, string>> ExportResourcesAsync(string[] ids)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            var taskList = new List<Task<(string resourceName, string template)>>();
            foreach (string id in ids)
            {
                taskList.Add(ExportResourceAsync(id));
            }
            foreach ((string name, string template) in await Task.WhenAll(taskList))
            {
                result.Add(name, template);
            }
            return result;
        }
        private static async Task<(string resourceName, string template)> ExportResourceAsync(string id)
        {
            
            if (TryParseResourceId(id) is not { } resourceId)
            {
                var message = $"Failed to parse supplied resourceId \"{id}\".";
                logger?.LogCritical("{message}", message);
                throw new Exception(message);
            }

            resourceId.Deconstruct(
                out string fullyQualifiedId,
                out string fullyQualifiedType,
                out string fullyQualifiedName,
                out string unqualifiedName,
                out string subscriptionId
            );
            var matchedType = azResourceTypeLoader.GetAvailableTypes()
                .Where(x => StringComparer.OrdinalIgnoreCase.Equals(fullyQualifiedType, x.FormatType()))
                .OrderByDescending(x => x.ApiVersion, ApiVersionComparer.Instance)
                .FirstOrDefault();
            if (matchedType is null || matchedType.ApiVersion is null)
            {
                var message = $"Failed to find a Bicep type definition for resource of type \"{resourceId.FullyQualifiedType}\".";
                logger?.LogCritical("{message}", message);
                throw new Exception(message);
            }
            string resourceType = matchedType.FormatType();
            string apiVersion = matchedType.ApiVersion;

            JsonElement? resource = null;
            try
            {
                var cancellationToken = new CancellationToken();
                resource = await azResourceProvider.GetGenericResource(configuration, resourceId, apiVersion, cancellationToken);
            }
            catch (Exception exception)
            {
                var message = $"Failed to fetch resource '{resourceId}' with API version {matchedType?.ApiVersion}: {exception}";
                logger?.LogError("{message}", message);
                throw new Exception(message);
            }
            var resourceIdentifier = new ResourceIdentifier(resourceId.FullyQualifiedId);
            string targetScope = (string?)(resourceIdentifier.Parent?.ResourceType) switch
            {
                "Microsoft.Resources/resourceGroups" => $"targetScope = 'resourceGroup'{Environment.NewLine}",
                "Microsoft.Resources/subscriptions" => $"targetScope = 'subscription'{Environment.NewLine}",
                "Microsoft.Management/managementGroups" => $"targetScope = 'managementGroup'{Environment.NewLine}",
                _ => $"targetScope = 'tenant'{Environment.NewLine}",
            };

            var resourceDeclaration = CreateResourceSyntax(resource.Value, resourceId, matchedType);

            // From GenerateCodeReplacement()
            var printOptions = new PrettyPrintOptions(NewlineOption.LF, IndentKindOption.Space, 2, false);
            var program = new ProgramSyntax(
                new[] { resourceDeclaration },
                SyntaxFactory.CreateToken(TokenType.EndOfFile),
                ImmutableArray<IDiagnostic>.Empty);

            var template = PrettyPrinter.PrintProgram(program, printOptions);

            BicepFile virtualBicepFile = SourceFileFactory.CreateBicepFile(new Uri($"inmemory://generated.bicep"), template);
            var workspace = new Workspace();
            workspace.UpsertSourceFiles(virtualBicepFile.AsEnumerable());
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, virtualBicepFile.FileUri, configuration, false);

            var compilation = new Compilation(featureProvider, namespaceProvider, sourceFileGrouping, configuration, apiVersionProvider, linterAnalyzer);

            var bicepFile = RewriterHelper.RewriteMultiple(
                    compilation,
                    SourceFileFactory.CreateBicepFile(virtualBicepFile.FileUri, template),
                    rewritePasses: 5,
                    model => new TypeCasingFixerRewriter(model),
                    model => new ReadOnlyPropertyRemovalRewriter(model));
            template = PrettyPrinter.PrintProgram(bicepFile.ProgramSyntax, printOptions);
            template = targetScope + template;
            var name = string.Format("{0}_{1}", fullyQualifiedType.Replace(@"/", ""), fullyQualifiedName.Replace(@"/", ""));
            return (name, template);
        }

        private static SyntaxBase ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var properties = new List<ObjectPropertySyntax>();
                    foreach (var property in element.EnumerateObject())
                    {
                        properties.Add(SyntaxFactory.CreateObjectProperty(property.Name, ConvertJsonElement(property.Value)));
                    }
                    return SyntaxFactory.CreateObject(properties);
                case JsonValueKind.Array:
                    var items = new List<SyntaxBase>();
                    foreach (var value in element.EnumerateArray())
                    {
                        items.Add(ConvertJsonElement(value));
                    }
                    return SyntaxFactory.CreateArray(items);
                case JsonValueKind.String:
                    return SyntaxFactory.CreateStringLiteral(element.GetString()!);
                case JsonValueKind.Number:
                    if (element.TryGetInt64(out long intValue))
                    {
                        return SyntaxFactory.CreatePositiveOrNegativeInteger(intValue);
                    }
                    return SyntaxFactory.CreateStringLiteral(element.ToString()!);
                case JsonValueKind.True:
                    return SyntaxFactory.CreateToken(TokenType.TrueKeyword);
                case JsonValueKind.False:
                    return SyntaxFactory.CreateToken(TokenType.FalseKeyword);
                case JsonValueKind.Null:
                    return SyntaxFactory.CreateToken(TokenType.NullKeyword);
                default:
                    throw new InvalidOperationException($"Failed to deserialize JSON");
            }
        }

        // Private method originally copied from InsertResourceHandler.cs
        private static ResourceDeclarationSyntax CreateResourceSyntax(JsonElement resource, IAzResourceProvider.AzResourceIdentifier resourceId, ResourceTypeReference typeReference)
        {
            var properties = new List<ObjectPropertySyntax>();
            foreach (var property in resource.EnumerateObject())
            {
                switch (property.Name.ToLowerInvariant())
                {
                    case "id":
                    case "type":
                    case "apiVersion":
                        // Don't add these to the resource properties - they're part of the resource declaration.
                        break;
                    case "name":
                        // Use the fully-qualified name instead of the name returned by the RP.
                        properties.Add(SyntaxFactory.CreateObjectProperty(
                            "name",
                            SyntaxFactory.CreateStringLiteral(resourceId.FullyQualifiedName)));
                        break;
                    default:
                        properties.Add(SyntaxFactory.CreateObjectProperty(
                            property.Name,
                            ConvertJsonElement(property.Value)));
                        break;
                }
            }

            var description = SyntaxFactory.CreateDecorator(
                "description",
                SyntaxFactory.CreateStringLiteral($"Generated from {resourceId.FullyQualifiedId}"));

            return new ResourceDeclarationSyntax(
                new SyntaxBase[] { description, SyntaxFactory.NewlineToken, },
                SyntaxFactory.CreateToken(TokenType.Identifier, "resource"),
                SyntaxFactory.CreateIdentifier(Regex.Replace(resourceId.UnqualifiedName, "[^a-zA-Z]", "")),
                SyntaxFactory.CreateStringLiteral(typeReference.FormatName()),
                null,
                SyntaxFactory.CreateToken(TokenType.Assignment),
                SyntaxFactory.CreateObject(properties));
        }

        // Private method originally copied from InsertResourceHandler.cs
        private static IAzResourceProvider.AzResourceIdentifier? TryParseResourceId(string? resourceIdString)
        {
            if (resourceIdString is null)
            {
                return null;
            }

            if (ResourceId.TryParse(resourceIdString, out var resourceId))
            {
                return new(
                    resourceId.FullyQualifiedId,
                    resourceId.FormatFullyQualifiedType(),
                    resourceId.FormatName(),
                    resourceId.NameHierarchy.Last(),
                    string.Empty);
            }

            var rgRegexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
            var rgRegex = new Regex(@"^/subscriptions/(?<subId>[^/]+)/resourceGroups/(?<rgName>[^/]+)$", rgRegexOptions);
            var rgRegexMatch = rgRegex.Match(resourceIdString);
            if (rgRegexMatch.Success)
            {
                return new(
                    resourceIdString,
                    "Microsoft.Resources/resourceGroups",
                    rgRegexMatch.Groups["rgName"].Value,
                    rgRegexMatch.Groups["rgName"].Value,
                    rgRegexMatch.Groups["subId"].Value);
            }

            return null;
        }
    }
}
