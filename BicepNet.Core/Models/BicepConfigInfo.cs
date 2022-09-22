namespace BicepNet.Core.Models;

public class BicepConfigInfo
{
    public string Path { get; }
    public string Config { get; }

    public BicepConfigInfo(string path, string config)
    {
        Path = path;
        Config = config;
    }

    public override string ToString()
    {
        return Path;
    }
}
