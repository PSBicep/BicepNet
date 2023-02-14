using BicepNet.Core.Azure;
using BicepNet.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
	public IDictionary<string, string> ExportChildResoures(string scopeId, ChildResourceType type, string? configurationPath = null, bool includeTargetScope = false) =>
		joinableTaskFactory.Run(() => ExportChildResouresAsync(scopeId, type, configurationPath, includeTargetScope));

	public async Task<IDictionary<string, string>> ExportChildResouresAsync(string scopeId, ChildResourceType type, string? configurationPath = null, bool includeTargetScope = false)
	{
		Dictionary<string, string> result = new();
		var scopeResourceId = AzureHelpers.ValidateResourceId(scopeId);
		var cancellationToken = new CancellationToken();
		string fullyQualifiedType = type switch
		{
			ChildResourceType.PolicyInitiatives => "Microsoft.Authorization/policySetDefinitions",
			ChildResourceType.PolicyDefinitions => "Microsoft.Authorization/policyDefinitions",
			ChildResourceType.PolicyAssignments => "Microsoft.Authorization/policyAssignment",
			ChildResourceType.RoleDefinitions => "Microsoft.Authorization/roleDefinitions",
			ChildResourceType.RoleAssignments => "Microsoft.Authorization/roleAssignments",
			ChildResourceType.Subscriptions => "Microsoft.Authorization/subscriptions",
			ChildResourceType.ResourceGroups => "Microsoft.Authorization/resourceGroups",
			_ => throw new Exception("Invalid child resource type"),
		};
		var matchedType = BicepHelper.ResolveBicepTypeDefinition(fullyQualifiedType, azResourceTypeLoader, logger);
        var config = configurationManager.GetConfiguration(new Uri(configurationPath ?? ""));
        switch (type)
		{
			case ChildResourceType.PolicyDefinitions:
				var policyDefinitions = await azResourceProvider.GetChildResourcesAsync(config, scopeResourceId, type, matchedType.ApiVersion, cancellationToken);
				foreach (var (id, resource) in policyDefinitions)
				{
                    var name = AzureHelpers.GetResourceFriendlyName(id);
					var resourceId = AzureHelpers.ValidateResourceId(id);
					result.Add(name, GenerateBicepTemplate(resourceId, matchedType, resource, includeTargetScope: includeTargetScope));
				}
				break;
			default:
				throw new NotImplementedException();
		}

		return result;
	}
}