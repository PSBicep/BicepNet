using System.Collections.Generic;

namespace BicepNet.Core.Models;

public class BicepRepository
{
    public string Name { get; }
    public string Endpoint { get; }
    public IList<BicepRepositoryModule> ModuleVersions { get; set; } = new List<BicepRepositoryModule>();

    public BicepRepository(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }

    public override string ToString() => Name;
}
