using Lab5.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace Lab5.Infrastructure.Identity;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthOperationResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return AuthOperationResult.Fail("Email and password are required.");

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return AuthOperationResult.Fail("A user with this email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return AuthOperationResult.Fail(createResult.Errors.Select(e => e.Description).ToArray());

        var roleResult = await _userManager.AddToRoleAsync(user, AuthRoles.User);
        if (!roleResult.Succeeded)
            return AuthOperationResult.Fail(roleResult.Errors.Select(e => e.Description).ToArray());

        return IssueToken(user, new[] { AuthRoles.User });
    }

    public async Task<AuthOperationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return AuthOperationResult.Fail("Email and password are required.");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return AuthOperationResult.Fail("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        return IssueToken(user, roles);
    }

    private AuthOperationResult IssueToken(ApplicationUser user, IList<string> roles)
    {
        var token = _jwtTokenService.CreateToken(user.Id, user.Email ?? user.UserName ?? string.Empty, roles, out var expiresAtUtc);
        return AuthOperationResult.Success(token, expiresAtUtc, user.Email ?? string.Empty, roles.ToList());
    }
}
