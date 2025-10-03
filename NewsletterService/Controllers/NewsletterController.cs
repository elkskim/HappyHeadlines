using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace NewsletterService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : Controller
{
    [HttpGet]
    public async Task<IActionResult> PretendToSendAMail()
    {
        MonitorService.Log.Warning("You sent an email. This could've been a meeting.");

        return Ok("yeah this is great. very superb email.");
    }
}