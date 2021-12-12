using Bicep.Core.Diagnostics;
using Bicep.Core.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BicepNet.Core.Models
{
    public class DiagnosticLogger
    {
        public List<DiagnosticEntry> diagnosticResult;
        public bool success;
        public DiagnosticLogger()
        {
            diagnosticResult = new List<DiagnosticEntry>();
            success = true;
        }

        public void LogDiagnostics(Uri fileUri, IDiagnostic diagnostic, ImmutableArray<int> lineStarts)
        {
            diagnosticResult.Add(
                    new DiagnosticEntry(
                        fileUri.LocalPath,
                        TextCoordinateConverter.GetPosition(lineStarts, diagnostic.Span.Position),
                        (DiagnosticLevel)diagnostic.Level,
                        diagnostic.Code,
                        diagnostic.Message
                    )
                    );
            success &= diagnostic.Level != Bicep.Core.Diagnostics.DiagnosticLevel.Error;
        }
    }
}