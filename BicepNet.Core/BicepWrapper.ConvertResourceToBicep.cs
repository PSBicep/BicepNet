using BicepNet.Core.Azure;
using System.Text.Json;

namespace BicepNet.Core;

public partial class BicepWrapper
{
    public static string ConvertResourceToBicep(string resourceId, string resourceBody)
    {
        var id = AzureHelpers.ValidateResourceId(resourceId);
        var matchedType = BicepHelper.ResolveBicepTypeDefinition(id.FullyQualifiedType, azResourceTypeLoader, logger);
        JsonElement resource = JsonSerializer.Deserialize<JsonElement>(resourceBody);

        return azResourceProvider.GenerateBicepTemplate(id, matchedType, resource);
    }
}