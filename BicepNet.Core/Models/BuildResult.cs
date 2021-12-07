using System.Collections.Generic;

namespace BicepNet.Core.Models
{
    public class BuildResult
    {
        public BuildResult(ICollection<string> template, IEnumerable<DiagnosticEntry> diagnostic)
        {
            Template = template;
            Diagnostic = diagnostic;
        }

        public ICollection<string> Template { get; }
        public IEnumerable<DiagnosticEntry> Diagnostic { get; }

    }
}
