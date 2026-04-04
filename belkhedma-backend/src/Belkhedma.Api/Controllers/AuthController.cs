using Belkhedma.Application;
using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ICustomerAuthService customerAuthService,
    IMarketplaceQueryService marketplaceQueryService) : ControllerBase
{
    public sealed record CustomerAuthPayload(string MobileNumber, string FullName);

    [HttpPost("customers/register-or-login")]
    public async Task<IActionResult> RegisterOrLogin([FromBody] CustomerAuthPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var result = await customerAuthService.RegisterOrLoginAsync(
                new CustomerAuthRequest(payload.MobileNumber, payload.FullName),
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("customers/me")]
    public async Task<IActionResult> GetCurrentCustomer([FromHeader(Name = "Authorization")] string? authorization, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return Unauthorized(new { message = "Authorization bearer token is required." });
        }

        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { message = "Authorization header must be Bearer token." });
        }

        var authToken = authorization[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(authToken))
        {
            return Unauthorized(new { message = "Bearer token is required." });
        }

        var customer = await marketplaceQueryService.GetCustomerProfileByTokenAsync(authToken, cancellationToken);
        if (customer is null)
        {
            return Unauthorized(new { message = "Invalid or expired auth token." });
        }

        return Ok(customer);
    }
}
