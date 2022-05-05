using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    public partial class BicepNetBaseCommand : PSCmdlet
    {
        private string name;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // Try to get the value of the Token parameter
            // Only some commands have this parameter, so it's allowed to fail
            // Since object and string can be null, we can still provide it to BicepWrapper
            object token;
            MyInvocation.BoundParameters.TryGetValue("Token", out token);

            BicepWrapper.Initialize(this, (string)token);
            name = MyInvocation.InvocationName;
        }
    }
}
