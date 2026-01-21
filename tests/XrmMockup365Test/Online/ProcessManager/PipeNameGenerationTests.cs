using System.Linq;
using Xunit;
using DG.Tools.XrmMockup.Online;

namespace DG.XrmMockupTest.Online.ProcessManager
{
    /// <summary>
    /// Tests for deterministic pipe name generation.
    /// </summary>
    [Trait("Category", "Unit")]
    public class PipeNameGenerationTests
    {
        [Fact]
        public void SameUrl_ProducesSamePipeName()
        {
            // Arrange
            var url = "https://myorg.crm.dynamics.com";

            // Act
            var name1 = ProxyProcessManager.GeneratePipeName(url);
            var name2 = ProxyProcessManager.GeneratePipeName(url);

            // Assert
            Assert.Equal(name1, name2);
        }

        [Fact]
        public void DifferentCasing_ProducesSamePipeName()
        {
            // Arrange
            var url1 = "https://myorg.crm.dynamics.com";
            var url2 = "HTTPS://MYORG.CRM.DYNAMICS.COM";
            var url3 = "Https://MyOrg.Crm.Dynamics.Com";

            // Act
            var name1 = ProxyProcessManager.GeneratePipeName(url1);
            var name2 = ProxyProcessManager.GeneratePipeName(url2);
            var name3 = ProxyProcessManager.GeneratePipeName(url3);

            // Assert - all should produce the same name (case-insensitive)
            Assert.Equal(name1, name2);
            Assert.Equal(name1, name3);
        }

        [Fact]
        public void DifferentUrls_ProduceDifferentNames()
        {
            // Arrange
            var url1 = "https://org1.crm.dynamics.com";
            var url2 = "https://org2.crm.dynamics.com";
            var url3 = "https://org1.crm4.dynamics.com"; // Different region

            // Act
            var name1 = ProxyProcessManager.GeneratePipeName(url1);
            var name2 = ProxyProcessManager.GeneratePipeName(url2);
            var name3 = ProxyProcessManager.GeneratePipeName(url3);

            // Assert
            Assert.NotEqual(name1, name2);
            Assert.NotEqual(name1, name3);
            Assert.NotEqual(name2, name3);
        }

        [Fact]
        public void PipeName_HasExpectedFormat()
        {
            // Arrange
            var url = "https://myorg.crm.dynamics.com";

            // Act
            var name = ProxyProcessManager.GeneratePipeName(url);

            // Assert - should be "XrmMockupProxy_" + 16 hex chars
            Assert.StartsWith("XrmMockupProxy_", name);
            Assert.Equal(31, name.Length); // 15 (prefix) + 16 (hex)

            // Verify the hash part is valid hex
            var hashPart = name.Substring(15);
            Assert.All(hashPart, c => Assert.True(
                char.IsDigit(c) || (c >= 'A' && c <= 'F'),
                string.Format("Character '{0}' is not a valid hex character", c)));
        }

        [Fact]
        public void PipeName_IsDeterministic()
        {
            // Arrange
            var url = "https://contoso.crm.dynamics.com";

            // Act - generate name multiple times
            var names = Enumerable.Range(0, 100)
                .Select(_ => ProxyProcessManager.GeneratePipeName(url))
                .ToList();

            // Assert - all should be identical
            Assert.All(names, n => Assert.Equal(names[0], n));
        }

        [Fact]
        public void PipeName_HandlesSpecialCharacters()
        {
            // Arrange
            var url = "https://my-org_test.crm.dynamics.com/api/data/v9.2?param=value&other=123";

            // Act
            var name = ProxyProcessManager.GeneratePipeName(url);

            // Assert - should still produce valid format
            Assert.StartsWith("XrmMockupProxy_", name);
            Assert.Equal(31, name.Length);
        }

        [Fact]
        public void PipeName_HandlesTrailingSlash()
        {
            // Arrange
            var url1 = "https://myorg.crm.dynamics.com";
            var url2 = "https://myorg.crm.dynamics.com/";

            // Act
            var name1 = ProxyProcessManager.GeneratePipeName(url1);
            var name2 = ProxyProcessManager.GeneratePipeName(url2);

            // Assert - these will be different (trailing slash is significant)
            // This is acceptable behavior - just documenting it
            Assert.NotEqual(name1, name2);
        }
    }
}
