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
        public void Build_ValidInput_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var validBicepPath = "valid/path/to/bicep/file";

            // Act
            var result = bicepWrapper.Build(validBicepPath);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<string>>(result);
        }

        [Fact]
        public async Task BuildAsync_ValidInput_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var validBicepPath = "valid/path/to/bicep/file";

            // Act
            var result = await bicepWrapper.BuildAsync(validBicepPath);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<string>>(result);
        }

        [Fact]
        public void Build_InvalidInput_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var invalidBicepPath = "invalid/path/to/bicep/file";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => bicepWrapper.Build(invalidBicepPath));
        }

        [Fact]
        public async Task BuildAsync_InvalidInput_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var invalidBicepPath = "invalid/path/to/bicep/file";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => bicepWrapper.BuildAsync(invalidBicepPath));
        }
    }
}
