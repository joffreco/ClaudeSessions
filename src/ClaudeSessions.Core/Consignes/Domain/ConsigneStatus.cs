namespace ClaudeSessions.Core.Consignes.Domain;

/// <summary>Where a consigne stands in its session's review.</summary>
public enum ConsigneStatus
{
    /// <summary>Reviewed and ready to run.</summary>
    Ready,

    /// <summary>Shown while answer-review checks it; not to be run yet.</summary>
    InReview,
}
