using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS
{
    [Cmdlet(VerbsCommon.Find, "BicepNetModule")]
    public class FindBicepNetModuleCommand : PSCmdlet
    {

        public FindBicepNetModuleCommand()
        {
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            var result = BicepWrapper.FindModules(Path);
            WriteObject(result);
        }

        protected override void EndProcessing()
        {

        }
    }
}