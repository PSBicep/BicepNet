using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Find, "BicepNetModule")]
    public class FindBicepNetModuleCommand : BicepNetBaseCommand
    {
        [Parameter(ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter()]
        public SwitchParameter UseCache { get; set; }

        protected override void ProcessRecord()
        {
            var result = BicepWrapper.FindModules(Path, UseCache.IsPresent);
            WriteObject(result);
        }
    }
}