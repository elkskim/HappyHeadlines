using System.Text;
using System.Text.Json;
using Moq;
using RabbitMQ.Client;
using SubscriberService.Messaging;
using SubscriberService.Models.Events;
using Xunit;

namespace SubscriberService.Tests.Messaging;

/// <summary>
/// Tests for SubscriberPublisher. The messenger that carries events into the void,
/// serializing our domain events to JSON and casting them into the RabbitMQ abyss.
/// We verify that the messages are correctly formatted, routed, and marked for persistence.
/// </summary>
public class SubscriberPublisherTests
{
    private readonly Mock<IChannel> _mockChannel;
    private readonly SubscriberPublisher _publisher;

    public SubscriberPublisherTests()
    {
        _mockChannel = new Mock<IChannel>();
        
        // NOTE: Do not attempt to Setup extension methods (ExchangeDeclareAsync) with Moq;
        // Moq cannot setup extension methods. We rely on the extension method to operate
        // against the mocked IChannel at runtime. Instead we setup the method we verify below.

        // Setup BasicPublishAsync to complete successfully when invoked.
        _mockChannel
            .Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _publisher = new SubscriberPublisher(_mockChannel.Object);
    }

    /// <summary>
    /// PublishSubscriberAdded must serialize the event to JSON and publish to the correct exchange.
    /// The message carries the subscriber's birth announcement into the distributed system.
    /// </summary>
    [Fact]
    public async Task PublishSubscriberAdded_SerializesAndPublishesCorrectly()
    {
        // Arrange: A new subscriber event, fresh from creation.
        var evt = new SubscriberAddedEvent
        {
            Id = 1,
            UserId = 123,
            Email = "newuser@example.com",
            Region = "Europe",
            SubscribedOn = DateTime.UtcNow
        };

        // Act: The message is cast into the exchange.
        await _publisher.PublishSubscriberAdded(evt);

        // Assert: Verify BasicPublishAsync was called with correct parameters.
        _mockChannel.Verify(c => c.BasicPublishAsync(
            "subscribers.exchange",  // Exchange name
            "",                      // Routing key (empty for fanout)
            false,                   // Mandatory
            It.Is<BasicProperties>(p => 
                p.ContentType == "application/json" && 
                p.DeliveryMode == DeliveryModes.Persistent),
            It.Is<ReadOnlyMemory<byte>>(body => 
                VerifyEventSerialization(body, evt)),
            It.IsAny<CancellationToken>()
        ), Times.Once, "The event must be published with correct exchange and properties.");
    }

    /// <summary>
    /// PublishSubscriberUpdated must serialize the event and publish with persistent delivery.
    /// Change is announced; the system must know.
    /// </summary>
    [Fact]
    public async Task PublishSubscriberUpdated_SerializesAndPublishesCorrectly()
    {
        // Arrange: An update event, reflecting transformation.
        var evt = new SubscriberUpdatedEvent
        {
            Id = 1,
            UserId = 123,
            Email = "updated@example.com",
            Region = "Asia"
        };

        // Act: The transformation is broadcast.
        await _publisher.PublishSubscriberUpdated(evt);

        // Assert: The message structure must be correct.
        _mockChannel.Verify(c => c.BasicPublishAsync(
            "subscribers.exchange",
            "",
            false,
            It.Is<BasicProperties>(p => 
                p.ContentType == "application/json" && 
                p.DeliveryMode == DeliveryModes.Persistent),
            It.Is<ReadOnlyMemory<byte>>(body => 
                VerifyEventSerialization(body, evt)),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    /// <summary>
    /// PublishSubscriberRemoved must announce the deletion to all who listen.
    /// The subscriber is gone; the message persists.
    /// </summary>
    [Fact]
    public async Task PublishSubscriberRemoved_SerializesAndPublishesCorrectly()
    {
        // Arrange: A removal event, the final message.
        var evt = new SubscriberRemovedEvent
        {
            Id = 1,
            UserId = 123,
            Email = "departed@example.com"
        };

        // Act: The deletion is announced into the void.
        await _publisher.PublishSubscriberRemoved(evt);

        // Assert: The farewell must reach all consumers.
        _mockChannel.Verify(c => c.BasicPublishAsync(
            "subscribers.exchange",
            "",
            false,
            It.Is<BasicProperties>(p => 
                p.ContentType == "application/json" && 
                p.DeliveryMode == DeliveryModes.Persistent),
            It.Is<ReadOnlyMemory<byte>>(body => 
                VerifyEventSerialization(body, evt)),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    /// <summary>
    /// Verifies that message properties are set correctly for all event types.
    /// Persistent delivery ensures messages survive broker restarts.
    /// JSON content type signals the serialization format to consumers.
    /// </summary>
    [Fact]
    public async Task Publish_SetsCorrectMessageProperties()
    {
        // Arrange: Any event will do for this verification.
        var evt = new SubscriberAddedEvent
        {
            Id = 1,
            UserId = 999,
            Email = "test@example.com",
            Region = "NA",
            SubscribedOn = DateTime.UtcNow
        };

        // Act
        await _publisher.PublishSubscriberAdded(evt);

        // Assert: Message properties must guarantee persistence and correct format.
        _mockChannel.Verify(c => c.BasicPublishAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.Is<BasicProperties>(p =>
                p.ContentType == "application/json" &&
                p.DeliveryMode == DeliveryModes.Persistent),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once, "Message must be persistent JSON.");
    }

    /// <summary>
    /// Helper method to verify that the serialized event matches expected structure.
    /// Deserializes the message body and compares key properties.
    /// </summary>
    private static bool VerifyEventSerialization<T>(ReadOnlyMemory<byte> body, T expectedEvent)
    {
        var json = Encoding.UTF8.GetString(body.ToArray());
        var deserializedEvent = JsonSerializer.Deserialize<T>(json);
        
        // Basic verification that serialization worked
        return deserializedEvent != null && 
               JsonSerializer.Serialize(deserializedEvent) == JsonSerializer.Serialize(expectedEvent);
    }
}
