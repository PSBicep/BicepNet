using BicepNet.Core;
using System;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Set, "BicepNetCredential")]
    [CmdletBinding(DefaultParameterSetName = "Interactive")]
    public class SetBicepNetCredentialCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ParameterSetName = "Token")]
        [ValidateNotNullOrEmpty]
        public string AccessToken { get; set; }

        [Parameter(ParameterSetName = "Interactive", Position = 0)]
        [ValidateNotNullOrEmpty]
        public string TenantId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            switch (ParameterSetName)
            {
                case "Token":
                    BicepWrapper.SetAuthentication(AccessToken);
                    break;
                case "Interactive":
                    BicepWrapper.SetAuthentication(null, TenantId);
                    break;
                default:
                    throw new Exception("Not a valid parameter set!");
            }
        }
    }
}