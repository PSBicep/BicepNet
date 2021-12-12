using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsData.Publish, "BicepNetFile")]
    public class PublishBicepNetFileCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Target { get; set; }

        protected override void ProcessRecord()
        {
            BicepWrapper.Publish(Path, Target, true);
        }
    }
}