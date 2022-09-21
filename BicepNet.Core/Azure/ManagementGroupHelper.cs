using Azure.Core;
using Azure.ResourceManager;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core.Azure;

internal static class ManagementGroupHelper
{
    public static async Task<JsonElement> GetManagementGroupAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        var mg = armClient.GetManagementGroupResource(resourceIdentifier);
        var mgResponse = await mg.GetAsync(cancellationToken: cancellationToken);
        if (mgResponse is null || mgResponse.GetRawResponse().ContentStream is not { } mgContentStream)
        {
            throw new Exception($"Failed to fetch resource from Id '{resourceIdentifier}'");
        }
        mgContentStream.Position = 0;
        return await JsonSerializer.DeserializeAsync<JsonElement>(mgContentStream, cancellationToken: cancellationToken);
    }

    public static async Task<IDictionary<string, JsonElement>> ListManagementGroupPoliciesAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, JsonElement>();
        var mg = armClient.GetManagementGroupResource(resourceIdentifier);

        var collection = mg.GetManagementGroupPolicyDefinitions();
        var list = collection.GetAllAsync(filter: "atExactScope()", cancellationToken: cancellationToken);
        JsonElement element;
        await foreach (var item in list)
        {
            var id = item.Id.ToString();
            var resourceId = AzureHelpers.ValidateResourceId(id);
            resourceId.Deconstruct(
                out string fullyQualifiedId,
                out string fullyQualifiedType,
                out string fullyQualifiedName,
                out string unqualifiedName,
                out string subscriptionId
            );
            var resource = await item.GetAsync(cancellationToken: cancellationToken);
            if (resource is null ||
                resource.GetRawResponse().ContentStream is not { } contentStream)
            {
                throw new Exception($"Failed to fetch resource from Id '{resourceId.FullyQualifiedId}'");
            }

            contentStream.Position = 0;
            element = await JsonSerializer.DeserializeAsync<JsonElement>(contentStream, cancellationToken: cancellationToken);
            //element = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(item.Data));
            result.Add(id, element);
        }
        return result;
    }
}