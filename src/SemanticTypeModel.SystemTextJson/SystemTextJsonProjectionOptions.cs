using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

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
    /// <summary>Gets or sets the metadata source used when SemanticTypeModel customizes JSON property names.</summary>
    public SemanticJsonPropertyNameSource PropertyNameSource { get; set; } = SemanticJsonPropertyNameSource.ExistingJsonContract;
    /// <summary>Gets or sets the transformation pipeline run before the System.Text.Json domain semantic model is created.</summary>
    public SchemaTransformationPipeline? Transformations { get; set; }
    /// <summary>Gets or sets transformation pipeline execution options.</summary>
    public SchemaPipelineOptions? PipelineOptions { get; set; }

    /// <summary>Configures the derivation to run the default canonical semantic model transformations.</summary>
    public SystemTextJsonProjectionOptions UseDefaultTransformations()
    {
        Transformations = SchemaTransformationPipeline.Create().UseCoreDefaults();
        return this;
    }
}
