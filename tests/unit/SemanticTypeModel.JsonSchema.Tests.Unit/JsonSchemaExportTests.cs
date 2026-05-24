using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies JSON Schema export behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaExportTests
{
    /// <summary>
    /// Verifies export of root content, definitions, references, annotations, constraints, and nullable properties.
    /// </summary>
    [Test]
    public async Task Project_should_export_root_defs_refs_annotations_constraints_and_nullable_property()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Identifier",
                new ScalarShape
                {
                    Kind = ScalarKind.String,
                    Annotations = [new SchemaAnnotation("title", "Identifier")],
                    Constraints = new ConstraintSet
                    {
                        Entries = [new ConstraintEntry("minLength", "3")],
                    },
                })
            .AddShape(
                "Root",
                new ObjectShape
                {
                    Annotations =
                    [
                        new SchemaAnnotation("title", "Customer"),
                        new SchemaAnnotation("description", "Customer record"),
                    ],
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "id",
                            IsRequired = true,
                            Type = ShapeRef.FromIdentifier("Identifier"),
                            Annotations = [new SchemaAnnotation("description", "Business identifier")],
                        },
                        new PropertyShape
                        {
                            Name = "nickname",
                            IsNullable = true,
                            Type = ShapeRef.FromInline(new ScalarShape
                            {
                                Kind = ScalarKind.String,
                                Constraints = new ConstraintSet
                                {
                                    Entries = [new ConstraintEntry("minLength", "2")],
                                },
                            }),
                        },
                    ],
                    AdditionalPropertiesAllowed = false,
                })
            .SetRoot("Root")
            .Build();

        var json = new JsonSchemaExporter().Project(model);
        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement nicknameOneOf = root.GetProperty("properties").GetProperty("nickname").GetProperty("oneOf");

        _ = await Assert.That(root.GetProperty("$schema").GetString()).IsEqualTo("https://json-schema.org/draft/2020-12/schema");
        _ = await Assert.That(root.GetProperty("title").GetString()).IsEqualTo("Customer");
        _ = await Assert.That(root.GetProperty("required")[0].GetString()).IsEqualTo("id");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("id").GetProperty("$ref").GetString()).IsEqualTo("#/$defs/Identifier");
        _ = await Assert.That(root.GetProperty("$defs").GetProperty("Identifier").GetProperty("minLength").GetInt32()).IsEqualTo(3);
        _ = await Assert.That(root.GetProperty("additionalProperties").GetBoolean()).IsFalse();
        _ = await Assert.That(nicknameOneOf.GetArrayLength()).IsEqualTo(2);
        _ = await Assert.That(nicknameOneOf[1].GetProperty("type").GetString()).IsEqualTo("null");
    }

    /// <summary>
    /// Verifies dedicated export behavior for dictionary and union shapes.
    /// </summary>
    [Test]
    public async Task Project_should_export_dictionary_and_union_shapes()
    {
        TypeSchemaModel dictionaryModel = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new DictionaryShape
                {
                    Values = ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.Integer }),
                })
            .SetRoot("Root")
            .Build();

        TypeSchemaModel unionModel = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new UnionShape
                {
                    Options =
                    [
                        ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                        ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.Integer }),
                    ],
                })
            .SetRoot("Root")
            .Build();

        using var dictionaryDocument = JsonDocument.Parse(new JsonSchemaExporter().Project(dictionaryModel));
        using var unionDocument = JsonDocument.Parse(new JsonSchemaExporter().Project(unionModel));

        _ = await Assert.That(dictionaryDocument.RootElement.GetProperty("additionalProperties").GetProperty("type").GetString()).IsEqualTo("integer");
        _ = await Assert.That(unionDocument.RootElement.GetProperty("oneOf").GetArrayLength()).IsEqualTo(2);
    }
}
