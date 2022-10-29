using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Get, "BicepNetAccessToken")]
    [CmdletBinding()]
    public class GetBicepNetAccessTokenCommand : BicepNetBaseCommand
    {
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            WriteObject(BicepWrapper.GetAccessToken());
        }
    }
}