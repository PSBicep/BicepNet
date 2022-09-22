using BicepNet.Core.Azure;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core;

public partial class BicepWrapper
{
	public static IDictionary<string, string> ExportChildResoures(string scopeId, ChildResourceType type) =>
		joinableTaskFactory.Run(() => ExportChildResouresAsync(scopeId, type));

	public static async Task<IDictionary<string, string>> ExportChildResouresAsync(string scopeId, ChildResourceType type)
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
		switch (type)
		{
			case ChildResourceType.PolicyDefinitions:
				var policyDefinitions = await azResourceProvider.GetChildResourcesAsync(configuration, scopeResourceId, type, matchedType.ApiVersion, cancellationToken);
				foreach (var (id, resource) in policyDefinitions)
				{
                        var name = AzureHelpers.GetResourceFriendlyName(id);
					var resourceId = AzureHelpers.ValidateResourceId(id);
					result.Add(name, azResourceProvider.GenerateBicepTemplate(resourceId, matchedType, resource));
				}
				break;
			default:
				throw new NotImplementedException();
		}

		return result;
	}
}