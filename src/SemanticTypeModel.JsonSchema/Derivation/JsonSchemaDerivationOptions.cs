using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.JsonSchema.Derivation;

/// <summary>Configures JSON Schema domain semantic model derivation.</summary>
public sealed class JsonSchemaDerivationOptions
{
    /// <summary>Gets the configurable canonical transformation sequence executed before domain mapping.</summary>
    public SchemaTransformationPipeline Transformations { get; } = SchemaTransformationPipeline.Create();

    /// <summary>Gets or sets the JSON Schema document identifier.</summary>
    public Uri? SchemaId { get; set; }

    /// <summary>Gets JSON Schema envelope projection policy configuration.</summary>
    public JsonSchemaEnvelopeProjectionOptions Envelopes { get; } = new();

    /// <summary>Gets or sets canonical transformation pipeline options.</summary>
    public SchemaPipelineOptions PipelineOptions { get; set; } = SchemaPipelineOptions.Default;

    /// <summary>Adds the default core and JSON Schema derivation transformations.</summary>
    public JsonSchemaDerivationOptions UseDefaultTransformations()
    {
        _ = Transformations.UseCoreDefaults().Add(new JsonSchemaUnsupportedCompositionTransformation());
        return this;
    }
}
