using DraftDatabase.Models;
using DraftService.Services;
using Microsoft.AspNetCore.Mvc;
using Monitoring;

namespace DraftService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DraftController : ControllerBase
{
    private readonly IDraftDiService _diService;

    public DraftController(IDraftDiService diService)
    {
        _diService = diService;
    }

    [HttpGet]
    public IActionResult GetDrafts()
    {
        using var activity = MonitorService.ActivitySource.StartActivity("HttpGet.GetDrafts");
        MonitorService.Log.Information("Incoming Request - GetDrafts");
        //var drafts = _diService.GetDrafts() ?? null;
        if (_diService.GetDrafts() is null)
        {
            MonitorService.Log.Information("No drafts found - GetDrafts");
            return NotFound("No drafts found");
        }

        MonitorService.Log.Information("Returning drafts");
        return Ok(_diService.GetDrafts());
    }

    [HttpGet("{id}")]
    public IActionResult GetDraftById(int id)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("HttpGet.GetDraftById");
        MonitorService.Log.Information("Incoming Request - GetDraft");
        var draft = _diService.GetDraftById(id);
        if (draft == null) return NotFound();
        return Ok(draft);
    }

    [HttpPost]
    public async Task<Draft> CreateDraft([FromBody] Draft draft)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("HttpPost.CreateDraft");
        MonitorService.Log.Information("Incoming Request - CreateDraft");
        var newDraft = await _diService.CreateDraft(draft);
        return newDraft;
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDraft([FromBody] Draft draft)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("HttpPut.UpdateDraft");
        MonitorService.Log.Information("Incoming Request - UpdateDraft");
        var updatedDraft = await _diService.UpdateDraft(draft);
        return Ok(updatedDraft);
    }

    [HttpDelete]
    public IActionResult DeleteDraft([FromBody] Draft draft)
    {
        using var activity = MonitorService.ActivitySource.StartActivity("HttpDelete.DeleteDraft");
        MonitorService.Log.Information("Incoming Request - DeleteDraft");
        return Ok(_diService.DeleteDraft(draft.Id));
    }
}