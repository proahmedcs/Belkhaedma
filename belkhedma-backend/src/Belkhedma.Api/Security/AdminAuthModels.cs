namespace Belkhedma.Api.Security;

public sealed record AdminLoginRequest(
    string UserNameOrEmail,
    string Password,
    bool RememberMe = true);
