namespace Belkhedma.Api.Security;

public static class AuthConstants
{
    public const string CustomerJwtScheme = "JwtCustomer";
    public const string AdminOnlyPolicy = "AdminOnlyPolicy";
    public const string AdminRole = "Admin";
    public const string CustomerLegacyTokenClaim = "legacy_token";
    public const string CustomerReferenceClaim = "customer_reference";
    public const string CustomerIdClaim = "customer_id";
}
