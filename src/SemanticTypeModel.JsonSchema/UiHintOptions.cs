namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Options controlling UI hint validation and normalization behavior.
/// </summary>
public sealed class UiHintOptions
{
    /// <summary>
    /// Default UI hint options.
    /// </summary>
    public static UiHintOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether widget hints should be inferred when absent.
    /// </summary>
    public bool InferWidgetHints { get; init; }

    /// <summary>
    /// Gets a value indicating whether unknown or unsupported UI hint keys should be diagnosed as errors.
    /// </summary>
    public bool StrictKnownHintsOnly { get; init; }

    /// <summary>
    /// Gets a value indicating whether <c>ui.title</c> should override <c>schema.title</c> when exporting JSON Schema title text.
    /// </summary>
    public bool PreferUiTitleOverDisplayName { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether inferred widget hints may overwrite explicitly assigned <c>ui.widget</c>.
    /// </summary>
    public bool OverwriteExplicitWidgetHint { get; init; }
}
