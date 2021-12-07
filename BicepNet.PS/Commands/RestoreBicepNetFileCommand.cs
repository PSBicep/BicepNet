using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS
{
    [Cmdlet(VerbsData.Restore, "BicepNetFile")]
    public class RestoreBicepNetFileCommand : PSCmdlet
    {
        public RestoreBicepNetFileCommand() {}

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            BicepWrapper.Restore(Path);
        }

        protected override void EndProcessing()
        {

        }
    }
}