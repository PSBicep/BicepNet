using System;

namespace BicepNet.Core.Models;

public class BicepRepositoryModuleTag
{
    public string Name { get; }
    public string Digest { get; }
    public string Target { get; }

    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset UpdatedOn { get; }

    public BicepRepositoryModuleTag(string name, string digest, string target, DateTimeOffset createdOn, DateTimeOffset updatedOn)
    {
        Name = name;
        Digest = digest;
        Target = target;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
    }

    public override string ToString() => Name;
}
