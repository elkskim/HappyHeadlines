using Moq;
using NewsletterService.Features;
using Xunit;

namespace NewsletterService.Tests.Features;

/// <summary>
/// Tests for FeatureToggleService. We test that the boolean gate
/// correctly reads configuration and determines fate.
/// </summary>
public class FeatureToggleServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSubscriberServiceEnabled_ReturnsConfigValue(bool expectedValue)
    {
        // Arrange: Mock configuration to return specific value
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var mockSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        
        mockSection.Setup(s => s.Value).Returns(expectedValue.ToString());
        mockConfig
            .Setup(c => c.GetSection("Features:EnableSubscriberService"))
            .Returns(mockSection.Object);

        // Create a proper configuration dictionary
        var configDict = new Dictionary<string, string?>
        {
            ["Features:EnableSubscriberService"] = expectedValue.ToString()
        };
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var service = new FeatureToggleService(configuration);

        // Act: "A single boolean determines existence and non-existence."
        var result = service.IsSubscriberServiceEnabled();

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public void IsSubscriberServiceEnabled_MissingConfig_ReturnsTrue()
    {
        // Arrange: No configuration provided. Default to enabled.
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new FeatureToggleService(configuration);

        // Act: "In the absence of instruction, we proceed."
        var result = service.IsSubscriberServiceEnabled();

        // Assert: Default is true (enabled)
        Assert.True(result, "Service should default to enabled when configuration is missing.");
    }
}

