using BlueHeeler.Domain.Members;

namespace BlueHeeler.Api.Members;

/// <summary>A member as shown in the member list.</summary>
internal sealed record MemberResponse(Guid Id, string Name, string Email, string Role, int? Tier, string Status)
{
    public static MemberResponse From(Member member) =>
        new(member.Id, member.Name, member.Email.Value, member.Role.ToString(), (int?)member.Tier, member.Status.ToString());
}
