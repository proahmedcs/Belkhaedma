using Belkhedma.Application;
using Belkhedma.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    ICustomerAuthService customerAuthService,
    IConfiguration configuration,
    IMarketplaceQueryService marketplaceQueryService) : ControllerBase
{
    public sealed record CustomerAuthPayload(string MobileNumber, string FullName);

    [AllowAnonymous]
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

            var accessToken = CreateCustomerJwt(result);
            return Ok(result with { AuthToken = accessToken });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("customers/login")]
    public async Task<IActionResult> Login([FromBody] CustomerAuthPayload payload, CancellationToken cancellationToken)
    {
        return await RegisterOrLogin(payload, cancellationToken);
    }

    [Authorize(AuthenticationSchemes = AuthConstants.CustomerJwtScheme)]
    [HttpGet("customers/me")]
    public async Task<IActionResult> GetCurrentCustomer(CancellationToken cancellationToken)
    {
        var authToken = User.FindFirstValue(AuthConstants.CustomerLegacyTokenClaim);
        if (string.IsNullOrWhiteSpace(authToken))
        {
            return Unauthorized(new { message = "Invalid customer token." });
        }

        var customer = await marketplaceQueryService.GetCustomerProfileByTokenAsync(authToken, cancellationToken);
        if (customer is null)
        {
            return Unauthorized(new { message = "Invalid or expired auth token." });
        }

        return Ok(customer);
    }

    private string CreateCustomerJwt(CustomerAuthResponse authResponse)
    {
        var jwtSettings = configuration.GetSection("Authentication:Jwt");
        if (!jwtSettings.Exists())
        {
            jwtSettings = configuration.GetSection("Jwt");
        }

        var issuer = jwtSettings["Issuer"] ?? "Belkhedma.Api";
        var audience = jwtSettings["Audience"] ?? "Belkhedma.Mobile";
        var signingKey = jwtSettings["SigningKey"] ?? jwtSettings["Key"] ?? "Belkhedma_DevOnly_ChangeThisVeryLongSecretKey_2026";

        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 characters.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, authResponse.CustomerId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, authResponse.CustomerReference),
            new(ClaimTypes.Name, authResponse.FullName),
            new(AuthConstants.CustomerLegacyTokenClaim, authResponse.AuthToken)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: authResponse.ExpiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
