using Belkhedma.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Belkhedma.Api.Controllers;

[ApiController]
[Route("api/admin-auth")]
public sealed class AdminAuthController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username/email and password are required." });
        }

        var userNameOrEmail = request.UserNameOrEmail.Trim();
        var user = await userManager.FindByNameAsync(userNameOrEmail)
                   ?? await userManager.FindByEmailAsync(userNameOrEmail);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        var loginResult = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: false);

        if (!loginResult.Succeeded)
        {
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            message = "Login successful.",
            user = new
            {
                user.UserName,
                user.Email,
                Roles = roles
            }
        });
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult LoginPage()
    {
        return Redirect("/admin/auth/login.html");
    }

    [Authorize(Policy = AuthConstants.AdminOnlyPolicy)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new { message = "Logged out." });
    }

    [Authorize(Policy = AuthConstants.AdminOnlyPolicy)]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Unauthorized(new { message = "Not authenticated." });
        }

        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            return Unauthorized(new { message = "Not authenticated." });
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.UserName,
            user.Email,
            Roles = roles
        });
    }
}
