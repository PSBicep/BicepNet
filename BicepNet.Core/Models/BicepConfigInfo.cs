namespace BicepNet.Core.Models
{
    public class BicepConfigInfo
    {
        public string Path { get; set; }
        public string Config { get; set; }

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
}
