using Bicep.Core.Workspaces;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static string BuiltInConfigurationResourcePath = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\bicepconfig.json";

        public static string BicepVersion { get; } = FileVersionInfo.GetVersionInfo(typeof(Workspace).Assembly.Location).FileVersion;
    }
}
