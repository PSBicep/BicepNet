using Azure.Core;
using Azure.ResourceManager;
using Bicep.Core.Configuration;
using Bicep.Core.Registry.Auth;
using Bicep.LanguageServer.Providers;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core.Azure;
public class AzureResourceProvider : IAzResourceProvider
{
    private readonly ITokenCredentialFactory credentialFactory;

    public AzureResourceProvider(ITokenCredentialFactory credentialFactory)
    {
        this.credentialFactory = credentialFactory;
    }

    private ArmClient CreateArmClient(RootConfiguration configuration, string subscriptionId, (string resourceType, string? apiVersion) resourceTypeApiVersionMapping)
    {
        var options = new ArmClientOptions
        {
            //Diagnostics.ApplySharedResourceManagerSettings();
            Environment = new ArmEnvironment(configuration.Cloud.ResourceManagerEndpointUri, configuration.Cloud.AuthenticationScope)
        };
        if(resourceTypeApiVersionMapping.apiVersion is not null)
        {
            options.SetApiVersion(new ResourceType(resourceTypeApiVersionMapping.resourceType), resourceTypeApiVersionMapping.apiVersion);
        }

        var credential = credentialFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);

        return new ArmClient(credential, subscriptionId, options);
    }

    public async Task<JsonElement> GetGenericResource(RootConfiguration configuration, IAzResourceProvider.AzResourceIdentifier resourceId, string? apiVersion, CancellationToken cancellationToken)
    {
        //var resourceTypeApiVersionMapping = new List<(string resourceType, string apiVersion)>();
        (string resourceType, string? apiVersion) resourceTypeApiVersionMapping = ("",null);
        if (apiVersion is not null)
        {
            resourceTypeApiVersionMapping = (resourceId.FullyQualifiedType, apiVersion);
            // If we have an API version from the Bicep type, use it.
            // Otherwise, the SDK client will attempt to fetch the latest version from the /providers/<provider> API.
            // This is not always guaranteed to work, as child resources are not necessarily declared.
            //resourceTypeApiVersionMapping.Add((
            //    resourceType: resourceId.FullyQualifiedType, apiVersion));
        }

        var armClient = CreateArmClient(configuration, resourceId.subscriptionId, resourceTypeApiVersionMapping);
        var resourceIdentifier = new ResourceIdentifier(resourceId.FullyQualifiedId);
        switch (resourceIdentifier.ResourceType)
        {
            case "Microsoft.Authorization/policyDefinitions":
                return await GetPolicyDefinitionAsync(resourceIdentifier, armClient, cancellationToken);

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

    public static async Task<JsonElement> GetPolicyDefinitionAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        switch (resourceIdentifier.Parent?.ResourceType)
        {
            case "Microsoft.Management/managementGroups":
                var mgPolicyDef = armClient.GetManagementGroupPolicyDefinitionResource(resourceIdentifier);
                var mgPolicyDefResponse = await mgPolicyDef.GetAsync(cancellationToken);
                
                if(mgPolicyDefResponse is null || mgPolicyDefResponse.GetRawResponse().ContentStream is not { } contentStream)
                {
                    throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}'");
                }
                contentStream.Position = 0;
                return await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, cancellationToken: cancellationToken);
            default:
                throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}'");
        }
    }

}
