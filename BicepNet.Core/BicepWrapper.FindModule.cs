using Azure.Containers.ContainerRegistry;
using Bicep.Core.FileSystem;
using Bicep.Core.Modules;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Tracing;
using Bicep.Core.Workspaces;
using BicepNet.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IList<BicepRepository> FindModules(string inputFilePath = null, bool searchCache = false)
        {
            // Create credential and options
            var tokenFactory = new TokenCredentialFactory();
            var cred = tokenFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);
            var options = new ContainerRegistryClientOptions();
            options.Diagnostics.ApplySharedContainerRegistrySettings();
            options.Audience = new ContainerRegistryAudience(configuration.Cloud.ResourceManagerAudience);

            List<string> endpoints = new List<string>();

            // If path is specified
            if (!string.IsNullOrWhiteSpace(inputFilePath))
            {
                logger.LogInformation($"Searching file {inputFilePath} for endpoints");
                var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

                // Create separate configuration for the build, to account for custom rule changes
                var buildConfiguration = configurationManager.GetConfiguration(inputUri);
                var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

                // TODO: Change to allow for already restored modules
                var moduleReferences = moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration);
                // FullyQualifiedReferences are already unwrapped from potential local aliases
                var fullReferences = moduleReferences.Select(m => m.FullyQualifiedReference).Distinct();
                // Create objects with all module references grouped by endpoint
                // Format endpoint from "br:example.azurecr.io/repository/template:tag" to "example.azurecr.io"
                endpoints.AddRange(fullReferences.Select(r => r.Substring(3).Split('/').First()));

                if (endpoints.Count > 0)
                {
                    logger.LogInformation($"Found endpoints:\n{string.Join("\n", endpoints)}");
                }
                else
                {
                    logger.LogInformation("Found no endpoints in file");
                }
            }

            // If user specified to search through endpoints in cache, get them from directory names in cache
            if (searchCache)
            {
                logger.LogInformation($"Searching cache {OciCachePath} for endpoints");
                var directories = Directory.GetDirectories(OciCachePath);
                foreach (var directoryPath in directories)
                {
                    var directoryName = Path.GetFileName(directoryPath);
                    logger.LogInformation($"Found endpoint {directoryName}");
                    endpoints.Add(directoryName);
                }
            }

            var repos = new List<BicepRepository>();
            foreach (var endpoint in endpoints.Distinct())
            {
                try
                {
                    logger.LogInformation($"Searching endpoint {endpoint}");
                    var client = new ContainerRegistryClient(new Uri($"https://{endpoint}"), cred, options);
                    var repositoryNames = client.GetRepositoryNames();

                    logger.LogInformation($"Found modules:\n{string.Join("\n", repositoryNames)}");

                    foreach (var repositoryName in repositoryNames)
                    {
                        logger.LogInformation($"Searching module {repositoryName}");

                        // Create model repository to output
                        BicepRepository bicepRepository = new BicepRepository
                        {
                            Endpoint = endpoint,
                            Name = repositoryName
                        };

                        var repository = client.GetRepository(repositoryName);
                        var repositoryManifests = repository.GetManifestPropertiesCollection();

                        foreach (var moduleVersion in repositoryManifests)
                        {
                            logger.LogInformation($"Found versions of module {repositoryName}:\n{string.Join("\n", moduleVersion.Tags)}");
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
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Could not get modules from endpoint {endpoint}!");
                }
            }

            return repos;
        }
    }
}
