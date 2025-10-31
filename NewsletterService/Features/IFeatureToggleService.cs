namespace NewsletterService.Features;

public interface IFeatureToggleService
{
    bool IsSubscriberServiceEnabled();
}