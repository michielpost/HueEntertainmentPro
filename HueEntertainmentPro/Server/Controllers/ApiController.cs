using HueLightDJ.Services;  // ✅ KORREKT - Nicht . Internal
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
    /// Triggers a beat effect with the specified intensity
    /// </summary>
    /// <param name="intensity">Beat intensity (typically 0.0 to 1.0)</param>
    [HttpPost("beat")]
    public IActionResult Beat([FromBody] double intensity)
    {
      try
      {
        _effectService.Beat(intensity);
        return Ok(new { success = true, message = "Beat triggered successfully" });
      }
      catch (System.Exception ex)
      {
        return StatusCode(500, new { success = false, error = ex.Message });
      }
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
