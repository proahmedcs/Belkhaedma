using Belkhedma.Application;
using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MarketplaceController(
    IMarketplaceQueryService marketplaceQueryService) : ControllerBase
{
    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await marketplaceQueryService.GetProvidersAsync(cancellationToken);
        return Ok(providers);
    }

    [HttpGet("offers")]
    public async Task<IActionResult> GetOffers([FromQuery] string? providerCode, CancellationToken cancellationToken)
    {
        var offers = await marketplaceQueryService.GetOffersAsync(providerCode, cancellationToken);
        return Ok(offers);
    }

    [HttpGet("prices/latest")]
    public async Task<IActionResult> GetLatestPrices([FromQuery] string? providerCode, CancellationToken cancellationToken)
    {
        var prices = await marketplaceQueryService.GetLatestPricesAsync(providerCode, cancellationToken);
        return Ok(prices);
    }

}
