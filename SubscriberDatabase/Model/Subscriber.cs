namespace SubscriberDatabase.Model;

public class Subscriber
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Region { get; set; }
    public DateTime SubscribedOn { get; set; }
}