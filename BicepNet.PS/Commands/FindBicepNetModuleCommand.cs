using BicepNet.Core;
using System;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Find, "BicepNetModule")]
    public class FindBicepNetModuleCommand : BicepNetBaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Registry")]
        [ValidateNotNullOrEmpty]
        public string Registry { get; set; }

        [Parameter(ParameterSetName = "Cache")]
        public SwitchParameter Cache { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Path":
                    WriteObject(BicepWrapper.FindModules(Path, false));
                    break;
                case "Registry":
                    WriteObject(BicepWrapper.FindModules(Registry, true));
                    break;
                case "Cache":
                    WriteObject(BicepWrapper.FindModules());
                    break;
                default:
                    break;
            }
        }
    }
}