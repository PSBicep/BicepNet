using Azure.Core;
using Azure.ResourceManager;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core.Azure;

internal static class PolicyHelper
{
    public static async Task<IDictionary<string, JsonElement>> ListPolicyDefinitionsAsync(ResourceIdentifier scopeResourceId, ArmClient armClient, CancellationToken cancellationToken)
    {
        return (string)scopeResourceId.ResourceType switch
        {
            "Microsoft.Resources/subscriptions" => throw new NotImplementedException(),
            "Microsoft.Management/managementGroups" => await ManagementGroupHelper.ListManagementGroupPoliciesAsync(scopeResourceId, armClient, cancellationToken),
            "Microsoft.Resources/tenants" => throw new NotImplementedException(),
            _ => throw new Exception($"Failed to list PolicyDefinitions on scope '{scopeResourceId}' with type '{scopeResourceId.ResourceType}"),
        };
    }
    public static async Task<JsonElement> GetPolicyDefinitionAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        switch (resourceIdentifier.Parent?.ResourceType)
        {
            case "Microsoft.Resources/subscriptions":
                var subPolicyDef = armClient.GetSubscriptionPolicyDefinitionResource(resourceIdentifier);
                var subPolicyDefResponse = await subPolicyDef.GetAsync(cancellationToken);

                if (subPolicyDefResponse is null || subPolicyDefResponse.GetRawResponse().ContentStream is not { } subContentStream)
                {
                    throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}'");
                }
                subContentStream.Position = 0;
                return await JsonSerializer.DeserializeAsync<JsonElement>(subContentStream, cancellationToken: cancellationToken);
            case "Microsoft.Management/managementGroups":
                var mgPolicyDef = armClient.GetManagementGroupPolicyDefinitionResource(resourceIdentifier);
                var mgPolicyDefResponse = await mgPolicyDef.GetAsync(cancellationToken);

                if (mgPolicyDefResponse is null || mgPolicyDefResponse.GetRawResponse().ContentStream is not { } mgContentStream)
                {
                    throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}'");
                }
                mgContentStream.Position = 0;
                return await JsonSerializer.DeserializeAsync<JsonElement>(mgContentStream, cancellationToken: cancellationToken);
            case "Microsoft.Resources/tenants":
                var tenantPolicyDef = armClient.GetTenantPolicyDefinitionResource(resourceIdentifier);
                var tenantPolicyDefResponse = await tenantPolicyDef.GetAsync(cancellationToken);

                if (tenantPolicyDefResponse is null || tenantPolicyDefResponse.GetRawResponse().ContentStream is not { } tenantContentStream)
                {
                    throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}'");
                }
                tenantContentStream.Position = 0;
                return await JsonSerializer.DeserializeAsync<JsonElement>(tenantContentStream, cancellationToken: cancellationToken);
            default:
                throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}' and parent '{resourceIdentifier.Parent?.ResourceType}");
        }
    }
}