using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsData.Export, "BicepNetResource")]
    public class ExportBicepNetResourceCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string[] ResourceId { get; set; }

        protected override void ProcessRecord()
        {
            var result = bicepWrapper.ExportResources(ResourceId);
            WriteObject(result);
        }
    }
}
