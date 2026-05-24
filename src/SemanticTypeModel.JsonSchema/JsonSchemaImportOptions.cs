namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Options controlling runtime JSON Schema import behavior.
/// </summary>
public sealed class JsonSchemaImportOptions
{
    /// <summary>
    /// Default import options.
    /// </summary>
    public static JsonSchemaImportOptions Default { get; } = new();

    /// <summary>
    /// Optional base URI for schema identifier resolution.
    /// </summary>
    public Uri? BaseUri { get; init; }

    /// <summary>
    /// Behavior used when unsupported keywords are encountered.
    /// </summary>
    public UnsupportedKeywordBehavior UnsupportedKeywordBehavior { get; init; } = UnsupportedKeywordBehavior.PreserveAsAnnotation;

    /// <summary>
    /// Gets a value indicating whether unsupported keywords should be preserved as namespaced annotations.
    /// </summary>
    public bool PreserveUnsupportedKeywordsAsAnnotations { get; init; } = true;
}
