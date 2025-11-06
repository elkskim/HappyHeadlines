using Microsoft.AspNetCore.Mvc;
using SubscriberService.Features;

namespace SubscriberService.Controllers;

/// <summary>
/// Admin endpoints for testing and debugging.
/// WARNING: In production, these should be protected by authentication/authorization
/// or removed entirely. Currently exposed for integration testing purposes.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly IFeatureToggleService _featureToggleService;

    public AdminController(IFeatureToggleService featureToggleService)
    {
        _featureToggleService = featureToggleService;
    }

    /// <summary>
    /// Temporarily disable the SubscriberService without restart.
    /// The override persists only in memory and is lost on service restart.
    /// </summary>
    /// <returns>Status message</returns>
    [HttpPost("disable-service")]
    public IActionResult DisableService()
    {
        _featureToggleService.SetRuntimeOverride(false);
        return Ok(new { 
            message = "SubscriberService disabled via runtime override",
            note = "This override is temporary and will be lost on service restart"
        });
    }

    /// <summary>
    /// Temporarily enable the SubscriberService without restart.
    /// The override persists only in memory and is lost on service restart.
    /// </summary>
    /// <returns>Status message</returns>
    [HttpPost("enable-service")]
    public IActionResult EnableService()
    {
        _featureToggleService.SetRuntimeOverride(true);
        return Ok(new { 
            message = "SubscriberService enabled via runtime override",
            note = "This override is temporary and will be lost on service restart"
        });
    }

    /// <summary>
    /// Clear the runtime override and return to configuration-based value.
    /// </summary>
    /// <returns>Status message</returns>
    [HttpPost("reset-feature-toggle")]
    public IActionResult ResetFeatureToggle()
    {
        _featureToggleService.ClearRuntimeOverride();
        return Ok(new { 
            message = "Runtime override cleared, using configuration value",
            note = "Feature toggle now reads from appsettings.json or environment variables"
        });
    }

    /// <summary>
    /// Get the current feature toggle status.
    /// </summary>
    /// <returns>Feature toggle status</returns>
    [HttpGet("feature-toggle-status")]
    public IActionResult GetFeatureToggleStatus()
    {
        var enabled = _featureToggleService.IsSubscriberServiceEnabled();
        return Ok(new { 
            enabled,
            message = enabled ? "Service is enabled" : "Service is disabled"
        });
    }
}

