// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json;
using System.Threading.Tasks;
using Bicep.Core.Configuration;
using System.Threading;

namespace BicepNet.Core.Azure;

public interface IAzResourceProvider
{
    public record AzResourceIdentifier(
        string FullyQualifiedId,
        string FullyQualifiedType,
        string FullyQualifiedName,
        string UnqualifiedName,
        string subscriptionId);

    Task<JsonElement> GetGenericResourceAsync(RootConfiguration configuration, AzResourceIdentifier resourceId, string? apiVersion, CancellationToken cancellationToken);
}
