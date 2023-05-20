using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
	public IDictionary<string, string> ExportChildResoures(string scopeId, string? configurationPath = null, bool includeTargetScope = false) =>
		joinableTaskFactory.Run(() => ExportChildResouresAsync(scopeId, configurationPath, includeTargetScope));

	public async Task<IDictionary<string, string>> ExportChildResouresAsync(string scopeId, string? configurationPath = null, bool includeTargetScope = false)
	{
		Dictionary<string, string> result = new();
		var scopeResourceId = AzureHelpers.ValidateResourceId(scopeId);
		var cancellationToken = new CancellationToken();
        var config = configurationManager.GetConfiguration(new Uri(configurationPath ?? "inmemory://main.bicep"));
        var resourceDefinitions = await azResourceProvider.GetChildResourcesAsync(config, scopeResourceId, cancellationToken);

        foreach (var (id, resource) in resourceDefinitions)
        {
            var name = AzureHelpers.GetResourceFriendlyName(id);
            var resourceId = AzureHelpers.ValidateResourceId(id);
            var matchedType = BicepHelper.ResolveBicepTypeDefinition(resourceId.FullyQualifiedType, azResourceTypeLoader, logger);
            result.Add(name, GenerateBicepTemplate(resourceId, matchedType, resource, includeTargetScope: includeTargetScope));
        }

		return result;
	}
}