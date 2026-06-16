using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Domain;

namespace SemanticTypeModel.JsonSchema.Export;

/// <summary>
/// Projects a unified canonical <see cref="TypeSchemaModel"/> or JSON Schema domain model to a JSON Schema Draft 2020-12 document.
/// </summary>
public sealed class JsonSchemaExporter : ISchemaProjection<string>
{
    /// <summary>Projects the unified canonical model to a JSON Schema Draft 2020-12 string.</summary>
    public string Project(TypeSchemaModel model, SchemaProjectionContext context)
    {
        JsonSchemaExportResult result = Export(model);
        foreach (SchemaDiagnostic diagnostic in result.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic);
        }

        return result.Document.RootElement.GetRawText();
    }

    /// <summary>Projects the unified canonical model to a JSON Schema Draft 2020-12 string.</summary>
    public string Project(TypeSchemaModel model)
    {
        _ = GetType();
        JsonSchemaExportResult result = Export(model);
        return result.Document.RootElement.GetRawText();
    }

    /// <summary>Exports a derived JSON Schema domain semantic model to a JSON Schema Draft 2020-12 document.</summary>
    public static JsonSchemaExportResult Export(JsonSchemaSemanticModel model, JsonSchemaExportOptions? options = null)
    {
        return JsonSchemaDomainExporter.Export(model, options);
    }

    /// <summary>Derives and exports a unified canonical semantic model to a JSON Schema Draft 2020-12 document.</summary>
    public static JsonSchemaExportResult Export(TypeSchemaModel model, JsonSchemaExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        JsonSchemaExportOptions exportOptions = options ?? JsonSchemaExportOptions.Default;
        SemanticDerivationResult<JsonSchemaSemanticModel> derivation = model.DeriveJsonSchemaModel(derive => derive.SchemaId = exportOptions.SchemaId);
        return JsonSchemaDomainExporter.Export(derivation.Model, exportOptions);
    }
}
