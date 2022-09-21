using BicepNet.Core.Azure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BicepNet.Core
{
    public partial class BicepWrapper
    {
        public static IDictionary<string, string> ExportResources(string[] ids) =>
            joinableTaskFactory.Run(() => ExportResourcesAsync(ids));
        public static async Task<IDictionary<string, string>> ExportResourcesAsync(string[] ids)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            var taskList = new List<Task<(string resourceName, string template)>>();
            foreach (string id in ids)
            {
                taskList.Add(ExportResourceAsync(id));
            }
            foreach ((string name, string template) in await Task.WhenAll(taskList))
            {
                result.Add(name, template);
            }
            return result;
        }
        private static async Task<(string resourceName, string template)> ExportResourceAsync(string id)
        {
            var resourceId = AzureHelpers.ValidateResourceId(id);
            resourceId.Deconstruct(
                out _,
                out string fullyQualifiedType,
                out _,
                out _,
                out _
            );
            var matchedType = BicepHelper.ResolveBicepTypeDefinition(fullyQualifiedType, azResourceTypeLoader, logger);

            JsonElement resource;
            try
            {
                var cancellationToken = new CancellationToken();
                resource = await azResourceProvider.GetGenericResource(configuration, resourceId, matchedType.ApiVersion, cancellationToken);
            }
            catch (Exception exception)
            {
                var message = $"Failed to fetch resource '{resourceId}' with API version {matchedType.ApiVersion}: {exception}";
                logger?.LogError("{message}", message);
                throw new Exception(message);
            }

            string template = azResourceProvider.GenerateBicepTemplate(resourceId, matchedType, resource);
            var name = AzureHelpers.GetResourceFriendlyName(id);
            return (name, template);
        }
    }
}