using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Access;

/// <summary>The outcome of <see cref="SignInPolicy.Decide"/>.</summary>
public abstract record SignInDecision
{
    private SignInDecision()
    {
    }

    /// <summary>An existing Active member signs in.</summary>
    public sealed record SignIn(Member Member) : SignInDecision;

    /// <summary>The configured first admin signs in for the first time; the caller must save <paramref name="Member"/>.</summary>
    public sealed record BootstrapFirstAdmin(Member Member) : SignInDecision;

    /// <summary>The person may not sign in.</summary>
    public sealed record Refuse(RefusalReason Reason) : SignInDecision;
}

/// <summary>Why a sign-in was refused.</summary>
public enum RefusalReason
{
    /// <summary>No member has this email.</summary>
    NotAMember,

    /// <summary>The member exists but is Inactive.</summary>
    Inactive,

    /// <summary>The identity provider has not verified the email.</summary>
    EmailNotVerified,
}
