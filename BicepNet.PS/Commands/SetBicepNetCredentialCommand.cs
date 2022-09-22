using BicepNet.Core;
using BicepNet.Core.Configuration;
using System;
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
            BicepWrapper.SetAccessToken(AccessToken);
        }
    }
}