using Azure.Containers.ContainerRegistry;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Tracing;
using Bicep.Core.Workspaces;
using BicepNet.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static object FindModules(string inputFilePath)
        {
            var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

            // Create separate configuration for the build, to account for custom rule changes
            var buildConfiguration = configurationManager.GetConfiguration(inputUri);
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

            // TODO: Change to allow for already restored modules
            var moduleReferences = moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration);
            // FullyQualifiedReferences are already unwrapped from potential local aliases
            var fullReferences = moduleReferences.Select(m => m.FullyQualifiedReference).Distinct();
            // Create objects with all module references grouped by endpoint
            // Format endpoint from "br:example.azurecr.io/repository/template:tag" to "https://example.azurecr.io"
            var formattedReferences = fullReferences.GroupBy(
                r => $"https://{r.Substring(3).Split('/').First()}",
                r => r,
                (key, m) => new { Endpoint = key, ModuleReferences = m.ToList() }
            );

            // Create credential and options
            var tokenFactory = new TokenCredentialFactory();
            var cred = tokenFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);
            var options = new ContainerRegistryClientOptions();
            options.Diagnostics.ApplySharedContainerRegistrySettings();
            options.Audience = new ContainerRegistryAudience(configuration.Cloud.ResourceManagerAudience);

            List<BicepRepository> repos = new List<BicepRepository>();
            foreach (var referenceCollection in formattedReferences)
            {
                logger.LogInformation($"Searching endpoint {referenceCollection.Endpoint}");
                var client = new ContainerRegistryClient(new Uri(referenceCollection.Endpoint), cred, options);
                var repositoryNames = client.GetRepositoryNames();

                foreach (var repositoryName in repositoryNames)
                {
                    logger.LogInformation($"Searching repository {repositoryName}");

                    // Create model repository to output
                    BicepRepository bicepRepository = new BicepRepository
                    {
                        Endpoint = referenceCollection.Endpoint,
                        Name = repositoryName
                    };

                    var repository = client.GetRepository(repositoryName);
                    var repositoryManifests = repository.GetManifestPropertiesCollection();

                    foreach (var moduleVersion in repositoryManifests)
                    {
                        bicepRepository.ModuleVersions.Add(new BicepRepositoryModule
                        {
                            Digest = moduleVersion.Digest,
                            Repository = repositoryName,
                            Tags = moduleVersion.Tags,
                            Created = moduleVersion.CreatedOn
                        });
                    }

                    repos.Add(bicepRepository);
                }
            }

            return repos;
        }
    }
}
