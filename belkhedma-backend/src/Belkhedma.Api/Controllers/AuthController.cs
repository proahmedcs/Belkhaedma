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
    public sealed record CustomerRegisterPayload(
        string Email,
        string MobileNumber,
        string FullName,
        string Password);
    public sealed record CustomerLoginPayload(
        string UserNameOrEmail,
        string Password);
    public sealed record CustomerRefreshPayload(
        string RefreshToken);
    public sealed record CustomerRevokePayload(
        string RefreshToken);
    public sealed record CustomerLegacyAuthPayload(
        string? MobileNumber,
        string? FullName,
        string? Password,
        string? Email,
        string? UserNameOrEmail);

    [AllowAnonymous]
    [HttpPost("customers/register")]
    public async Task<IActionResult> Register([FromBody] CustomerRegisterPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var result = await customerAuthService.RegisterAsync(
                new CustomerRegisterRequest(
                    payload.Email,
                    payload.MobileNumber,
                    payload.FullName,
                    payload.Password),
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
    public async Task<IActionResult> Login([FromBody] CustomerLoginPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var result = await customerAuthService.LoginAsync(
                new CustomerLoginRequest(
                    payload.UserNameOrEmail,
                    payload.Password),
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
    [HttpPost("customers/register-or-login")]
    public async Task<IActionResult> RegisterOrLogin([FromBody] CustomerLegacyAuthPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var normalizedRequest = new CustomerAuthRequest(
                payload.Email,
                payload.MobileNumber,
                payload.FullName,
                payload.Password ?? string.Empty,
                payload.UserNameOrEmail);
            var result = await customerAuthService.RegisterOrLoginAsync(normalizedRequest, cancellationToken);

            var accessToken = CreateCustomerJwt(result);
            return Ok(result with { AuthToken = accessToken });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = AuthConstants.CustomerJwtScheme)]
    [HttpGet("customers/me")]
    public async Task<IActionResult> GetCurrentCustomer(CancellationToken cancellationToken)
    {
        var customerIdClaim = User.FindFirstValue(AuthConstants.CustomerIdClaim);
        if (!Guid.TryParse(customerIdClaim, out var customerId))
        {
            return Unauthorized(new { message = "Invalid customer token." });
        }

        var customer = await marketplaceQueryService.GetCustomerProfileByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return Unauthorized(new { message = "Invalid or inactive customer account." });
        }

        return Ok(customer);
    }

    [AllowAnonymous]
    [HttpPost("customers/refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] CustomerRefreshPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var result = await customerAuthService.RefreshTokenAsync(
                new CustomerTokenRefreshRequest(payload.RefreshToken),
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
    [HttpPost("customers/revoke")]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] CustomerRevokePayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            await customerAuthService.RevokeRefreshTokenAsync(
                new CustomerTokenRevokeRequest(payload.RefreshToken),
                cancellationToken);
            return Ok(new { message = "Refresh token revoked." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
            new(AuthConstants.CustomerIdClaim, authResponse.CustomerId.ToString()),
            new(AuthConstants.CustomerReferenceClaim, authResponse.CustomerReference),
            new(AuthConstants.CustomerEmailClaim, authResponse.Email),
            new(AuthConstants.CustomerMobileClaim, authResponse.MobileNumber)
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
