namespace BlueHeeler.Domain.Members;

/// <summary>The part of a member an admin edits.</summary>
public sealed record MemberDetails(string Name, EmailAddress Email, Role Role, WalkerTier? Tier, MemberStatus Status)
{
    /// <summary>Validates raw input, such as a form, into details.</summary>
    /// <returns>The details, or every field error keyed by the lower-case field name.</returns>
    public static MemberDetailsResult Create(string? name, string? email, Role? role, int? tier, MemberStatus? status)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmedName = name?.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            errors["name"] = ["Enter a name."];
        }
        else if (trimmedName.Length > Member.MaxNameLength)
        {
            errors["name"] = [$"Use at most {Member.MaxNameLength} characters."];
        }

        var address = EmailAddress.TryCreate(email);
        if (address is null)
        {
            errors["email"] = ["Enter an email address."];
        }

        if (role is not { } r || !Enum.IsDefined(r))
        {
            errors["role"] = ["Choose a role."];
        }

        if (status is not { } s || !Enum.IsDefined(s))
        {
            errors["status"] = ["Choose a status."];
        }

        if (tier is not null && !Enum.IsDefined((WalkerTier)tier.Value))
        {
            errors["tier"] = ["Tier must be 1, 2 or 3, or left blank."];
        }

        return errors.Count > 0
            ? new MemberDetailsResult.Invalid(errors)
            : new MemberDetailsResult.Valid(new MemberDetails(trimmedName!, address!, role!.Value, (WalkerTier?)tier, status!.Value));
    }
}

/// <summary>The outcome of <see cref="MemberDetails.Create"/>.</summary>
public abstract record MemberDetailsResult
{
    private MemberDetailsResult()
    {
    }

    /// <summary>The input was valid.</summary>
    public sealed record Valid(MemberDetails Details) : MemberDetailsResult;

    /// <summary>The input had errors, keyed by field.</summary>
    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : MemberDetailsResult;
}
