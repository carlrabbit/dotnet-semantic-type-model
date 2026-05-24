namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Controls how UI/editor annotations are emitted during JSON Schema export.
/// </summary>
public enum JsonSchemaUiMode
{
    /// <summary>
    /// Emits no UI/editor projection hints.
    /// </summary>
    None = 0,

    /// <summary>
    /// Emits only generic <c>ui:*</c> extension annotations when enabled.
    /// </summary>
    GenericExtensions = 1,

    /// <summary>
    /// Emits generic extensions and selected JSON-editor-compatible keywords when enabled.
    /// </summary>
    JsonEditorCompatible = 2,
}
