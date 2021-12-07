using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS
{
    [Cmdlet(VerbsData.Publish, "BicepNetFile")]
    public class PublishBicepNetFileCommand : PSCmdlet
    {
        public PublishBicepNetFileCommand() {}

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

        protected override void EndProcessing()
        {

        }
    }
}