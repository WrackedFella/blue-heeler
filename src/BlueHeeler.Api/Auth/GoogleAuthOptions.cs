using System.ComponentModel.DataAnnotations;

namespace BlueHeeler.Api.Auth;

/// <summary>The Google OAuth client the app signs in with.</summary>
internal sealed class GoogleAuthOptions
{
    public const string Section = "Authentication:Google";

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}
