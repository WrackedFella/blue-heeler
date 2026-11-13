using System.Security.Claims;
using BlueHeeler.Domain.Access;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace BlueHeeler.Api.Auth;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/google", () =>
            Results.Challenge(new AuthenticationProperties { RedirectUri = "/members" }, [GoogleDefaults.AuthenticationScheme]));

        app.MapPost("/api/auth/sign-out", async (HttpContext http) =>
        {
            await http.SignOutAsync(AuthSetup.CookieScheme);
            return Results.NoContent();
        });

        app.MapGet("/api/me", (ClaimsPrincipal user) => new MeResponse(
                AppClaims.MemberId(user) ?? throw new InvalidOperationException("An authenticated principal always carries a member id."),
                user.FindFirstValue(ClaimTypes.Name)!,
                user.FindFirstValue(ClaimTypes.Email)!,
                user.FindFirstValue(ClaimTypes.Role)!,
                [.. user.FindAll(AppClaims.Permission).Select(c => c.Value)]))
            .RequireAuthorization();

        return app;
    }
}

/// <summary>The signed-in member, as the SPA needs it to choose what to show.</summary>
internal sealed record MeResponse(Guid Id, string Name, string Email, string Role, string[] Permissions);
