using FeatureHubSDK;
using Microsoft.Extensions.Configuration;

namespace SubscriberService.Features;

public class FeatureToggleService : IFeatureToggleService
{
    private readonly IConfiguration _configuration;

    public FeatureToggleService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsSubscriberServiceEnabled() =>
        _configuration.GetValue<bool>("Features:EnableSubscriberService", true);
    
}