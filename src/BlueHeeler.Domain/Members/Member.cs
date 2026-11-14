namespace BlueHeeler.Domain.Members;

/// <summary>A person on the shelter's volunteer roster. The roster, not the identity provider, decides who may sign in.</summary>
public sealed class Member
{
    /// <summary>The longest name a member can have.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Creates a member.</summary>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty, or <paramref name="name"/> is blank or longer than <see cref="MaxNameLength"/>.</exception>
    public Member(Guid id, string name, EmailAddress email, Role role, WalkerTier? tier, MemberStatus status, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A member id must not be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(email);

        Id = id;
        Name = ValidName(name);
        Email = email;
        Role = role;
        Tier = tier;
        Status = status;
        CreatedAt = createdAt;
    }

    private static string ValidName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        return trimmed.Length <= MaxNameLength
            ? trimmed
            : throw new ArgumentException($"A member name must be at most {MaxNameLength} characters.", nameof(name));
    }

    /// <summary>Puts a new member on the roster.</summary>
    public static Member Add(Guid id, MemberDetails details, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new Member(id, details.Name, details.Email, details.Role, details.Tier, details.Status, now);
    }

    /// <summary>Stable identifier.</summary>
    public Guid Id { get; }

    /// <summary>Display name, trimmed and never blank.</summary>
    public string Name { get; private set; }

    /// <summary>Sign-in email, unique across members.</summary>
    public EmailAddress Email { get; private set; }

    /// <summary>The role that grants this member's permissions.</summary>
    public Role Role { get; private set; }

    /// <summary>Training level, or <see langword="null"/> for members who do not walk.</summary>
    public WalkerTier? Tier { get; private set; }

    /// <summary>Roster status.</summary>
    public MemberStatus Status { get; private set; }

    /// <summary>When the member was added.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Replaces the editable fields. Check <see cref="Access.RosterPolicy.CheckEdit"/> first.</summary>
    /// <exception cref="ArgumentException">The name is blank or longer than <see cref="MaxNameLength"/>; details from
    /// <see cref="MemberDetails.Create"/> never are.</exception>
    public void Apply(MemberDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(details.Email);
        Name = ValidName(details.Name);
        Email = details.Email;
        Role = details.Role;
        Tier = details.Tier;
        Status = details.Status;
    }
}
