using Belkhedma.Api.Security;
using Belkhedma.Application;
using Belkhedma.Infrastructure;
using Belkhedma.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BelkhedmaSharedDb")
    ?? throw new InvalidOperationException("Connection string 'BelkhedmaSharedDb' is missing.");
var jwtSettings = builder.Configuration.GetSection("Authentication:Jwt");
if (!jwtSettings.Exists())
{
    jwtSettings = builder.Configuration.GetSection("Jwt");
}
var jwtIssuer = jwtSettings["Issuer"] ?? "Belkhedma.Api";
var jwtAudience = jwtSettings["Audience"] ?? "Belkhedma.Mobile";
var jwtKey = jwtSettings["SigningKey"] ?? jwtSettings["Key"] ?? "Belkhedma_DevOnly_ChangeThisVeryLongSecretKey_2026";
var seedAdminSettings = builder.Configuration.GetSection("Authentication:Admin");
var seedAdminEmail = seedAdminSettings["SeedEmail"] ?? "admin@belkhedma.local";
var seedAdminPassword = seedAdminSettings["SeedPassword"] ?? "Admin#12345";
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<BelkhedmaDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
    .AddJwtBearer(AuthConstants.CustomerJwtScheme, options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "belkhedma.admin.auth";
        options.LoginPath = "/admin/auth/login.html";
        options.AccessDeniedPath = "/admin/auth/login.html";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.AdminOnlyPolicy, policy =>
        policy.RequireAuthenticatedUser()
            .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
            .RequireRole(AuthConstants.AdminRole));
});

builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            PrepareSchemaIfNecessary = true,
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.FromSeconds(15),
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
    {
        var isLoginPage = path.StartsWith("/admin/auth/login.html", StringComparison.OrdinalIgnoreCase);
        if (!isLoginPage)
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
            var isAdmin = context.User?.IsInRole(AuthConstants.AdminRole) ?? false;
            if (!isAuthenticated || !isAdmin)
            {
                var returnUrl = Uri.EscapeDataString(path + context.Request.QueryString.Value);
                context.Response.Redirect($"/admin/auth/login.html?returnUrl={returnUrl}");
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BelkhedmaDbContext>();
    await dbContext.Database.MigrateAsync();
    await BelkhedmaDbSeeder.SeedAsync(dbContext);
    await AdminAuthSeeder.SeedAsync(scope.ServiceProvider, seedAdminEmail, seedAdminPassword);
}

RecurringJob.AddOrUpdate<IDataCollectionService>(
    "collect-all-providers-pricing",
    service => service.CollectAllProvidersDataAsync(CancellationToken.None),
    "*/30 * * * *");

app.UseHttpsRedirection();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AdminHangfireAuthorizationFilter()]
});

app.MapControllers();

app.Run();
