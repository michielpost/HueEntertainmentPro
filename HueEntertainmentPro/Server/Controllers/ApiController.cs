using HueLightDJ.Services;
using Microsoft.AspNetCore.Mvc;

namespace HueEntertainmentPro.Server.Controllers
{
  [Route("api")]
  [ApiController]
  public class ApiController : ControllerBase
  {
    private readonly EffectService _effectService;

    public ApiController(EffectService effectService)
    {
      _effectService = effectService;
    }

    /// <summary>
    /// Triggers a beat effect
    /// </summary>
    [Route("beat")]
    [HttpPost]
    [HttpGet]
    public IActionResult Beat()
    {
      try
      {
        _effectService.Beat();
        return Ok(new { success = true });
      }
      catch (System.Exception ex)
      {
        return StatusCode(500, new { success = false, error = ex.Message });
      }
    }

    [HttpPost("setcolors")]
    public void SetColors([FromBody] string[,] matrix)
    {
      ManualControlService.SetColors(matrix);
    }

    [HttpPost("setcolorslist")]
    public void SetColors([FromBody] List<List<string>> matrix)
    {
      ManualControlService.SetColors(matrix);
    }

    /// <summary>
    /// Test endpoint to verify API is working
    /// </summary>
    [HttpGet("test")]
    public IActionResult Test()
    {
      return Ok(new { status = "API is working", timestamp = System.DateTime.UtcNow });
    }
  }
}
