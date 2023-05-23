using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core.Azure;

internal static class RoleHelper
{
    public static async Task<IDictionary<string, JsonElement>> ListRoleDefinitionsAsync(ResourceIdentifier scopeResourceId, ArmClient armClient, CancellationToken cancellationToken)
    {
        return (string)scopeResourceId.ResourceType switch
        {
            "Microsoft.Management/managementGroups" => await ListManagementGroupRoleDefinitionAsync(scopeResourceId, armClient, cancellationToken),
            "Microsoft.Resources/subscriptions" => await ListSubscriptionRoleDefinitionAsync(scopeResourceId, armClient, cancellationToken),
            _ => throw new InvalidOperationException($"Failed to list RoleDefinitions on scope '{scopeResourceId}' with type '{scopeResourceId.ResourceType}")
        };
    }

    public static async Task<IDictionary<string, JsonElement>> ListRoleAssignmentsAsync(ResourceIdentifier scopeResourceId, ArmClient armClient, CancellationToken cancellationToken)
    {
        return (string)scopeResourceId.ResourceType switch
        {
            "Microsoft.Management/managementGroups" => await ListManagementGroupRoleAssignmentsAsync(scopeResourceId, armClient, cancellationToken),
            "Microsoft.Resources/subscriptions" => await ListSubscriptionRoleAssignmentsAsync(scopeResourceId, armClient, cancellationToken),
            "Microsoft.Resources/resourceGroups" => await ListResourceGroupRoleAssignmentsAsync(scopeResourceId, armClient, cancellationToken),
            _ => await ListResourceRoleAssignmentsAsync(scopeResourceId, armClient, cancellationToken) // Default to "resource" scope
        };
    }

    private static async Task<IDictionary<string, JsonElement>> ListManagementGroupRoleDefinitionAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
        => await GetRoleDefinitionResourcesAsync(armClient.GetManagementGroupResource(resourceIdentifier).GetAuthorizationRoleDefinitions(), cancellationToken);

    private static async Task<IDictionary<string, JsonElement>> ListSubscriptionRoleDefinitionAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
        => await GetRoleDefinitionResourcesAsync(armClient.GetSubscriptionResource(resourceIdentifier).GetAuthorizationRoleDefinitions(), cancellationToken);
    
    private static async Task<IDictionary<string, JsonElement>> ListManagementGroupRoleAssignmentsAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
        => await GetRoleAssignmentResourcesAsync(armClient.GetManagementGroupResource(resourceIdentifier).GetRoleAssignments(), cancellationToken);
    
    private static async Task<IDictionary<string, JsonElement>> ListSubscriptionRoleAssignmentsAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
        => await GetRoleAssignmentResourcesAsync(armClient.GetSubscriptionResource(resourceIdentifier).GetRoleAssignments(), cancellationToken);

    private static async Task<IDictionary<string, JsonElement>> ListResourceGroupRoleAssignmentsAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
        => await GetRoleAssignmentResourcesAsync(armClient.GetResourceGroupResource(resourceIdentifier).GetRoleAssignments(), cancellationToken);

    private static async Task<IDictionary<string, JsonElement>> ListResourceRoleAssignmentsAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
        => await GetRoleAssignmentResourcesAsync(armClient.GetGenericResource(resourceIdentifier).GetRoleAssignments(), cancellationToken);

    private static async Task<IDictionary<string, JsonElement>> GetRoleAssignmentResourcesAsync(RoleAssignmentCollection collection, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, JsonElement>();
        var list = collection.GetAllAsync(filter: "atExactScope()", cancellationToken: cancellationToken);

        JsonElement element;

        var taskList = new Dictionary<string, Task<Response<RoleAssignmentResource>>>();
        await foreach (var item in list)
        {
            taskList.Add(item.Id.ToString(), item.GetAsync(cancellationToken: cancellationToken));
        }

        foreach (var id in taskList.Keys)
        {
            var policyItemResponse = await taskList[id];
            var resourceId = AzureHelpers.ValidateResourceId(id);
            if (policyItemResponse is null ||
                policyItemResponse.GetRawResponse().ContentStream is not { } contentStream)
            {
                throw new InvalidOperationException($"Failed to fetch resource from Id '{resourceId.FullyQualifiedId}'");
            }
            contentStream.Position = 0;
            element = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, cancellationToken: cancellationToken);
            result.Add(id, element);
        }

        return result;
    }

    private static async Task<IDictionary<string, JsonElement>> GetRoleDefinitionResourcesAsync(AuthorizationRoleDefinitionCollection collection, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, JsonElement>();
        var list = collection.GetAllAsync(filter: "atExactScope()", cancellationToken: cancellationToken);

        JsonElement element;

        var taskList = new Dictionary<string, Task<Response<AuthorizationRoleDefinitionResource>>>();
        await foreach (var item in list)
        {
            taskList.Add(item.Id.ToString(), item.GetAsync(cancellationToken: cancellationToken));
        }

        foreach (var id in taskList.Keys)
        {
            var policyItemResponse = await taskList[id];
            var resourceId = AzureHelpers.ValidateResourceId(id);
            if (policyItemResponse is null ||
                policyItemResponse.GetRawResponse().ContentStream is not { } contentStream)
            {
                throw new InvalidOperationException($"Failed to fetch resource from Id '{resourceId.FullyQualifiedId}'");
            }
            contentStream.Position = 0;
            element = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, cancellationToken: cancellationToken);
            result.Add(id, element);
        }

        return result;
    }

    public static async Task<JsonElement> GetRoleDefinitionAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        try
        {
            var resource = armClient.GetAuthorizationRoleDefinitionResource(resourceIdentifier);
            var roleDefResponse = await resource.GetAsync(cancellationToken);

            if (roleDefResponse is null || roleDefResponse.GetRawResponse().ContentStream is not { } contentStream)
            {
                throw new InvalidOperationException($"Failed to fetch resource from Id '{resourceIdentifier}'");
            }
            contentStream.Position = 0;
            return await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list RoleDefinitions on scope '{resourceIdentifier}' with type '{resourceIdentifier.ResourceType}", ex);
        }
    }

    public static async Task<JsonElement> GetRoleAssignmentAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        var pa = armClient.GetRoleAssignmentResource(resourceIdentifier);
        var paResponse = await pa.GetAsync(cancellationToken: cancellationToken);
        if (paResponse is null || paResponse.GetRawResponse().ContentStream is not { } paContentStream)
        {
            throw new InvalidOperationException($"Failed to fetch resource from Id '{resourceIdentifier}'");
        }
        paContentStream.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(paContentStream, cancellationToken: cancellationToken);
    }
}