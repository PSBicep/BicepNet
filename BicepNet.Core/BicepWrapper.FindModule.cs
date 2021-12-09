using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Workspaces;
using Azure.Containers.ContainerRegistry;
using Bicep.Core.Modules;
using Bicep.Core.Registry.Auth;
using System.Linq;
using System;
using System.Collections.Generic;
using Bicep.Core.Tracing;
using Azure.Identity;

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

            var moduleReferences = moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, buildConfiguration).Select(r => (OciArtifactModuleReference)r);
            
            var tokenFactory = new TokenCredentialFactory();
            var cred = tokenFactory.CreateChain(configuration.Cloud.CredentialPrecedence, configuration.Cloud.ActiveDirectoryAuthorityUri);
            
            var options = new ContainerRegistryClientOptions();
            options.Diagnostics.ApplySharedContainerRegistrySettings();
            options.Audience = new ContainerRegistryAudience(configuration.Cloud.ResourceManagerAudience);
            
            var client = new ContainerRegistryClient(new Uri("https://pwrops.azurecr.io"), cred, options);

            return client.GetRepositoryNames().ToList();
        }
    }
}
