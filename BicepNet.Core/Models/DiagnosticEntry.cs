namespace BicepNet.Core.Models
{
    public class DiagnosticEntry
    {
        public DiagnosticEntry(string localPath, (int line, int character) position, DiagnosticLevel level, string code, string message)
        {
            LocalPath = localPath;
            Position = position;
            Level = level;
            Code = code;
            Message = message;
        }

        public string LocalPath { get; }
        public (int line, int character) Position { get; }
        public DiagnosticLevel Level { get; }
        public string Code { get; }
        public string Message { get; }
    }
}
