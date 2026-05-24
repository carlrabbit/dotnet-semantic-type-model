namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Behavior for unsupported JSON Schema keywords encountered during import.
/// </summary>
public enum UnsupportedKeywordBehavior
{
    /// <summary>
    /// Preserve unsupported keywords as namespaced annotations and emit informational diagnostics.
    /// </summary>
    PreserveAsAnnotation,

    /// <summary>
    /// Ignore unsupported keywords and emit warning diagnostics.
    /// </summary>
    IgnoreWithWarning,

    /// <summary>
    /// Reject unsupported keywords and emit error diagnostics.
    /// </summary>
    RejectWithError,
}
