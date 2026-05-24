using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies JSON Schema roundtrip behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaRoundtripTests
{
    /// <summary>
    /// Verifies the supported JSON Schema baseline can roundtrip through the canonical model.
    /// </summary>
    [Test]
    public async Task Import_then_export_should_preserve_supported_baseline_shape_information()
    {
        var json = /*lang=json,strict*/ """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "Root",
              "title": "Order",
              "type": "object",
              "required": ["status", "items"],
              "properties": {
                "status": {
                  "$ref": "#/$defs/Status"
                },
                "items": {
                  "type": "array",
                  "items": {
                    "$ref": "#/$defs/Item"
                  }
                },
                "notes": {
                  "oneOf": [
                    { "type": "string", "maxLength": 50 },
                    { "type": "null" }
                  ]
                }
              },
              "$defs": {
                "Status": {
                  "enum": ["new", "processing", "done"]
                },
                "Item": {
                  "type": "object",
                  "properties": {
                    "sku": { "type": "string" }
                  }
                }
              }
            }
            """;

        TypeSchemaModel imported = new JsonSchemaImporter(json).Load();
        var exported = new JsonSchemaExporter().Project(imported);
        TypeSchemaModel roundTripped = new JsonSchemaImporter(exported).Load();

        var root = roundTripped.Root as ObjectShape;
        var status = roundTripped.GetShape("Status") as EnumShape;
        var item = roundTripped.GetShape("Item") as ObjectShape;

        _ = await Assert.That(root).IsNotNull();
        _ = await Assert.That(status).IsNotNull();
        _ = await Assert.That(item).IsNotNull();

        PropertyShape notes = root!.Properties.Single(static property => property.Name == "notes");
        PropertyShape items = root.Properties.Single(static property => property.Name == "items");
        var itemArray = items.Type?.Resolve(roundTripped) as ArrayShape;

        _ = await Assert.That(itemArray).IsNotNull();
        var skuShape = item!.Properties.Single(static property => property.Name == "sku").Type?.Resolve(roundTripped) as ScalarShape;

        _ = await Assert.That(skuShape).IsNotNull();
        _ = await Assert.That(root.Properties.Count).IsEqualTo(3);
        _ = await Assert.That(status!.Values.Count).IsEqualTo(3);
        _ = await Assert.That(skuShape!.Kind).IsEqualTo(ScalarKind.String);
        _ = await Assert.That(notes.IsNullable).IsTrue();
        _ = await Assert.That(itemArray!.Items?.Identifier).IsEqualTo("Item");
    }

    /// <summary>
    /// Verifies exported schema text is valid JSON.
    /// </summary>
    [Test]
    public async Task Exported_schema_should_remain_valid_json()
    {
        TypeSchemaModel model = new JsonSchemaImporter(/*lang=json,strict*/ """
            {
              "$id": "Root",
              "type": "string",
              "default": "sample"
            }
            """).Load();

        var exported = new JsonSchemaExporter().Project(model);
        using var document = JsonDocument.Parse(exported);

        _ = await Assert.That(document.RootElement.GetProperty("default").GetString()).IsEqualTo("sample");
    }
}
