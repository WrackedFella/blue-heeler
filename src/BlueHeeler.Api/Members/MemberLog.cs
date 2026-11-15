using BlueHeeler.Domain.Access;

namespace BlueHeeler.Api.Members;

/// <summary>Roster changes, logged by member id only: names and emails never reach the logs.</summary>
internal sealed partial class MemberLog(ILogger<MemberLog> logger)
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Member {MemberId} added")]
    public partial void Added(Guid memberId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Member {MemberId} edited")]
    public partial void Edited(Guid memberId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Edit of member {MemberId} refused: {Refusal}")]
    public partial void EditRefused(Guid memberId, EditRefusal refusal);
}
