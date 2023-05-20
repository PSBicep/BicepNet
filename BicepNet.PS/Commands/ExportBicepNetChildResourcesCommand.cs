using System.Management.Automation;

namespace BicepNet.PS.Commands;

[Cmdlet(VerbsData.Export, "BicepNetChildResource")]
public class ExportBicepNetChildResourceCommand : BicepNetBaseCommand
{
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty]
    public string ParentResourceId { get; set; }

    protected override void ProcessRecord()
    {
        var result = bicepWrapper.ExportChildResoures(ParentResourceId);
        foreach (var key in result.Keys)
        {
            var outputObject = new PSObject();
            outputObject.Members.Add(new PSNoteProperty("name", key));
            outputObject.Members.Add(new PSNoteProperty("template", result[key]));
            WriteObject(outputObject);
        }
    }
}
