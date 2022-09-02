using System;

namespace BicepNet.Core.Models;

public class BicepRepositoryModuleTag
{
    public string Name { get; set; }
    public string Digest { get; set; }
    public string Target { get; set; }

    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public BicepRepositoryModuleTag(string name, string digest, string target, DateTimeOffset createdOn, DateTimeOffset updatedOn)
    {
        Name = name;
        Digest = digest;
        Target = target;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
    }

    public override string ToString()
    {
        return Name;
    }
}
