using System.ComponentModel.DataAnnotations;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Api.Auth;

/// <summary>Settings for creating the first admin on an empty roster.</summary>
internal sealed class BootstrapOptions : IValidatableObject
{
    public const string Section = "Bootstrap";

    /// <summary>The Google account that becomes the first Admin, while no Admin exists. Optional once one does.</summary>
    public string? FirstAdminEmail { get; set; }

    public EmailAddress? FirstAdmin => EmailAddress.TryCreate(FirstAdminEmail);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(FirstAdminEmail) && FirstAdmin is null)
        {
            yield return new ValidationResult("Bootstrap:FirstAdminEmail is not an email address.", [nameof(FirstAdminEmail)]);
        }
    }
}
