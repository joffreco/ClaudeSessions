namespace ClaudeSessions.Core.Domain;

/// <summary>What a tab expects from the user, the most pressing last.</summary>
public enum TabAttention
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>Its session finished working while nobody watched it.</summary>
    Finished,

    /// <summary>Its session asks for an approval and is stuck until the user answers.</summary>
    Approval,
}
