using System;
using System.Collections.Generic;
using System.Linq;

namespace BicepNet.Core.Models;

public class BicepRepositoryModule
{
    public string Digest { get; set; }
    public string Repository { get; set; }
    public IList<BicepRepositoryModuleTag> Tags { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public BicepRepositoryModule(string digest, string repository, IList<BicepRepositoryModuleTag> tags, DateTimeOffset createdOn, DateTimeOffset updatedOn)
    {
        Digest = digest;
        Repository = repository;
        Tags = tags;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
    }

    public override string ToString()
    {
        return string.Join(", ", Tags.OrderByDescending(t => t.UpdatedOn).Select(t => t.ToString()));
    }
}
