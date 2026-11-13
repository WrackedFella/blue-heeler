using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Access;

/// <summary>What an identity provider asserts about the person signing in.</summary>
/// <param name="Email">The asserted email address.</param>
/// <param name="EmailVerified">Whether the provider has verified that the person controls <paramref name="Email"/>.</param>
/// <param name="DisplayName">The provider's display name, possibly blank.</param>
public sealed record ExternalIdentity(EmailAddress Email, bool EmailVerified, string DisplayName);
