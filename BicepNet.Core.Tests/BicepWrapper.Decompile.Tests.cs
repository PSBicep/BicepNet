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
        public void Decompile_ValidTemplatePath_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var validTemplatePath = "valid/path/to/template/file";

            // Act
            var result = bicepWrapper.Decompile(validTemplatePath);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, string>>(result);
        }

        [Fact]
        public async Task DecompileAsync_ValidTemplatePath_ShouldSucceed()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var validTemplatePath = "valid/path/to/template/file";

            // Act
            var result = await bicepWrapper.DecompileAsync(validTemplatePath);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Dictionary<string, string>>(result);
        }

        [Fact]
        public void Decompile_InvalidTemplatePath_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var invalidTemplatePath = "invalid/path/to/template/file";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => bicepWrapper.Decompile(invalidTemplatePath));
        }

        [Fact]
        public async Task DecompileAsync_InvalidTemplatePath_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var bicepWrapper = new BicepWrapper(null);
            var invalidTemplatePath = "invalid/path/to/template/file";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => bicepWrapper.DecompileAsync(invalidTemplatePath));
        }
    }
}
