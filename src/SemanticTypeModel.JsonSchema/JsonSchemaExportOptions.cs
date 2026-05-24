namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Options controlling runtime JSON Schema export behavior.
/// </summary>
public sealed class JsonSchemaExportOptions
{
    /// <summary>
    /// Default export options.
    /// </summary>
    public static JsonSchemaExportOptions Default { get; } = new();

    /// <summary>
    /// Optional schema identifier to emit as <c>$id</c>.
    /// </summary>
    public Uri? SchemaId { get; init; }

    /// <summary>
    /// Dialect to emit. Only Draft 2020-12 is supported.
    /// </summary>
    public JsonSchemaDialect Dialect { get; init; } = JsonSchemaDialect.Draft202012;

    /// <summary>
    /// Gets a value indicating whether projection and unsupported keyword annotations should be emitted.
    /// </summary>
    public bool IncludeProjectionAnnotations { get; init; } = true;
}
