namespace SemanticTypeModel.Core.Inspection;

/// <summary>
/// Configures deterministic human-readable diagnostic inspection output.
/// </summary>
public sealed class DiagnosticTextOptions
{
    /// <summary>
    /// Gets or sets the inspection detail level.
    /// </summary>
    public SemanticTextDetail Detail { get; set; } = SemanticTextDetail.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether related model paths should be included.
    /// </summary>
    public bool IncludeRelatedPaths { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether source metadata should be included.
    /// </summary>
    public bool IncludeSource { get; set; }
}
