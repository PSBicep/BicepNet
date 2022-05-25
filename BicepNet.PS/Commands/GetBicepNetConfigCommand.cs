using BicepNet.Core;
using System;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Get, "BicepNetConfig")]
    [CmdletBinding]
    public class GetBicepNetConfigCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void EndProcessing()
        {
            WriteObject(BicepWrapper.GetBicepConfigInfo(Path));
        }
    }
}