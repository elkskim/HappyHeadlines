using Microsoft.Extensions.Configuration;

namespace SubscriberService.Features;

/// <summary>
/// Feature toggle service with runtime override capability for testing.
/// The configuration remains the source of truth, but can be temporarily overridden
/// without restart for integration testing purposes.
/// </summary>
public class FeatureToggleService : IFeatureToggleService
{
    private readonly IConfiguration _configuration;
    private bool? _runtimeOverride;

    public FeatureToggleService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsSubscriberServiceEnabled()
    {
        // Runtime override takes precedence (for testing)
        if (_runtimeOverride.HasValue)
        {
            return _runtimeOverride.Value;
        }

        // Read from configuration
        var raw = _configuration["Features:EnableSubscriberService"];
        if (bool.TryParse(raw, out var parsed)) return parsed;

        // Fallback to true when missing or unparsable
        return true;
    }

    /// <summary>
    /// Sets a runtime override for the feature toggle.
    /// This persists only in memory and is lost on service restart.
    /// Intended for integration testing without redeployment.
    /// </summary>
    public void SetRuntimeOverride(bool? enabled)
    {
        _runtimeOverride = enabled;
    }

    /// <summary>
    /// Clears the runtime override and returns to configuration-based value.
    /// </summary>
    public void ClearRuntimeOverride()
    {
        _runtimeOverride = null;
    }
}

