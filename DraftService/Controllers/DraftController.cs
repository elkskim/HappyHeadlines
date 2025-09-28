using System.Threading.Tasks.Sources;
using DraftDatabase.Models;
using DraftService.Services;
using Microsoft.AspNetCore.Mvc;

namespace DraftService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DraftController : ControllerBase
{
    private readonly IDraftDiService _diService;
    
    [HttpGet]
    public List<Draft> GetDrafts()
    {
        return _diService.GetDrafts();
    } 
    
}