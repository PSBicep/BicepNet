﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BicepNet.Core.Models;

public class BicepRepositoryModule
{
    public string Digest { get; }
    public string Repository { get; }
    public List<BicepRepositoryModuleTag> Tags { get; }
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset UpdatedOn { get; }

    public BicepRepositoryModule(string digest, string repository, List<BicepRepositoryModuleTag> tags, DateTimeOffset createdOn, DateTimeOffset updatedOn)
    {
        Digest = digest;
        Repository = repository;
        Tags = tags;
        CreatedOn = createdOn;
        UpdatedOn = updatedOn;
    }

    public override string ToString()
    {
        return Tags.Any() ? string.Join(", ", Tags.OrderByDescending(t => t.UpdatedOn).Select(t => t.ToString())) : "null";
    }
}
