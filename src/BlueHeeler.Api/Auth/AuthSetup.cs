using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;

namespace BlueHeeler.Api.Auth;

/// <summary>Wires cookie sessions, Google sign-in and per-permission policies.</summary>
internal static class AuthSetup
{
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static IServiceCollection AddAppAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<GoogleAuthOptions>().Bind(configuration.GetSection(GoogleAuthOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<BootstrapOptions>().Bind(configuration.GetSection(BootstrapOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
        services.AddScoped<ExternalSignIn>();

        services
            .AddAuthentication(CookieScheme)
            .AddCookie(CookieScheme, ConfigureCookie)
            .AddGoogle(GoogleDefaults.AuthenticationScheme, ConfigureGoogle);
        services.AddOptions<GoogleOptions>(GoogleDefaults.AuthenticationScheme)
            .Configure<IOptions<GoogleAuthOptions>>((google, app) =>
            {
                google.ClientId = app.Value.ClientId;
                google.ClientSecret = app.Value.ClientSecret;
            });

        services.AddAuthorization(options => options.AddPermissionPolicies());
        return services;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        // __Host- pins the cookie to this exact origin over HTTPS.
        options.Cookie.Name = "__Host-pas-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Lax, not Strict: the redirect back from Google is a cross-site navigation that must carry the new cookie.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/sign-in";
        options.Events.OnValidatePrincipal = MemberSessionValidator.ValidateAsync;
        options.Events.OnRedirectToLogin = context => ApiStatusOrRedirect(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => ApiStatusOrRedirect(context, StatusCodes.Status403Forbidden);
    }

    private static void ConfigureGoogle(GoogleOptions options)
    {
        options.SignInScheme = CookieScheme;
        options.ClaimActions.MapCustomJson(ExternalSignIn.EmailVerifiedClaim, user =>
            user.TryGetProperty("email_verified", out var verified) && verified.ValueKind == JsonValueKind.True ? "true" : "false");
        options.Events.OnTicketReceived = context =>
            context.HttpContext.RequestServices.GetRequiredService<ExternalSignIn>().CompleteAsync(context);
    }

    /// <summary>The SPA routes the user itself, so API calls get a status code instead of an HTML redirect.</summary>
    private static Task ApiStatusOrRedirect(RedirectContext<CookieAuthenticationOptions> context, int status)
    {
        if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = status;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    }
}
