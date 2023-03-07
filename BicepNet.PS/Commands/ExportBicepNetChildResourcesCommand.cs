using BicepNet.Core.Azure;
using System;
using System.Management.Automation;

namespace BicepNet.PS.Commands;

[Cmdlet(VerbsData.Export, "BicepNetChildResource")]
public class ExportBicepNetChildResourceCommand : BicepNetBaseCommand
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public string ParentResourceId { get; set; }

    [Parameter(Mandatory = true)]
    [ValidateSet(new[] { "PolicyDefinitions", "PolicyInitiatives", "PolicyAssignments", "RoleDefinitions", "RoleAssignments", "Subscriptions", "ResourceGroups" })]
    public string ResourceType { get; set; }

    protected override void ProcessRecord()
    {
        var result = bicepWrapper.ExportChildResoures(ParentResourceId, (ChildResourceType)Enum.Parse(typeof(ChildResourceType), ResourceType));
        WriteObject(result);
    }
}
