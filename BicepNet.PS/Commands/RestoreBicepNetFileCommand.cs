using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsData.Restore, "BicepNetFile")]
    public class RestoreBicepNetFileCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        public string Token { get; set; }

        protected override void ProcessRecord()
        {
            BicepWrapper.Restore(Path);
        }
    }
}