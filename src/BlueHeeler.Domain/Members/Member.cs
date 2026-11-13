namespace BlueHeeler.Domain.Members;

/// <summary>A person on the shelter's volunteer roster. The roster, not the identity provider, decides who may sign in.</summary>
public sealed class Member
{
    /// <summary>Creates a member.</summary>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or <paramref name="name"/> is blank.</exception>
    public Member(Guid id, string name, EmailAddress email, Role role, WalkerTier? tier, MemberStatus status, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A member id must not be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(email);

        Id = id;
        Name = name.Trim();
        Email = email;
        Role = role;
        Tier = tier;
        Status = status;
        CreatedAt = createdAt;
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
}
