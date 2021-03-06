using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Get, "BicepNetVersion")]
    [CmdletBinding]
    public class GetBicepNetVersionCommand : BicepNetBaseCommand
    {
        protected override void EndProcessing()
        {
            var result = BicepWrapper.BicepVersion;
            WriteObject(result);
        }
    }
}