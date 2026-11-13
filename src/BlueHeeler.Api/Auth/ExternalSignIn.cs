using System.Security.Claims;
using BlueHeeler.Api.Data;
using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlueHeeler.Api.Auth;

/// <summary>Turns an identity provider's ticket into a roster sign-in, or refuses it.</summary>
internal sealed partial class ExternalSignIn(
    AppDbContext db,
    IOptions<BootstrapOptions> bootstrap,
    TimeProvider time,
    ILogger<ExternalSignIn> logger)
{
    public const string EmailVerifiedClaim = "email_verified";

    public async Task CompleteAsync(TicketReceivedContext context)
    {
        var ct = context.HttpContext.RequestAborted;
        var principal = context.Principal ?? throw new InvalidOperationException("A received ticket always carries a principal.");
        var email = EmailAddress.TryCreate(principal.FindFirstValue(ClaimTypes.Email));
        if (email is null)
        {
            Refuse(context, RefusalReason.NotAMember);
            return;
        }

        var identity = new ExternalIdentity(
            email,
            principal.FindFirstValue(EmailVerifiedClaim) == "true",
            principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty);
        var existing = await db.Members.SingleOrDefaultAsync(m => m.Email == email, ct);
        var adminExists = await db.Members.AnyAsync(m => m.Role == Role.Admin, ct);

        var decision = SignInPolicy.Decide(identity, existing, bootstrap.Value.FirstAdmin, adminExists, time.GetUtcNow());
        switch (decision)
        {
            case SignInDecision.SignIn(var member):
                context.Principal = AppClaims.CookiePrincipal(member);
                LogSignedIn(member.Id);
                break;
            case SignInDecision.BootstrapFirstAdmin(var admin):
                db.Members.Add(admin);
                await db.SaveChangesAsync(ct);
                context.Principal = AppClaims.CookiePrincipal(admin);
                LogBootstrapped(admin.Id);
                break;
            case SignInDecision.Refuse(var reason):
                Refuse(context, reason);
                break;
            default:
                throw new InvalidOperationException($"Unhandled sign-in decision {decision.GetType().Name}.");
        }
    }

    private void Refuse(TicketReceivedContext context, RefusalReason reason)
    {
        LogRefused(reason);
        context.Response.Redirect($"/sign-in?error={ErrorCode(reason)}");
        context.HandleResponse();
    }

    private static string ErrorCode(RefusalReason reason) => reason switch
    {
        RefusalReason.NotAMember => "not-a-member",
        RefusalReason.Inactive => "inactive",
        RefusalReason.EmailNotVerified => "email-not-verified",
    };

    [LoggerMessage(Level = LogLevel.Information, Message = "Member {MemberId} signed in")]
    private partial void LogSignedIn(Guid memberId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "First admin {MemberId} created from the bootstrap setting")]
    private partial void LogBootstrapped(Guid memberId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sign-in refused: {Reason}")]
    private partial void LogRefused(RefusalReason reason);
}
