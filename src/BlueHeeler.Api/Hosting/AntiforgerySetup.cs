using Microsoft.AspNetCore.Antiforgery;

namespace BlueHeeler.Api.Hosting;

/// <summary>
/// Cross-site request protection with ASP.NET Core antiforgery: the SPA reads a request token from
/// <see cref="TokenPath"/> and echoes it in <see cref="HeaderName"/> on every state-changing call.
/// </summary>
internal static class AntiforgerySetup
{
    public const string HeaderName = "X-XSRF-TOKEN";
    public const string TokenPath = "/api/antiforgery/token";

    public static IServiceCollection AddAppAntiforgery(this IServiceCollection services) =>
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = "__Host-pas-xsrf";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            // Only the app's own same-origin calls ever need the cookie half of the token pair.
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

    public static IEndpointRouteBuilder MapAntiforgeryEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous on purpose: sign-in requests made before a session exists need a token too.
        // The token is bound to the caller's member id, so the SPA fetches a new one after signing in or out.
        app.MapGet(TokenPath, (IAntiforgery antiforgery, HttpContext http) =>
            new AntiforgeryTokenResponse(antiforgery.GetAndStoreTokens(http).RequestToken!));
        return app;
    }
}

internal sealed record AntiforgeryTokenResponse(string Token);

/// <summary>
/// Refuses state-changing API calls without a valid antiforgery token. Runs after authorization, so a
/// signed-out caller gets 401 (and the SPA sends them to sign in) rather than a token error.
/// </summary>
internal sealed class AntiforgeryValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // IsRequestValidAsync passes GET, HEAD, OPTIONS and TRACE without a token.
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && !await antiforgery.IsRequestValidAsync(context))
        {
            await Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid antiforgery token",
                detail: $"State-changing API calls must send a token from {AntiforgerySetup.TokenPath} in the {AntiforgerySetup.HeaderName} header.")
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
