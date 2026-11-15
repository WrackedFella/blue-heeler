using BlueHeeler.Domain.Members;

namespace BlueHeeler.Api.Members;

/// <summary>
/// The member form as sent by the SPA. Role and status are nullable so a missing value is a
/// validation error rather than the enum's first member.
/// </summary>
internal sealed record MemberRequest(string? Name, string? Email, Role? Role, int? Tier, MemberStatus? Status)
{
    public MemberDetailsResult ToDetails() => MemberDetails.Create(Name, Email, Role, Tier, Status);
}
