using System.ComponentModel;
namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Legacy compatibility options for runtime JSON Schema import. JSON Schema import is not a supported canonical model creation path.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
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
