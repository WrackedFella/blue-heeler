using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Access;

/// <summary>Rules that protect the roster as a whole when one member changes.</summary>
public static class RosterPolicy
{
    /// <summary>Checks whether <paramref name="current"/> may take on <paramref name="proposed"/>.</summary>
    /// <param name="current">The member as stored.</param>
    /// <param name="proposed">The new details.</param>
    /// <param name="activeAdminCount">Active Admins on the roster now, including <paramref name="current"/> if it is one.</param>
    /// <returns><see langword="null"/> when allowed, otherwise why not. Both refusals guard the same failure: the
    /// first-admin bootstrap never runs again while any Admin exists, so a roster whose only Active Admin cannot sign
    /// in can only be repaired in the database.</returns>
    public static EditRefusal? CheckEdit(Member current, MemberDetails proposed, int activeAdminCount)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);

        var isActiveAdmin = current.Role == Role.Admin && current.Status == MemberStatus.Active;
        var staysActiveAdmin = proposed.Role == Role.Admin && proposed.Status == MemberStatus.Active;
        if (!isActiveAdmin || activeAdminCount > 1)
        {
            return null;
        }

        if (!staysActiveAdmin)
        {
            return EditRefusal.LastActiveAdmin;
        }

        // A mistyped address would lock the last admin out once their session ends.
        return proposed.Email != current.Email ? EditRefusal.LastActiveAdminEmail : null;
    }
}

/// <summary>Why a member edit was refused.</summary>
public enum EditRefusal
{
    /// <summary>The edit would leave the roster without an Active Admin.</summary>
    LastActiveAdmin,

    /// <summary>The edit would change the sign-in email of the only Active Admin.</summary>
    LastActiveAdminEmail,
}
