using Hangfire.Dashboard;
using Microsoft.AspNetCore.Identity;

namespace Belkhedma.Api.Security;

public static class AdminAuthSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        string defaultAdminEmail,
        string defaultAdminPassword)
    {
        const string defaultAdminUserName = "admin";
        var normalizedEmail = string.IsNullOrWhiteSpace(defaultAdminEmail)
            ? "admin@belkhedma.local"
            : defaultAdminEmail.Trim();
        var normalizedPassword = string.IsNullOrWhiteSpace(defaultAdminPassword)
            ? "Admin#12345"
            : defaultAdminPassword;

        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(AuthConstants.AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AuthConstants.AdminRole));
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed creating admin role: {errors}");
            }
        }

        var admin = await userManager.FindByNameAsync(defaultAdminUserName)
                    ?? await userManager.FindByEmailAsync(normalizedEmail);

        if (admin is null)
        {
            admin = new IdentityUser
            {
                UserName = defaultAdminUserName,
                Email = normalizedEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin, normalizedPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed creating default admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AuthConstants.AdminRole))
        {
            var addRoleResult = await userManager.AddToRoleAsync(admin, AuthConstants.AdminRole);
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join("; ", addRoleResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed assigning admin role: {errors}");
            }
        }
    }
}

public sealed class AdminHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole(AuthConstants.AdminRole);
    }
}
