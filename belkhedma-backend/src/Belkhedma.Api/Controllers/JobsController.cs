using Belkhedma.Application;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController(IDataCollectionService dataCollectionService) : ControllerBase
{
    [HttpPost("collect/{providerCode}")]
    public IActionResult CollectProvider([FromRoute] string providerCode)
    {
        BackgroundJob.Enqueue(() => dataCollectionService.CollectProviderDataAsync(providerCode, CancellationToken.None));
        return Accepted(new { message = $"Collection job queued for provider '{providerCode}'." });
    }

    [HttpPost("collect-all")]
    public IActionResult CollectAll()
    {
        BackgroundJob.Enqueue(() => dataCollectionService.CollectAllProvidersDataAsync(CancellationToken.None));
        return Accepted(new { message = "Collection job queued for all providers." });
    }
}
