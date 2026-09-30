using Lab5.Application.Auth;

namespace Lab5.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest request, IAuthService auth, CancellationToken cancellationToken) =>
        {
            var result = await auth.RegisterAsync(request, cancellationToken);
            if (!result.Succeeded)
            {
                var conflict = result.Errors.Any(e => e.Contains("already exists", StringComparison.OrdinalIgnoreCase));
                return conflict
                    ? Results.Conflict(new { Message = result.Errors.FirstOrDefault(), Errors = result.Errors })
                    : Results.BadRequest(new { Message = result.Errors.FirstOrDefault(), Errors = result.Errors });
            }

            return Results.Created("/api/auth/login", new AuthResponse(
                result.Token!,
                result.ExpiresAtUtc!.Value,
                result.Email!,
                result.Roles));
        })
        .WithName("Register")
        .WithSummary("Register a new Identity user with the User role")
        .WithOpenApi();

        group.MapPost("/login", async (LoginRequest request, IAuthService auth, CancellationToken cancellationToken) =>
        {
            var result = await auth.LoginAsync(request, cancellationToken);
            if (!result.Succeeded)
                return Results.Unauthorized();

            return Results.Ok(new AuthResponse(
                result.Token!,
                result.ExpiresAtUtc!.Value,
                result.Email!,
                result.Roles));
        })
        .WithName("Login")
        .WithSummary("Authenticate and receive a JWT")
        .WithOpenApi();

        return group;
    }
}
