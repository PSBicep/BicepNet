using Bicep.Core.Workspaces;
using System.Diagnostics;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion;

    }
}
