namespace Lab5.Application.Auth;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, DateTime ExpiresAtUtc, string Email, IReadOnlyList<string> Roles);

public sealed class AuthOperationResult
{
    public bool Succeeded { get; init; }
    public string? Token { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public string? Email { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static AuthOperationResult Success(
        string token,
        DateTime expiresAtUtc,
        string email,
        IReadOnlyList<string> roles) =>
        new()
        {
            Succeeded = true,
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Email = email,
            Roles = roles
        };

    public static AuthOperationResult Fail(params string[] errors) =>
        new()
        {
            Succeeded = false,
            Errors = errors
        };
}
