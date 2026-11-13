namespace BlueHeeler.Domain.Members;

/// <summary>
/// An email address in normalised form: trimmed and lower-cased, so two spellings of one
/// address compare equal and can share a unique index.
/// </summary>
/// <remarks>
/// Validation is deliberately loose (one <c>@</c>, non-empty parts, no whitespace, RFC 5321's
/// 254-character limit). Deliverability is proven by the identity provider or by the
/// magic link, not by parsing.
/// </remarks>
public sealed record EmailAddress
{
    private const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    /// <summary>The normalised address.</summary>
    public string Value { get; }

    /// <summary>Normalises and validates <paramref name="input"/>.</summary>
    /// <returns>The address, or <see langword="null"/> when the input is not a plausible email address.</returns>
    public static EmailAddress? TryCreate(string? input)
    {
        var candidate = input?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(candidate) || candidate.Length > MaxLength || candidate.Any(char.IsWhiteSpace))
        {
            return null;
        }

        var at = candidate.IndexOf('@', StringComparison.Ordinal);
        var isSingleAtBetweenParts = at > 0 && at == candidate.LastIndexOf('@') && at < candidate.Length - 1;
        return isSingleAtBetweenParts ? new EmailAddress(candidate) : null;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
