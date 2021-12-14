using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS.Commands
{
    [Cmdlet(VerbsCommon.Get, "BicepNetCachePath")]
    [CmdletBinding]
    public class GetBicepNetCachePathCommand : BicepNetBaseCommand
    {
        [Parameter(Mandatory = true, ParameterSetName="br")]
        public SwitchParameter Oci { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ts")]
        public SwitchParameter TemplateSpecs { get; set; }

        protected override void EndProcessing()
        {
            string result = "";
            if (Oci.IsPresent)
            {
                result = BicepWrapper.OciCachePath;
            }
            else if (TemplateSpecs.IsPresent)
            {
                result = BicepWrapper.TemplateSpecsCachePath;
            }
            WriteObject(result);
        }
    }
}