using System.Diagnostics.CodeAnalysis;
using System.Text;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Import;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticSeverity;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies JSON Schema runtime import fixture coverage.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaImportTests
{
    /// <summary>
    /// Fixture 1: simple object schema with required, nullable, and annotations.
    /// </summary>
    [Test]
    public async Task Fixture_1_simple_object_should_preserve_requiredness_and_nullability()
    {
        var json = /*lang=json,strict*/ """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "Customer",
              "title": "Customer",
              "description": "Customer record",
              "type": "object",
              "required": ["id"],
              "properties": {
                "id": { "type": "string" },
                "age": { "type": "integer" },
                "active": { "type": "boolean" },
                "nickname": { "type": ["string", "null"] }
              }
            }
            """;

        JsonSchemaImportResult result = JsonSchemaImporter.Import(json);
        var root = result.Model.Root as ObjectShape;

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(root).IsNotNull();
        _ = await Assert.That(root!.Properties.Single(static p => p.Name == "id").IsRequired).IsTrue();
        _ = await Assert.That(root.Properties.Single(static p => p.Name == "nickname").IsNullable).IsTrue();
        _ = await Assert.That(root.Annotations.Any(static annotation => annotation.Key == "schema.title")).IsTrue();
        _ = await Assert.That(root.Annotations.Any(static annotation => annotation.Key == "schema.description")).IsTrue();
    }

    /// <summary>
    /// Fixture 2: form/editor metadata via ui annotations survives import.
    /// </summary>
    [Test]
    public async Task Fixture_2_form_editor_schema_should_preserve_ui_metadata_annotations()
    {
        var json = /*lang=json,strict*/ """
            {
              "$id": "Profile",
              "type": "object",
              "properties": {
                "contact": {
                  "type": "string",
                  "format": "email",
                  "ui:category": "Contact",
                  "ui:order": 2
                },
                "website": {
                  "type": "string",
                  "format": "uri"
                },
                "birthDate": {
                  "type": "string",
                  "format": "date"
                }
              }
            }
            """;

        JsonSchemaImportResult result = JsonSchemaImporter.Import(json);
        var root = result.Model.Root as ObjectShape;
        PropertyShape contact = root!.Properties.Single(static p => p.Name == "contact");
        var contactScalar = contact.Type?.Resolve(result.Model) as ScalarShape;

        _ = await Assert.That(contactScalar).IsNotNull();
        _ = await Assert.That(contactScalar!.Constraints.Entries.Any(static entry => entry.Key == "format" && entry.Value == "email")).IsTrue();
        _ = await Assert.That(contact.Annotations.Any(static annotation => annotation.Key == "ui.category")).IsTrue();
        _ = await Assert.That(contact.Annotations.Any(static annotation => annotation.Key == "ui.order")).IsTrue();
    }

    /// <summary>
    /// Fixture 3: defs and refs preserve stable named references.
    /// </summary>
    [Test]
    public async Task Fixture_3_defs_and_refs_should_resolve_reusable_references()
    {
        var json = /*lang=json,strict*/ """
            {
              "$id": "Root",
              "type": "object",
              "properties": {
                "node": { "$ref": "#/$defs/Node" }
              },
              "$defs": {
                "Node": {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string" },
                    "next": { "$ref": "#/$defs/Node" }
                  }
                }
              }
            }
            """;

        JsonSchemaImportResult result = JsonSchemaImporter.Import(json);
        var node = result.Model.GetShape("Node") as ObjectShape;
        ShapeRef? next = node!.Properties.Single(static p => p.Name == "next").Type;

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UNRESOLVED_LOCAL_REF")).IsFalse();
        _ = await Assert.That(next!.Identifier).IsEqualTo("Node");
    }

    /// <summary>
    /// Fixture 6: unsupported keyword behavior can preserve annotations with diagnostics.
    /// </summary>
    [Test]
    public async Task Fixture_6_unsupported_keywords_should_follow_configured_behavior()
    {
        var json = /*lang=json,strict*/ """
            {
              "$id": "Root",
              "type": "object",
              "x-note": "custom",
              "properties": {
                "name": { "type": "string", "x-extra": { "display": "Name" } }
              }
            }
            """;

        JsonSchemaImportResult preserve = JsonSchemaImporter.Import(
            json,
            new JsonSchemaImportOptions
            {
                UnsupportedKeywordBehavior = UnsupportedKeywordBehavior.PreserveAsAnnotation,
                PreserveUnsupportedKeywordsAsAnnotations = true,
            });

        JsonSchemaImportResult reject = JsonSchemaImporter.Import(
            json,
            new JsonSchemaImportOptions
            {
                UnsupportedKeywordBehavior = UnsupportedKeywordBehavior.RejectWithError,
                PreserveUnsupportedKeywordsAsAnnotations = false,
            });

        var root = preserve.Model.Root as ObjectShape;

        _ = await Assert.That(root!.Annotations.Any(static annotation => annotation.Key == "jsonSchema.keyword.x-note")).IsTrue();
        _ = await Assert.That(preserve.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UNSUPPORTED_KEYWORD_PRESERVED")).IsTrue();
        _ = await Assert.That(reject.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UNSUPPORTED_KEYWORD_REJECTED" && diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsTrue();
    }

    /// <summary>
    /// Verifies stream-based runtime import entrypoint.
    /// </summary>
    [Test]
    public async Task Import_should_support_stream_entrypoint()
    {
        var json = /*lang=json,strict*/ """{"$id":"Root","type":"string"}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        JsonSchemaImportResult result = JsonSchemaImporter.Import(stream);

        _ = await Assert.That(result.Model.Root).IsTypeOf<ScalarShape>();
    }
}
