using BicepNet.Core;
using System.Management.Automation;

namespace BicepNet.PS
{
    [Cmdlet(VerbsData.ConvertTo, "BicepNetFile")]
    public class ConvertToBicepNetFile : PSCmdlet
    {

        public ConvertToBicepNetFile()
        {
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        protected override void ProcessRecord()
        {
            var result = BicepWrapper.Decompile(Path);
            WriteObject(result);
        }

        protected override void EndProcessing()
        {

        }
    }
}