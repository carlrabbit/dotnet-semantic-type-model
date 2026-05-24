namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Supported JSON Schema dialects for runtime import and export.
/// </summary>
public enum JsonSchemaDialect
{
    /// <summary>
    /// JSON Schema Draft 2020-12.
    /// </summary>
    Draft202012,
}

/// <summary>
/// Constants for JSON Schema dialect metadata.
/// </summary>
public static class JsonSchemaDialectUris
{
    /// <summary>
    /// JSON Schema Draft 2020-12 meta-schema URI.
    /// </summary>
    public const string Draft202012 = "https://json-schema.org/draft/2020-12/schema";
}
