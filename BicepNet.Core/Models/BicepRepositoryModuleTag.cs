using System;

namespace BicepNet.Core.Models;

public class BicepRepositoryModuleTag
{
    public string Name { get; set; }
    public string Digest { get; set; }
    public string Target { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
