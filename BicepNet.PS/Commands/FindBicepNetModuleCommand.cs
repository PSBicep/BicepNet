using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Find, "BicepNetModule")]
    public class FindBicepNetModuleCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            var result = BicepWrapper.FindModules(Path);
            WriteObject(result);
        }
    }
}