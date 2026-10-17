using Microsoft.AspNetCore.Mvc;

namespace SreAgentDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SampleController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SampleResponse), StatusCodes.Status200OK)]
    public ActionResult<SampleResponse> Get() =>
        Ok(new SampleResponse("This is a mock response from the sample controller.", "sample"));
}

/// <summary>Represents the mock response returned by the sample endpoint.</summary>
public sealed record SampleResponse(string Message, string Source);
