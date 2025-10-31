using Microsoft.AspNetCore.Http;
using Moq;
using SubscriberService.Features;
using SubscriberService.Middleware;
using Xunit;

namespace SubscriberService.Tests.Middleware;

/// <summary>
/// Tests for ServiceToggleMiddleware. The gatekeeper that determines
/// existence and non-existence with a single boolean check.
/// </summary>
public class ServiceToggleMiddlewareTests
{
    [Fact]
    public async Task Invoke_ServiceEnabled_CallsNextMiddleware()
    {
        // Arrange: The gate is open. The path is clear.
        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle
            .Setup(f => f.IsSubscriberServiceEnabled())
            .Returns(true);

        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ServiceToggleMiddleware(next, mockFeatureToggle.Object);

        // Act: "The way is lit. The path is clear."
        await middleware.Invoke(context);

        // Assert
        Assert.True(nextCalled, "The next middleware must be called when the service is enabled.");
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_ServiceDisabled_Returns503()
    {
        // Arrange: The gate is closed. The service does not exist in this timeline.
        var mockFeatureToggle = new Mock<IFeatureToggleService>();
        mockFeatureToggle
            .Setup(f => f.IsSubscriberServiceEnabled())
            .Returns(false);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ServiceToggleMiddleware(next, mockFeatureToggle.Object);

        // Act: "A single boolean toggles existence and non-existence, being and void."
        await middleware.Invoke(context);

        // Assert
        Assert.False(nextCalled, "The next middleware must NOT be called when disabled.");
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);

        // Verify the response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseText = await reader.ReadToEndAsync();
        Assert.Equal("SubscriberService is disabled", responseText);
    }
}

