using BicepNet.Core;
using System;
using Xunit;

namespace BicepNet.Core.Tests
{
    public partial class BicepWrapperTests
    {
        [Fact]
        public void ConvertResourceToBicep_ValidResourceId_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var validResourceId = "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}";

            // Act
            var result = bicepWrapper.ConvertResourceToBicep(validResourceId, "{}");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("resource", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertResourceToBicep_InvalidResourceId_ShouldThrowArgumentException()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var invalidResourceId = "invalidResourceId";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => bicepWrapper.ConvertResourceToBicep(invalidResourceId, "{}"));
        }

        [Fact]
        public void ConvertResourceToBicep_NullResourceId_ShouldThrowArgumentNullException()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => bicepWrapper.ConvertResourceToBicep(null, "{}"));
        }
    }
}
