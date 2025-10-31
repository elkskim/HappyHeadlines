using Microsoft.AspNetCore.Mvc;
using Moq;
using SubscriberService.Controllers;
using SubscriberService.Models.DTO;
using SubscriberService.Services;
using Xunit;

namespace SubscriberService.Tests.Controllers;

/// <summary>
/// Tests for SubscriberController. The API layer: the boundary between the
/// external chaos and our carefully structured domain. Here we verify that
/// HTTP requests are correctly translated to service calls, and that status
/// codes match the reality they represent.
/// As with the service tests, I annotate for future reference and clarity.
/// </summary>
public class SubscriberControllerTests
{
    private readonly Mock<ISubscriberAppService> _mockService;
    private readonly SubscriberController _controller;

    public SubscriberControllerTests()
    {
        _mockService = new Mock<ISubscriberAppService>();
        _controller = new SubscriberController(_mockService.Object);
    }

    /// <summary>
    /// GetSubscribers should return 200 OK with a list of subscribers.
    /// Even an empty list is a valid response; absence is not failure.
    /// </summary>
    [Fact]
    public async Task GetSubscribers_ReturnsOkWithSubscriberList()
    {
        // Arrange: A small collection of subscribers in the ether.
        var subscribers = new List<SubscriberReadDto>
        {
            new() { Id = 1, UserId = 1, Email = "user1@test.com", Region = "NA" },
            new() { Id = 2, UserId = 2, Email = "user2@test.com", Region = "EU" }
        };

        _mockService
            .Setup(s => s.GetSubscribers())
            .ReturnsAsync(subscribers);

        // Act: The request is made, the list is retrieved.
        var result = await _controller.GetSubscribers();

        // Assert: 200 OK, the expected shape of success.
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<SubscriberReadDto>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    /// <summary>
    /// GetSubscriber with a valid ID should return 200 OK with the subscriber.
    /// The subscriber exists; the API honors this truth with OK.
    /// </summary>
    [Fact]
    public async Task GetSubscriber_ExistingSubscriber_ReturnsOk()
    {
        // Arrange: A known subscriber in the database. A purveyor of Horrors to come.
        var subscriber = new SubscriberReadDto
        {
            Id = 1,
            UserId = 123,
            Email = "found@example.com",
            Region = "Asia"
        };

        _mockService
            .Setup(s => s.GetById(1))
            .ReturnsAsync(subscriber);

        // Act: The query is made, the subscriber emerges.
        var result = await _controller.GetSubscriber(1);

        // Assert: 200 OK - the subscriber is delivered as promised.
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<SubscriberReadDto>(okResult.Value);
        Assert.Equal("found@example.com", returnValue.Email);
    }

    /// <summary>
    /// GetSubscriber with a non-existent ID should return 404 Not Found.
    /// The subscriber does not exist. The API must reflect this absence.
    /// </summary>
    [Fact]
    public async Task GetSubscriber_NonExistentSubscriber_ReturnsNotFound()
    {
        // Arrange: The void returns nothing.
        _mockService
            .Setup(s => s.GetById(999))
            .ReturnsAsync((SubscriberReadDto?)null);

        // Act: The search yields emptiness.
        var result = await _controller.GetSubscriber(999);

        // Assert: 404 Not Found. Honest of its abscence.
        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Creating a valid subscriber should return 201 Created with location header.
    /// A new entity is born; the incorporeal API must celebrate with Created.
    /// </summary>
    [Fact]
    public async Task Subscribe_ValidSubscriber_ReturnsCreated()
    {
        // Arrange: A new subscriber arrives, foolishly seeking entry.
        var createDto = new SubscriberCreateDto
        {
            Email = "newuser@example.com",
            Region = "NA"
        };

        var createdSubscriber = new SubscriberReadDto
        {
            Id = 1,
            UserId = 456,
            Email = "newuser@example.com",
            Region = "NA"
        };

        _mockService
            .Setup(s => s.Create(It.IsAny<SubscriberCreateDto>()))
            .ReturnsAsync(createdSubscriber);

        // Act: The creation is executed.
        var result = await _controller.Subscribe(createDto);

        // Assert: 201 CreatedAtAction, the route to the new entity provided.
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetSubscriber), createdResult.ActionName);
        Assert.Equal(1, createdResult.RouteValues?["id"]);

        var returnValue = Assert.IsType<SubscriberReadDto>(createdResult.Value);
        Assert.Equal("newuser@example.com", returnValue.Email);
    }

    /// <summary>
    /// Updating an existing subscriber should return 200 OK with updated data.
    /// Change is acknowledged; the API returns the new truth.
    /// </summary>
    [Fact]
    public async Task Update_ExistingSubscriber_ReturnsOk()
    {
        // Arrange: An existing subscriber seeks to change.
        var updateDto = new SubscriberUpdateDto
        {
            Email = "updated@example.com",
            Region = "EU"
        };

        var updatedSubscriber = new SubscriberReadDto
        {
            Id = 1,
            UserId = 123,
            Email = "updated@example.com",
            Region = "EU"
        };

        _mockService
            .Setup(s => s.Update(1, It.IsAny<SubscriberUpdateDto>()))
            .ReturnsAsync(updatedSubscriber);

        // Act: The update is applied.
        var result = await _controller.Update(1, updateDto);

        // Assert: 200 OK. The change is confirmed.
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<SubscriberReadDto>(okResult.Value);
        Assert.Equal("updated@example.com", returnValue.Email);
    }

    /// <summary>
    /// Updating a non-existent subscriber should return 404 Not Found.
    /// You cannot change what does not exist.
    /// </summary>
    [Fact]
    public async Task Update_NonExistentSubscriber_ReturnsNotFound()
    {
        // Arrange: The phantom subscriber.
        var updateDto = new SubscriberUpdateDto
        {
            Email = "ghost@example.com",
            Region = "Nowhere"
        };

        _mockService
            .Setup(s => s.Update(999, It.IsAny<SubscriberUpdateDto>()))
            .ReturnsAsync((SubscriberReadDto?)null);

        // Act: The attempt to update nothing.
        var result = await _controller.Update(999, updateDto);

        // Assert: 404 Not Found - the void cannot be altered.
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Deleting an existing subscriber should return 200 OK.
    /// The subscriber is gone and a confirmation message is returned.
    /// </summary>
    [Fact]
    public async Task Unsubscribe_ExistingSubscriber_ReturnsOk()
    {
        // Arrange: A subscriber marked for removal.
        _mockService
            .Setup(s => s.Delete(1))
            .ReturnsAsync(true);

        // Act: The deletion is carried out.
        var result = await _controller.Unsubscribe(1);

        // Assert: 200 OK. The unsubscription is confirmed.
        Assert.IsType<OkObjectResult>(result);
    }

    /// <summary>
    /// Deleting a non-existent subscriber should return 404 Not Found.
    /// The subscriber was never there. The API cannot pretend otherwise.
    /// </summary>
    [Fact]
    public async Task Unsubscribe_NonExistentSubscriber_ReturnsNotFound()
    {
        // Arrange: Attempting to delete the void.
        _mockService
            .Setup(s => s.Delete(999))
            .ReturnsAsync(false);

        // Act: The futile deletion attempt.
        var result = await _controller.Unsubscribe(999);

        // Assert: 404 Not Found. You cannot delete what never was.
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
