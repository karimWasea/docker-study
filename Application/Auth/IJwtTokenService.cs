namespace Lab5.Application.Auth;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, IEnumerable<string> roles, out DateTime expiresAtUtc);
}
