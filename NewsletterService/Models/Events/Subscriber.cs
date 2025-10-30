namespace NewsletterService.Models.Events;

public class Subscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public DateTime SubscribedOn { get; set; }
}

