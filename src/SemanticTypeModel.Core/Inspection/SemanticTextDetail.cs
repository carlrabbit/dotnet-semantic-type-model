namespace SemanticTypeModel.Core.Inspection;

/// <summary>
/// Controls the amount of deterministic human-readable inspection detail emitted for semantic model text.
/// </summary>
public enum SemanticTextDetail
{
    /// <summary>
    /// Emits a compact overview suitable for quick console output.
    /// </summary>
    Summary,

    /// <summary>
    /// Emits the default development-loop detail level.
    /// </summary>
    Normal,

    /// <summary>
    /// Emits expanded detail intended for tests and troubleshooting.
    /// </summary>
    Detailed,
}
