using Microsoft.Extensions.Configuration;

namespace SubscriberService.Features;

public class FeatureToggleService : IFeatureToggleService
{
    private readonly IConfiguration _configuration;

    public FeatureToggleService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsSubscriberServiceEnabled()
    {
        // read config with indexer for the f/"(#ing tests to work
        var raw = _configuration["Features:EnableSubscriberService"];
        if (bool.TryParse(raw, out var parsed)) return parsed;

        // fallback to true when missing or unparsable
        return true;
    }
    
}