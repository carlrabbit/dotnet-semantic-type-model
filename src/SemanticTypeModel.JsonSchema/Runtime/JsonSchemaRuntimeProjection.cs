using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Domain;
using SemanticTypeModel.JsonSchema.Export;
using Model = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.JsonSchema.Runtime;

/// <summary>Projects the runtime canonical semantic model to JSON Schema.</summary>
public sealed class JsonSchemaRuntimeProjection : Model.ISchemaProjection<JsonSchemaExportResult>, Model.IProjectionCapabilityProvider
{
    /// <inheritdoc />
    public JsonSchemaExportResult Project(Model.TypeSchemaModel model, Model.SchemaProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        SemanticDerivationResult<JsonSchemaSemanticModel> derived = model.DeriveJsonSchemaModel();
        foreach (Model.SchemaDiagnostic diagnostic in derived.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic);
        }

        JsonSchemaExportResult result = JsonSchemaDomainExporter.Export(derived.Model);
        foreach (Model.SchemaDiagnostic diagnostic in result.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic);
        }

        return result;
    }

    /// <inheritdoc />
    public Model.ProjectionCompatibilityContract GetCapabilities()
    {
        return Model.ProjectionCapabilityCatalog.ForTarget(Model.ProjectionTarget.JsonSchema);
    }
}
