using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Import;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies JSON Schema import behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaImportTests
{
    /// <summary>
    /// Verifies import of objects, definitions, annotations, constraints, and nullable properties.
    /// </summary>
    [Test]
    public async Task Load_should_import_object_schema_with_defs_annotations_constraints_and_nullable_property()
    {
        var json = /*lang=json,strict*/ """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "Root",
              "title": "Customer",
              "description": "Customer record",
              "type": "object",
              "required": ["id"],
              "properties": {
                "id": {
                  "$ref": "#/$defs/Identifier",
                  "description": "Business identifier"
                },
                "nickname": {
                  "oneOf": [
                    { "type": "string", "minLength": 2 },
                    { "type": "null" }
                  ]
                }
              },
              "additionalProperties": false,
              "$defs": {
                "Identifier": {
                  "type": "string",
                  "minLength": 3,
                  "title": "Identifier"
                }
              }
            }
            """;

        TypeSchemaModel model = new JsonSchemaImporter(json).Load();

        var root = model.Root as ObjectShape;
        var identifier = model.GetShape("Identifier") as ScalarShape;
        PropertyShape idProperty = root.Properties.Single(static property => property.Name == "id");
        PropertyShape nickname = root.Properties.Single(static property => property.Name == "nickname");

        _ = await Assert.That(model.RootIdentifier).IsEqualTo("Root");
        _ = await Assert.That(root).IsNotNull();
        _ = await Assert.That(identifier).IsNotNull();
        _ = await Assert.That(root!.AdditionalPropertiesAllowed).IsFalse();
        _ = await Assert.That(root.Annotations.Count).IsEqualTo(2);
        _ = await Assert.That(idProperty.IsRequired).IsTrue();
        _ = await Assert.That(idProperty.Type?.Identifier).IsEqualTo("Identifier");
        _ = await Assert.That(idProperty.Annotations.Single(static annotation => annotation.Key == "description").Value).IsEqualTo("Business identifier");
        _ = await Assert.That(nickname.IsNullable).IsTrue();
        _ = await Assert.That(identifier!.Kind).IsEqualTo(ScalarKind.String);
        _ = await Assert.That(identifier.Constraints.Entries.Single(static entry => entry.Key == "minLength").Value).IsEqualTo("3");
    }

    /// <summary>
    /// Verifies schema-valued additional properties import as a dictionary shape.
    /// </summary>
    [Test]
    public async Task Load_should_import_dictionary_shape_from_schema_valued_additionalProperties()
    {
        var json = /*lang=json,strict*/ """
            {
              "$id": "Root",
              "type": "object",
              "additionalProperties": {
                "type": "integer"
              }
            }
            """;

        TypeSchemaModel model = new JsonSchemaImporter(json).Load();
        var root = model.Root as DictionaryShape;
        var value = root?.Values?.Resolve(model) as ScalarShape;

        _ = await Assert.That(root).IsNotNull();
        _ = await Assert.That(value).IsNotNull();
        _ = await Assert.That(value!.Kind).IsEqualTo(ScalarKind.Integer);
    }

    /// <summary>
    /// Verifies multi-branch oneOf imports as a union shape.
    /// </summary>
    [Test]
    public async Task Load_should_import_union_when_oneOf_has_multiple_non_null_options()
    {
        var json = /*lang=json,strict*/ """
            {
              "$id": "Root",
              "oneOf": [
                { "type": "string" },
                { "type": "integer" }
              ]
            }
            """;

        TypeSchemaModel model = new JsonSchemaImporter(json).Load();
        var root = model.Root as UnionShape;

        _ = await Assert.That(root).IsNotNull();
        _ = await Assert.That(root!.Options.Count).IsEqualTo(2);
        _ = await Assert.That(root.Options.All(static option => option.Inline is ScalarShape)).IsTrue();
    }

    /// <summary>
    /// Verifies import fails when a named JSON Schema reference cannot be resolved.
    /// </summary>
    [Test]
    public async Task Load_should_throw_when_ref_cannot_be_resolved()
    {
        var json = /*lang=json,strict*/ """
            {
              "$id": "Root",
              "type": "object",
              "properties": {
                "child": {
                  "$ref": "#/$defs/Missing"
                }
              }
            }
            """;

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(new JsonSchemaImporter(json).Load()));

        _ = await Assert.That(exception).IsNotNull();
        _ = await Assert.That(exception!.Message).IsEqualTo("Shape reference 'Missing' cannot be resolved in this model.");
    }
}
