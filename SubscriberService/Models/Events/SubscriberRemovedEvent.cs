namespace SubscriberService.Models.Events;

public class SubscriberRemovedEvent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}