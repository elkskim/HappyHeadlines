namespace SubscriberService.Features;

public interface IFeatureToggleService
{
    bool IsSubscriberServiceEnabled();
    
    /// <summary>
    /// Sets a runtime override for testing purposes.
    /// Pass null to clear the override.
    /// </summary>
    void SetRuntimeOverride(bool? enabled);
    
    /// <summary>
    /// Clears any runtime override and returns to configuration-based value.
    /// </summary>
    void ClearRuntimeOverride();
}
