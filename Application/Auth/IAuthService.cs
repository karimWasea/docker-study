namespace Lab5.Application.Auth;

public interface IAuthService
{
    Task<AuthOperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
