using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Set, "BicepNetCredential")]
    [CmdletBinding()]
    public class SetBicepNetCredentialCommand : BicepNetBaseCommand
    {
        [Parameter(ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string AccessToken { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (MyInvocation.BoundParameters.ContainsKey("AccessToken"))
            {
                BicepWrapper.SetAuthentication(AccessToken);
            }
            else
            {
                // Interactive login
                BicepWrapper.SetAuthentication();
            }
        }
    }
}