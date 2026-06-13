using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Canonical.SchemaDiagnosticSeverity;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies JSON Schema import/export roundtrip behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaRoundtripTests
{
    /// <summary>
    /// Roundtrips a schema containing defs, refs, unions, and constraints.
    /// </summary>
    [Test]
    public async Task Roundtrip_should_preserve_supported_semantic_shape_information()
    {
        var input = /*lang=json,strict*/ """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "Order",
              "type": "object",
              "required": ["status", "items"],
              "properties": {
                "status": { "$ref": "#/$defs/Status" },
                "items": {
                  "type": "array",
                  "items": { "$ref": "#/$defs/Item" },
                  "minItems": 1
                },
                "notes": {
                  "oneOf": [
                    { "type": "string", "maxLength": 50 },
                    { "type": "null" }
                  ]
                },
                "mode": {
                  "anyOf": [
                    { "type": "string" },
                    { "type": "integer" }
                  ]
                }
              },
              "$defs": {
                "Status": { "enum": ["new", "processing", "done"] },
                "Item": {
                  "type": "object",
                  "properties": {
                    "sku": { "type": "string" },
                    "quantity": { "type": "integer", "minimum": 1 }
                  }
                }
              }
            }
            """;

        JsonSchemaImportResult imported = JsonSchemaImporter.Import(input);
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(imported.Model);
        JsonSchemaImportResult roundtripped = JsonSchemaImporter.Import(exported.Document);

        var root = roundtripped.Model.Root as ObjectShape;
        PropertyShape notes = root!.Properties.Single(static property => property.Name == "notes");
        PropertyShape items = root.Properties.Single(static property => property.Name == "items");
        var itemArray = items.Type?.Resolve(roundtripped.Model) as ArrayShape;
        var status = roundtripped.Model.GetShape("Status") as EnumShape;

        _ = await Assert.That(imported.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(root.Properties.Single(static property => property.Name == "status").IsRequired).IsTrue();
        _ = await Assert.That(notes.IsNullable).IsTrue();
        _ = await Assert.That(itemArray!.Constraints.Entries.Any(static entry => entry.Key == "minItems" && entry.Value == "1")).IsTrue();
        _ = await Assert.That(status!.Values.Count).IsEqualTo(3);
        _ = await Assert.That(exported.Document.RootElement.GetProperty("$schema").GetString()).IsEqualTo(JsonSchemaDialectUris.Draft202012);
    }

    /// <summary>
    /// Ensures unresolved and remote refs produce diagnostics instead of hard failures.
    /// </summary>
    [Test]
    public async Task Import_should_emit_diagnostics_for_unresolved_and_remote_refs()
    {
        var schema = /*lang=json,strict*/ """
            {
              "$id": "Root",
              "type": "object",
              "properties": {
                "missing": { "$ref": "#/$defs/NotFound" },
                "remote": { "$ref": "https://example.com/schemas/value.json" }
              }
            }
            """;

        JsonSchemaImportResult result = JsonSchemaImporter.Import(schema);

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UNRESOLVED_LOCAL_REF" && diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsTrue();
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_REMOTE_REF_UNSUPPORTED" && diagnostic.Severity == SchemaDiagnosticSeverity.Warning)).IsTrue();
    }
}
