using System.Security.Claims;
using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Api.Auth;

/// <summary>Builds the signed-in principal from a member's current state.</summary>
internal static class AppClaims
{
    public const string Permission = "permission";

    /// <summary>The principal written to the cookie: the member id only, so nothing in it can go stale.</summary>
    public static ClaimsPrincipal CookiePrincipal(Member member) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, member.Id.ToString())], AuthSetup.CookieScheme));

    /// <summary>The principal a request runs as, rebuilt from the member on every request.</summary>
    public static ClaimsPrincipal RequestPrincipal(Member member)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new(ClaimTypes.Name, member.Name),
            new(ClaimTypes.Email, member.Email.Value),
            new(ClaimTypes.Role, member.Role.ToString()),
        };
        claims.AddRange(RolePermissions.For(member.Role).Select(p => new Claim(Permission, p.ToString())));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthSetup.CookieScheme));
    }

    public static Guid? MemberId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
