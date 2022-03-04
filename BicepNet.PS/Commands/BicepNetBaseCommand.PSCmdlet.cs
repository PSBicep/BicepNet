using BicepNet.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    public partial class BicepNetBaseCommand : PSCmdlet
    {
        private string name;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            BicepWrapper.Initialize(this);
            name = MyInvocation.InvocationName;
        }
    }
}
