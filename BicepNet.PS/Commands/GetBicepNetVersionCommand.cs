using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS
{
    [Cmdlet(VerbsCommon.Get, "BicepNetVersion")]
    [CmdletBinding]
    public class GetBicepNetVersionCommand : PSCmdlet
    {

        public GetBicepNetVersionCommand()
        {
        }

        protected override void EndProcessing()
        {
            var result = BicepWrapper.BicepVersion;
            WriteObject(result);
        }
    }
}