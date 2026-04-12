using Belkhedma.Api.Security;
using Belkhedma.Application;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthConstants.AdminOnlyPolicy)]
public sealed class JobsController(
    IDataCollectionService dataCollectionService,
    IProviderCrawlerJobService providerCrawlerJobService) : ControllerBase
{
    [HttpPost("crawler/{providerCode}")]
    public IActionResult StartCrawlerProvider([FromRoute] string providerCode)
    {
        BackgroundJob.Enqueue(() => providerCrawlerJobService.RunProviderCrawlerAsync(providerCode, CancellationToken.None));
        return Accepted(new { message = $"Crawler job queued for provider '{providerCode}'." });
    }

    [HttpPost("crawler/all")]
    public IActionResult StartCrawlerAll()
    {
        BackgroundJob.Enqueue(() => providerCrawlerJobService.RunAllProvidersCrawlerAsync(CancellationToken.None));
        return Accepted(new { message = "Crawler jobs queued for all providers." });
    }

    [HttpPost("crawler/daily")]
    public IActionResult StartDailyCrawlerJob()
    {
        BackgroundJob.Enqueue(() => providerCrawlerJobService.RunDailyProviderPriceRefreshAsync(CancellationToken.None));
        return Accepted(new { message = "Daily crawler job queued." });
    }

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
