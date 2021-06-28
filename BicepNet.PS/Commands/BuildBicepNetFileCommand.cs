using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS
{
    [Cmdlet(VerbsLifecycle.Build, "BicepNetFile")]
    public class BuildBicepNetFileCommand : PSCmdlet
    {

        public BuildBicepNetFileCommand()
        {
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            var result = BicepWrapper.Build(Path);
            WriteObject(result);
        }

        protected override void EndProcessing()
        {

        }
    }
}