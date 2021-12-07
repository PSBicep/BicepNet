using Azure.Containers.ContainerRegistry;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Tracing;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IEnumerable<string> FindModules(string inputFilePath)
        {
            var inputUri = PathHelper.FilePathToFileUrl(inputFilePath);

            // Create separate configuration for the build, to account for custom rule changes
            var buildConfiguration = configurationManager.GetConfiguration(inputUri);

            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, inputUri, buildConfiguration);

            logger.LogTrace(JsonSerializer.Serialize(sourceFileGrouping.ModulesToRestore));

            var moduleReferences = moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration);
            foreach (var item in moduleReferences)
            {
                logger.LogTrace(item.FullyQualifiedReference);
            }

            var tokenFactory = new TokenCredentialFactory();
            var cred = tokenFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);

            var options = new ContainerRegistryClientOptions();
            options.Diagnostics.ApplySharedContainerRegistrySettings();
            options.Audience = new ContainerRegistryAudience(configuration.Cloud.ResourceManagerAudience);

            var client = new ContainerRegistryClient(new Uri("pwrops.azurecr.io"), cred, options);

            return client.GetRepositoryNames().ToList();
        }
    }
}
