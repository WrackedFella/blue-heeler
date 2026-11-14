using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Access;

/// <summary>Rules that protect the roster as a whole when one member changes.</summary>
public static class RosterPolicy
{
    /// <summary>Checks whether <paramref name="current"/> may take on <paramref name="proposed"/>.</summary>
    /// <param name="current">The member as stored.</param>
    /// <param name="proposed">The new details.</param>
    /// <param name="activeAdminCount">Active Admins on the roster now, including <paramref name="current"/> if it is one.</param>
    /// <returns><see langword="null"/> when allowed. <see cref="EditRefusal.LastActiveAdmin"/> when the change would
    /// leave no Active Admin: the first-admin bootstrap would not run again while an (Inactive) Admin exists, so nobody
    /// could sign in to repair it.</returns>
    public static EditRefusal? CheckEdit(Member current, MemberDetails proposed, int activeAdminCount)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);

        var isActiveAdmin = current.Role == Role.Admin && current.Status == MemberStatus.Active;
        var staysActiveAdmin = proposed.Role == Role.Admin && proposed.Status == MemberStatus.Active;
        return isActiveAdmin && !staysActiveAdmin && activeAdminCount <= 1 ? EditRefusal.LastActiveAdmin : null;
    }
}

/// <summary>Why a member edit was refused.</summary>
public enum EditRefusal
{
    /// <summary>The edit would leave the roster without an Active Admin.</summary>
    LastActiveAdmin,
}
