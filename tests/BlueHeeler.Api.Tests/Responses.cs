namespace BlueHeeler.Api.Tests;

public sealed record MemberResponse(Guid Id, string Name, string Email, string Role, int? Tier, string Status);

public sealed record MeResponse(Guid Id, string Name, string Email, string Role, string[] Permissions);
