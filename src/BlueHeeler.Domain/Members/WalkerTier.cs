namespace BlueHeeler.Domain.Members;

/// <summary>A walker's training level. It does not limit sign-up; shifts target a number of Tier 2+ walkers.</summary>
public enum WalkerTier
{
    /// <summary>Entry level.</summary>
    Tier1 = 1,

    /// <summary>Counts toward a shift's Tier 2 target.</summary>
    Tier2 = 2,

    /// <summary>Most experienced; also counts toward the Tier 2 target.</summary>
    Tier3 = 3,
}
