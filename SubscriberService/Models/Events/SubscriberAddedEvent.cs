namespace SubscriberService.Models.Events;

public class SubscriberAddedEvent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public DateTime SubscribedOn { get; set; }
}