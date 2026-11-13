namespace BlueHeeler.Domain.Members;

/// <summary>Whether a member is on the active roster. Only Active members can sign in.</summary>
public enum MemberStatus
{
    /// <summary>On the roster and able to sign in.</summary>
    Active,

    /// <summary>Kept for records; cannot sign in.</summary>
    Inactive,
}
