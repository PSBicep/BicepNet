using Azure.Core;
using Azure.ResourceManager;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    public static async Task<IEnumerable<string>> GetManagementGroupDescendantsAsync(ResourceIdentifier resourceIdentifier, ArmClient armClient, CancellationToken cancellationToken)
    {
        var mg = armClient.GetManagementGroupResource(resourceIdentifier);
        var list = mg.GetDescendantsAsync(cancellationToken: cancellationToken);

        var taskList = new List<string>();


        var subRegexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        var subRegex = new Regex(@"^/subscriptions/(?<subId>[^/]+)$", subRegexOptions);
       
        await foreach (var item in list)
        {
            var subRegexMatch = subRegex.Match(item.Id.ToString());
            if (subRegexMatch.Success)
            {
                var subId = $"{mg.Id}/subscriptions/{subRegexMatch.Groups["subId"].Value}";
                taskList.Add(subId);
            } else
            {
                taskList.Add(item.Id.ToString());
            }

        }
        return taskList;
    }

}