using Belkhedma.Application;
using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MarketplaceController(
    IMarketplaceQueryService marketplaceQueryService,
    IMarketplaceAdminService marketplaceAdminService) : ControllerBase
{
    public sealed record UpdateExpirationRequest(DateTime? ExpiresAtUtc);

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

    [HttpGet("prices")]
    public async Task<IActionResult> GetAllPrices([FromQuery] string? providerCode, [FromQuery] bool includeExpired = true, CancellationToken cancellationToken = default)
    {
        var prices = await marketplaceQueryService.GetAllPricesAsync(providerCode, includeExpired, cancellationToken);
        return Ok(prices);
    }

    [HttpPut("prices/{priceSnapshotId:guid}/expiration")]
    public async Task<IActionResult> SetPriceExpiration([FromRoute] Guid priceSnapshotId, [FromBody] UpdateExpirationRequest request, CancellationToken cancellationToken)
    {
        var affected = await marketplaceAdminService.SetPriceExpirationAsync(priceSnapshotId, request.ExpiresAtUtc, cancellationToken);
        if (affected == 0)
        {
            return NotFound(new { message = "Price snapshot not found." });
        }

        return Ok(new
        {
            message = request.ExpiresAtUtc.HasValue
                ? "Price expiration updated."
                : "Price expiration removed (set to far future).",
            affected
        });
    }

    [HttpPut("prices/expiration")]
    public async Task<IActionResult> SetPricesExpirationBulk([FromQuery] string? providerCode, [FromBody] UpdateExpirationRequest request, CancellationToken cancellationToken)
    {
        var affected = await marketplaceAdminService.SetPricesExpirationBulkAsync(providerCode, request.ExpiresAtUtc, cancellationToken);
        return Ok(new
        {
            message = request.ExpiresAtUtc.HasValue
                ? "Bulk price expiration updated."
                : "Bulk price expiration removed (set to far future).",
            affected
        });
    }

    [HttpGet("json-documents")]
    public async Task<IActionResult> GetJsonDocuments([FromQuery] string? providerCode, [FromQuery] bool includeExpired = true, CancellationToken cancellationToken = default)
    {
        var documents = await marketplaceQueryService.GetProviderJsonDocumentsAsync(providerCode, includeExpired, cancellationToken);
        return Ok(documents);
    }

    [HttpPut("json-documents/{documentId:guid}/expiration")]
    public async Task<IActionResult> SetJsonDocumentExpiration([FromRoute] Guid documentId, [FromBody] UpdateExpirationRequest request, CancellationToken cancellationToken)
    {
        var affected = await marketplaceAdminService.SetJsonDocumentExpirationAsync(documentId, request.ExpiresAtUtc, cancellationToken);
        if (affected == 0)
        {
            return NotFound(new { message = "JSON document not found." });
        }

        return Ok(new
        {
            message = request.ExpiresAtUtc.HasValue
                ? "JSON document expiration updated."
                : "JSON document expiration removed (set to far future).",
            affected
        });
    }

    [HttpPut("json-documents/expiration")]
    public async Task<IActionResult> SetJsonDocumentExpirationBulk([FromQuery] string? providerCode, [FromBody] UpdateExpirationRequest request, CancellationToken cancellationToken)
    {
        var affected = await marketplaceAdminService.SetJsonDocumentsExpirationBulkAsync(providerCode, request.ExpiresAtUtc, cancellationToken);
        return Ok(new
        {
            message = request.ExpiresAtUtc.HasValue
                ? "Bulk JSON document expiration updated."
                : "Bulk JSON document expiration removed (set to far future).",
            affected
        });
    }

}
