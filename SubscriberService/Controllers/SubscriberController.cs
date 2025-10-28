using Microsoft.AspNetCore.Mvc;
using SubscriberDatabase.Model;
using SubscriberService.Models.DTO;
using SubscriberService.Services;

namespace SubscriberService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriberController : ControllerBase
{
    private readonly ISubscriberAppService _subscriberAppService;

    public SubscriberController(ISubscriberAppService subscriberAppService)
    {
        _subscriberAppService = subscriberAppService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscriberCreateDto subscriber)
    {
        var created = await _subscriberAppService.Create(subscriber);
        if (created == null)
        {
            return BadRequest("Could not subscribe");
        }
        
        return CreatedAtAction(nameof(GetSubscriber), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SubscriberUpdateDto subscriber)
    {
        var updated = await _subscriberAppService.Update(id, subscriber);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Unsubscribe(int id)
    {
        var deleted = await _subscriberAppService.Delete(id);

        if (!deleted)
        {
            return NotFound($"Could not find subscriber with ID {id} to unsubscribe");
        }
        
        return Ok("Subscriber was unsubscribed");
    }

    [HttpGet]
    public async Task<IActionResult> GetSubscribers()
    {
        var subscribers = await _subscriberAppService.GetSubscribers();
        if (!subscribers.Any())
        {
            return NotFound("It seems that there are no subscribers");
        }
        return Ok(subscribers);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSubscriber(int id)
    {
        var subscriber = await _subscriberAppService.GetById(id);
        if (subscriber == null)
        {
            return NotFound("This subscriber was not found");
        }
        return Ok(subscriber);
    }
}