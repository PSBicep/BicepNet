using BicepNet.Core;
using System;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Get, "BicepNetConfig")]
    [CmdletBinding(DefaultParameterSetName = "Default")]
    public class GetBicepNetConfigCommand : BicepNetBaseCommand
    {

        [Parameter(ParameterSetName = "Default")]
        public SwitchParameter Default { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void EndProcessing()
        {
            if (ParameterSetName == "Default")
            {
                WriteObject(BicepWrapper.GetBicepConfigInfo());
            }
            else
            {
                WriteObject(BicepWrapper.GetBicepConfigInfo(Path));
            }
        }
    }
}