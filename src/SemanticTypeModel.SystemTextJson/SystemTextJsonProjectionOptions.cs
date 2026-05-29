namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Configures System.Text.Json integration while preserving the boundary between semantic names and serialization names.
/// </summary>
public sealed class SystemTextJsonProjectionOptions
{
    /// <summary>Gets or sets whether System.Text.Json attributes are imported as annotations.</summary>
    public bool ImportSystemTextJsonAttributes { get; set; } = true;
    /// <summary>Gets or sets whether JsonPropertyName is stored as serialization-name metadata.</summary>
    public bool UseJsonPropertyNameAsSerializationName { get; set; } = true;
    /// <summary>Gets or sets whether JsonPropertyName also replaces the semantic member name.</summary>
    public bool UseJsonPropertyNameAsSemanticName { get; set; }
    /// <summary>Gets or sets whether unsupported converter metadata is preserved as annotations.</summary>
    public bool PreserveUnsupportedConverterMetadata { get; set; } = true;
    /// <summary>Gets or sets whether the source generator emits a JsonSerializerContext.</summary>
    public bool GenerateJsonSerializerContext { get; set; }
    /// <summary>Gets or sets the generated JsonSerializerContext type name.</summary>
    public string? GeneratedContextName { get; set; }
}
