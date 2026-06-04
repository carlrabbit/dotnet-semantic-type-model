namespace SemanticTypeModel.Core.Inspection;

/// <summary>
/// Configures deterministic human-readable semantic model inspection output.
/// </summary>
public sealed class SemanticTextOptions
{
    /// <summary>
    /// Gets or sets the inspection detail level.
    /// </summary>
    public SemanticTextDetail Detail { get; set; } = SemanticTextDetail.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether diagnostics should be included when supplied to inspection overloads.
    /// </summary>
    public bool IncludeDiagnostics { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether annotations should be included.
    /// </summary>
    public bool IncludeAnnotations { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether constraints should be included.
    /// </summary>
    public bool IncludeConstraints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether source metadata should be included.
    /// </summary>
    public bool IncludeSource { get; set; }
}
