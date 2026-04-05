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
    public sealed record HomePromotionPayload(
        string? Code,
        string CompanyNameAr,
        string CompanyNameEn,
        string TitleAr,
        string TitleEn,
        string SubtitleAr,
        string SubtitleEn,
        string ImageUrl,
        string? TargetUrl,
        string? DeepLink,
        IReadOnlyList<string>? Items,
        string? ProviderCode,
        int DisplayOrder,
        bool IsActive);
    public sealed record CustomerProfileResponse(
        Guid CustomerId,
        string CustomerReference,
        string FullName,
        string MobileNumber);

    private bool TryReadBearerToken(out string token)
    {
        token = string.Empty;
        if (!Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            return false;
        }

        var headerValue = authorization.ToString();
        const string prefix = "Bearer ";
        if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = headerValue[prefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

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

    [HttpGet("home-promotions")]
    public async Task<IActionResult> GetHomePromotions([FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var promotions = await marketplaceQueryService.GetHomePromotionsAsync(includeInactive, cancellationToken);
        return Ok(promotions);
    }

    [HttpPost("home-promotions")]
    public async Task<IActionResult> CreateHomePromotion([FromBody] HomePromotionPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var created = await marketplaceAdminService.CreateHomePromotionAsync(
                new CreateOrUpdateHomePromotionRequest(
                    payload.Code,
                    payload.CompanyNameAr,
                    payload.CompanyNameEn,
                    payload.TitleAr,
                    payload.TitleEn,
                    payload.SubtitleAr,
                    payload.SubtitleEn,
                    payload.ImageUrl,
                    payload.TargetUrl,
                    payload.DeepLink,
                    payload.Items,
                    payload.ProviderCode,
                    payload.DisplayOrder,
                    payload.IsActive),
                cancellationToken);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("home-promotions/{promotionId:guid}")]
    public async Task<IActionResult> UpdateHomePromotion(
        [FromRoute] Guid promotionId,
        [FromBody] HomePromotionPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var updated = await marketplaceAdminService.UpdateHomePromotionAsync(
                promotionId,
                new CreateOrUpdateHomePromotionRequest(
                    payload.Code,
                    payload.CompanyNameAr,
                    payload.CompanyNameEn,
                    payload.TitleAr,
                    payload.TitleEn,
                    payload.SubtitleAr,
                    payload.SubtitleEn,
                    payload.ImageUrl,
                    payload.TargetUrl,
                    payload.DeepLink,
                    payload.Items,
                    payload.ProviderCode,
                    payload.DisplayOrder,
                    payload.IsActive),
                cancellationToken);
            if (updated is null)
            {
                return NotFound(new { message = "Home promotion not found." });
            }

            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("home-promotions/{promotionId:guid}")]
    public async Task<IActionResult> DeleteHomePromotion([FromRoute] Guid promotionId, CancellationToken cancellationToken = default)
    {
        var deleted = await marketplaceAdminService.DeleteHomePromotionAsync(promotionId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Home promotion not found." });
        }

        return Ok(new { message = "Home promotion deleted." });
    }

    [HttpGet("customers/{customerReference}/locations")]
    public async Task<IActionResult> GetCustomerSavedLocations([FromRoute] string customerReference, CancellationToken cancellationToken = default)
    {
        if (!TryReadBearerToken(out var authToken))
        {
            return Unauthorized(new { message = "Authorization token is required." });
        }

        var profile = await marketplaceQueryService.GetCustomerProfileByTokenAsync(authToken, cancellationToken);
        if (profile is null)
        {
            return Unauthorized(new { message = "Invalid or expired token." });
        }

        if (string.IsNullOrWhiteSpace(customerReference))
        {
            return BadRequest(new { message = "customerReference is required." });
        }

        if (!string.Equals(profile.CustomerReference, customerReference, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var locations = await marketplaceQueryService.GetCustomerSavedLocationsAsync(customerReference, cancellationToken);
        return Ok(locations);
    }

    [HttpGet("customers/me")]
    public async Task<IActionResult> GetMyCustomerProfile(CancellationToken cancellationToken = default)
    {
        if (!TryReadBearerToken(out var authToken))
        {
            return Unauthorized(new { message = "Authorization token is required." });
        }

        var profile = await marketplaceQueryService.GetCustomerProfileByTokenAsync(authToken, cancellationToken);
        if (profile is null)
        {
            return Unauthorized(new { message = "Invalid or expired token." });
        }

        return Ok(new CustomerProfileResponse(
            profile.CustomerId,
            profile.CustomerReference,
            profile.FullName,
            profile.MobileNumber));
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
