namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Controls projection of UI/editor hints into JSON Schema output.
/// </summary>
public sealed class JsonSchemaUiExportOptions
{
    /// <summary>
    /// Default UI export options.
    /// </summary>
    public static JsonSchemaUiExportOptions Default { get; } = new();

    /// <summary>
    /// Gets the configured UI projection mode.
    /// </summary>
    public JsonSchemaUiMode UiMode { get; init; } = JsonSchemaUiMode.None;

    /// <summary>
    /// Gets a value indicating whether generic <c>ui:*</c> extension annotations should be emitted.
    /// </summary>
    public bool IncludeGenericUiAnnotations { get; init; }

    /// <summary>
    /// Gets a value indicating whether selected JSON-editor-compatible keywords should be emitted.
    /// </summary>
    public bool IncludeJsonEditorCompatibilityAnnotations { get; init; }
}
