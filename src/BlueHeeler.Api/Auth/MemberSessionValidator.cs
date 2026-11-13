using BlueHeeler.Api.Data;
using BlueHeeler.Domain.Members;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace BlueHeeler.Api.Auth;

/// <summary>
/// Re-reads the member on every request, so a role change or deactivation applies at once
/// instead of when the cookie expires.
/// </summary>
internal static class MemberSessionValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var memberId = context.Principal is null ? null : AppClaims.MemberId(context.Principal);
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var member = memberId is null
            ? null
            : await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.Id == memberId, context.HttpContext.RequestAborted);

        if (member is null || member.Status != MemberStatus.Active)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AuthSetup.CookieScheme);
            return;
        }

        context.ReplacePrincipal(AppClaims.RequestPrincipal(member));
    }
}
