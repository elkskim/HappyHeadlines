using Microsoft.Extensions.Configuration;
using Moq;
using SubscriberService.Features;
using Xunit;

namespace SubscriberService.Tests.Features;

/// <summary>
/// Tests for FeatureToggleService. The oracle that consults the configuration
/// files to see what is and what is not.
/// </summary>
public class FeatureToggleServiceTests
{
    /// <summary>
    /// Verifies that when the configuration explicitly sets EnableSubscriberService
    /// to true, the feature toggle correctly returns true.
    /// The service exists. The path is clear.
    /// </summary>
    [Fact]
    public void IsSubscriberServiceEnabled_ConfiguredTrue_ReturnsTrue()
    {
        // Arrange: The gate is open.
        var mockConfig = new Mock<IConfiguration>();
        mockConfig
            .Setup(c => c["Features:EnableSubscriberService"])
            .Returns("true");

        var service = new FeatureToggleService(mockConfig.Object);

        // Act: There is light in the dark.
        var result = service.IsSubscriberServiceEnabled();

        // Assert: Let us decide on the truth.
        Assert.True(result, "Feature should be enabled when config is true");
    }

    /// <summary>
    /// Verifies that when the configuration explicitly disables the feature,
    /// the toggle correctly returns false.
    /// The service does not exist in this time or place.
    /// </summary>
    [Fact]
    public void IsSubscriberServiceEnabled_ConfiguredFalse_ReturnsFalse()
    {
        // Arrange: Rejection, denial, non-existence.
        var mockConfig = new Mock<IConfiguration>();
        mockConfig
            .Setup(c => c["Features:EnableSubscriberService"])
            .Returns("false");

        var service = new FeatureToggleService(mockConfig.Object);

        // Act: The road ahead is blocked.
        var result = service.IsSubscriberServiceEnabled();

        // Assert: The flag is down, the gate is closed.
        Assert.False(result, "Feature should be disabled when config is false");
    }

    /// <summary>
    /// Verifies that when the configuration key is missing entirely,
    /// the service falls back to its default value of true.
    /// In the absence of explicit instruction, we assume existence.
    /// </summary>
    [Fact]
    public void IsSubscriberServiceEnabled_MissingConfig_DefaultsToTrue()
    {
        // Arrange: The unknown. The undefined. The absent.
        var mockConfig = new Mock<IConfiguration>();
        mockConfig
            .Setup(c => c["Features:EnableSubscriberService"])
            .Returns((string?)null);  // Like earlier, remember to be explicit in the cast.

        var service = new FeatureToggleService(mockConfig.Object);

        // Act: The path forward is uncertain.
        var result = service.IsSubscriberServiceEnabled();

        // Assert: If doubt remains, Let us choose existence.
        Assert.True(result, "Feature should default to enabled when config is missing");
    }
}
