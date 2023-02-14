using Azure.Core;
using Azure.ResourceManager;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Parsing;
using Bicep.Core.PrettyPrint;
using Bicep.Core.PrettyPrint.Options;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Resources;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Syntax;
using Bicep.LanguageServer.Providers;
using BicepNet.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core.Azure;
public class AzureResourceProvider : IAzResourceProvider
{
    private readonly ITokenCredentialFactory credentialFactory;
    private readonly IFileResolver fileResolver;
    private readonly IModuleDispatcher moduleDispatcher;
    private readonly BicepNetConfigurationManager configurationManager;
    private readonly IFeatureProviderFactory featureProviderFactory;
    private readonly INamespaceProvider namespaceProvider;
    private readonly IApiVersionProviderFactory apiVersionProviderFactory;
    private readonly IBicepAnalyzer linterAnalyzer;

    public AzureResourceProvider(ITokenCredentialFactory credentialFactory, IFileResolver fileResolver,
        IModuleDispatcher moduleDispatcher, BicepNetConfigurationManager configurationManager, IFeatureProviderFactory featureProviderFactory, INamespaceProvider namespaceProvider,
        IApiVersionProviderFactory apiVersionProviderFactory, IBicepAnalyzer linterAnalyzer)
    {
        this.credentialFactory = credentialFactory;
        this.fileResolver = fileResolver;
        this.moduleDispatcher = moduleDispatcher;
        this.configurationManager = configurationManager;
        this.featureProviderFactory = featureProviderFactory;
        this.namespaceProvider = namespaceProvider;
        this.apiVersionProviderFactory = apiVersionProviderFactory;
        this.linterAnalyzer = linterAnalyzer;
    }

    private ArmClient CreateArmClient(RootConfiguration configuration, string subscriptionId, (string resourceType, string? apiVersion) resourceTypeApiVersionMapping)
    {
        var options = new ArmClientOptions
        {
            Environment = new ArmEnvironment(configuration.Cloud.ResourceManagerEndpointUri, configuration.Cloud.AuthenticationScope)
        };
        if (resourceTypeApiVersionMapping.apiVersion is not null)
        {
            options.SetApiVersion(new ResourceType(resourceTypeApiVersionMapping.resourceType), resourceTypeApiVersionMapping.apiVersion);
        }

        var credential = credentialFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);

        return new ArmClient(credential, subscriptionId, options);
    }
    public async Task<IDictionary<string, JsonElement>> GetChildResourcesAsync(RootConfiguration configuration, IAzResourceProvider.AzResourceIdentifier resourceId, ChildResourceType childType, string? apiVersion, CancellationToken cancellationToken)
    {
        (string resourceType, string? apiVersion) resourceTypeApiVersionMapping = (resourceId.FullyQualifiedType, apiVersion);

        var armClient = CreateArmClient(configuration, resourceId.subscriptionId, resourceTypeApiVersionMapping);
        var resourceIdentifier = new ResourceIdentifier(resourceId.FullyQualifiedId);

        return childType switch
        {
            ChildResourceType.PolicyDefinitions => await PolicyHelper.ListPolicyDefinitionsAsync(resourceIdentifier, armClient, cancellationToken),
            ChildResourceType.PolicyInitiatives => throw new NotImplementedException(),
            ChildResourceType.PolicyAssignments => throw new NotImplementedException(),
            ChildResourceType.RoleDefinitions => throw new NotImplementedException(),
            ChildResourceType.RoleAssignments => throw new NotImplementedException(),
            ChildResourceType.Subscriptions => throw new NotImplementedException(),
            ChildResourceType.ResourceGroups => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };
    }
    public async Task<JsonElement> GetGenericResource(RootConfiguration configuration, IAzResourceProvider.AzResourceIdentifier resourceId, string? apiVersion, CancellationToken cancellationToken)
    {
        (string resourceType, string? apiVersion) resourceTypeApiVersionMapping = (resourceId.FullyQualifiedType, apiVersion);

        var armClient = CreateArmClient(configuration, resourceId.subscriptionId, resourceTypeApiVersionMapping);
        var resourceIdentifier = new ResourceIdentifier(resourceId.FullyQualifiedId);

        switch (resourceIdentifier.ResourceType)
        {
            case "Microsoft.Management/managementGroups":
                return await ManagementGroupHelper.GetManagementGroupAsync(resourceIdentifier, armClient, cancellationToken);
            case "Microsoft.Authorization/policyDefinitions":
                return await PolicyHelper.GetPolicyDefinitionAsync(resourceIdentifier, armClient, cancellationToken);

            default:
                var genericResourceResponse = await armClient.GetGenericResource(resourceIdentifier).GetAsync(cancellationToken);
                if (genericResourceResponse is null ||
                    genericResourceResponse.GetRawResponse().ContentStream is not { } contentStream)
                {
                    throw new Exception($"Failed to fetch resource from Id '{resourceId.FullyQualifiedId}'");
                }

                contentStream.Position = 0;
                return await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, cancellationToken: cancellationToken);
        }

    }
    public static string GenerateBicepTemplate(IAzResourceProvider.AzResourceIdentifier resourceId, ResourceTypeReference resourceType, JsonElement resource)
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

        template = targetScope + template;
        return template;
    }
}