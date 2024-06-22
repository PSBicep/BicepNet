using BicepNet.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BicepNet.Core.Tests
{
    public partial class BicepWrapperTests
    {
        [Fact]
        public async Task ExportResourceAsync_ValidResourceId_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var validResourceIds = new string[] { "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}" };

            // Act
            var result = await bicepWrapper.ExportResourcesAsync(validResourceIds);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }

        [Fact]
        public async Task ExportResourceAsync_InvalidResourceId_ShouldFail()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var invalidResourceIds = new string[] { "invalidResourceId" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => bicepWrapper.ExportResourcesAsync(invalidResourceIds));
        }

        [Fact]
        public async Task ExportResourcesAsync_MultipleResourceIds_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var resourceIds = new string[]
            {
                "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}",
                "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{accountName}"
            };

            // Act
            var result = await bicepWrapper.ExportResourcesAsync(resourceIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(resourceIds.Length, result.Count);
        }
    }
}
