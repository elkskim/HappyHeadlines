using Moq;
using SubscriberDatabase.Data;
using SubscriberDatabase.Model;
using SubscriberService.Messaging;
using SubscriberService.Models.DTO;
using SubscriberService.Services;
using Xunit;

namespace SubscriberService.Tests.Services;

/// <summary>
/// Tests for SubscriberAppService. We test the service layer in isolation,
/// mocking the repository and publisher. This is all to prevent booting the whole app and sending lil messages
/// into the void just to see if they go through. We want fast, reliable tests that
/// confirm our logic without external dependencies.
/// As I am terribly new to testing, I have made comments so that I remember these for future implementations.
/// </summary>
public class SubscriberAppServiceTests
{
    private readonly Mock<ISubscriberRepository> _mockRepository;
    private readonly Mock<SubscriberPublisher> _mockPublisher;
    private readonly SubscriberAppService _service;

    public SubscriberAppServiceTests()
    {
        // We prepare our mocks, our test doubles, our stand-ins for reality.
        _mockRepository = new Mock<ISubscriberRepository>();
        _mockPublisher = new Mock<SubscriberPublisher>();
        _service = new SubscriberAppService(_mockRepository.Object, _mockPublisher.Object);
    }

    /// <summary>
     /// We expect that creating a subscriber results in a published event.
     /// Return the created subscriber as a DTO or perish.
     /// </summary>
    [Fact]
    public async Task Create_ValidSubscriber_ReturnsSubscriberReadDto()
    {
        // Arrange: The setup, the preparation, the calm before the test.
        var createDto = new SubscriberCreateDto
        {
            UserId = 1,
            Email = "test@example.com",
            Region = "Europe"
        };

        var expectedSubscriber = new Subscriber
        {
            Id = 1,
            UserId = 1,
            Email = "test@example.com",
            Region = "Europe",
            SubscribedOn = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<Subscriber>()))
            .ReturnsAsync(expectedSubscriber);

        // Act: The execution, the moment of truth, the strike.
        var result = await _service.Create(createDto);

        // Assert: A premonition; Time to decide the outcome, to see if the stars align.
        Assert.NotNull(result);
        Assert.Equal(expectedSubscriber.Id, result.Id);
        Assert.Equal(expectedSubscriber.Email, result.Email);
        
        // Verify the publisher was called; the message sent into the void.
        _mockPublisher.Verify(
            p => p.PublishSubscriberAdded(It.IsAny<SubscriberService.Models.Events.SubscriberAddedEvent>()),
            Times.Once,
            "The event must be published, even if no one listens.");
    }

    /// <summary>
     /// Retrieving an existing subscriber by ID should return the subscriber.
     /// </summary>
    [Fact]
    public async Task GetById_ExistingSubscriber_ReturnsSubscriber()
    {
        // Arrange: The setup, the known entity in the sea of uncertainty.
        var existingSubscriber = new Subscriber
        {
            Id = 1,
            UserId = 1,
            Email = "existing@example.com",
            Region = "Asia",
            SubscribedOn = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingSubscriber);

        // Act: The action, the retrieval, the seeking of truth.
        var result = await _service.GetById(1);

        // Assert: Something beckons in the dark.
        Assert.NotNull(result);
        Assert.Equal(existingSubscriber.Email, result.Email);
    }

    /// <summary>
     /// Subscriber is nullable. Make sure this is handled gracefully.
     /// </summary>
    [Fact]
    public async Task GetById_NonExistentSubscriber_ReturnsNull()
    {
        // Arrange: The void stares back.
        _mockRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Subscriber?)null); //Subscriber is nullable, make it so explicitly.

        // Act: Not aught of use is found.
        var result = await _service.GetById(999);

        // Assert: In the emptiness, there is nothing.
        Assert.Null(result);
    }

    /// <summary>
     /// Existing subscribers must be able to update by Email and Region, as defined by the UpdateDto.
     /// An update event must be published.
     /// </summary>
    [Fact]
    public async Task Update_ExistingSubscriber_PublishesUpdateEvent()
    {
        // Arrange: The existing subscriber, the one to be changed.
        var existingSubscriber = new Subscriber
        {
            Id = 1,
            UserId = 1,
            Email = "old@example.com",
            Region = "Europe",
            SubscribedOn = DateTime.UtcNow
        };
        // How shall we change thee? Let me count the ways. Email and Region as UpdateDto dictates.
        var updateDto = new SubscriberUpdateDto
        {
            Email = "new@example.com",
            Region = "Asia"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingSubscriber);

        _mockRepository
            .Setup(r => r.UpdateAsync(1, It.IsAny<Subscriber>()))
            .ReturnsAsync(existingSubscriber);

        // Act: Grant this one a new lease on life by Email and Region.
        var result = await _service.Update(1, updateDto);

        // Assert: Truth emerges from the transformation.
        Assert.NotNull(result);
        _mockPublisher.Verify(
            p => p.PublishSubscriberUpdated(It.IsAny<SubscriberService.Models.Events.SubscriberUpdatedEvent>()),
            Times.Once,
            "The update must be broadcast, for those who care to listen.");
    }
    
    /// <summary>
    /// Attempting to update a subscriber that does not exist should return null.
    /// You cannot change what was never there.
    /// </summary>
    [Fact]
    public async Task Update_NonExistentSubscriber_ReturnsNull()
    {
        // Arrange
        var updateDto = new SubscriberUpdateDto
        {
            Email = "ghost@example.com",
            Region = "Nowhere"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Subscriber?)null);

        // Act
        var result = await _service.Update(999, updateDto);

        // Assert
        Assert.Null(result);
        _mockPublisher.Verify(
            p => p.PublishSubscriberUpdated(It.IsAny<SubscriberService.Models.Events.SubscriberUpdatedEvent>()),
            Times.Never,
            "No event for the phantom update.");
    }

    /// <summary>
     /// Deleting an existing subscriber must publish a removal event.
     /// The subscriber is gone, but the message persists.
     /// </summary>
    [Fact]
    public async Task Delete_ExistingSubscriber_PublishesRemovalEvent()
    {
        // Arrange
        var existingSubscriber = new Subscriber
        {
            Id = 1,
            UserId = 1,
            Email = "doomed@example.com",
            Region = "Europe",
            SubscribedOn = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingSubscriber);

        _mockRepository
            .Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act: "More dust, more ashes, more disappointment."
        var result = await _service.Delete(1);

        // Assert
        Assert.True(result);
        _mockPublisher.Verify(
            p => p.PublishSubscriberRemoved(It.IsAny<SubscriberService.Models.Events.SubscriberRemovedEvent>()),
            Times.Once,
            "The removal must be announced. The subscriber is gone, but the message persists.");
    }

    /// <summary>
     /// Attempting to delete a non-existent subscriber should return false and not publish an event.
     /// If this fails, we risk announcing the deletion of that which never was.
        /// </summary>
    [Fact]
    public async Task Delete_NonExistentSubscriber_ReturnsFalse()
    {
        // Arrange: Attempting to delete what does not exist.
        _mockRepository
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Subscriber?)null); // As mentioned earlier, Subscriber is nullable and must be cast explicitly here,

        // Act
        var result = await _service.Delete(999);

        // Assert: "The void cannot be deleted, for it is already nothing."
        Assert.False(result);
        _mockPublisher.Verify(
            p => p.PublishSubscriberRemoved(It.IsAny<SubscriberService.Models.Events.SubscriberRemovedEvent>()),
            Times.Never,
            "No event is published for that which never was.");
    }

    /// <summary>
    /// If the repository delete operation fails, we must not publish the removal event.
    /// The subscriber remains, stubbornly clinging to existence.
    /// </summary>
    [Fact]
    public async Task Delete_RepositoryFailure_ReturnsFalseAndDoesNotPublish()
    {
        // Arrange
        var existingSubscriber = new Subscriber
        {
            Id = 1,
            UserId = 1,
            Email = "stubborn@example.com",
            Region = "Limbo",
            SubscribedOn = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(existingSubscriber);

        _mockRepository
            .Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(false); // Delete operation fails

        // Act
        var result = await _service.Delete(1);

        // Assert
        Assert.False(result);
        _mockPublisher.Verify(
            p => p.PublishSubscriberRemoved(It.IsAny<SubscriberService.Models.Events.SubscriberRemovedEvent>()),
            Times.Never,
            "The subscriber survives. No event is sent.");
    }
}

