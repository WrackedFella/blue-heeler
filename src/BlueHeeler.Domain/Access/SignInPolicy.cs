using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Access;

/// <summary>Decides what an external sign-in means for the roster.</summary>
public static class SignInPolicy
{
    /// <summary>Decides whether <paramref name="identity"/> may sign in.</summary>
    /// <param name="identity">What the identity provider asserted.</param>
    /// <param name="existing">The member whose email matches, if any.</param>
    /// <param name="firstAdminEmail">The configured first admin, if any.</param>
    /// <param name="adminExists">Whether any Admin is on the roster. The first-admin bootstrap only applies while none is,
    /// so the setting cannot be used to take over a running system.</param>
    /// <param name="now">The time to stamp on a bootstrapped member.</param>
    public static SignInDecision Decide(
        ExternalIdentity identity,
        Member? existing,
        EmailAddress? firstAdminEmail,
        bool adminExists,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!identity.EmailVerified)
        {
            return new SignInDecision.Refuse(RefusalReason.EmailNotVerified);
        }

        if (existing is not null)
        {
            return existing.Status == MemberStatus.Active
                ? new SignInDecision.SignIn(existing)
                : new SignInDecision.Refuse(RefusalReason.Inactive);
        }

        if (!adminExists && identity.Email == firstAdminEmail)
        {
            var name = string.IsNullOrWhiteSpace(identity.DisplayName) ? identity.Email.Value : identity.DisplayName.Trim();
            if (name.Length > Member.MaxNameLength)
            {
                name = name[..Member.MaxNameLength];
            }

            var admin = new Member(Guid.CreateVersion7(now), name, identity.Email, Role.Admin, tier: null, MemberStatus.Active, now);
            return new SignInDecision.BootstrapFirstAdmin(admin);
        }

        return new SignInDecision.Refuse(RefusalReason.NotAMember);
    }
}
