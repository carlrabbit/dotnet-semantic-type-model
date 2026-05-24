using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;
using SemanticTypeModel.JsonSchema.Export;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticSeverity;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies JSON Schema runtime export fixture coverage.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaExportTests
{
    /// <summary>
    /// Fixture 4: composition support for oneOf and anyOf, plus deferred allOf preservation.
    /// </summary>
    [Test]
    public async Task Fixture_4_composition_should_export_union_semantics_and_preserved_allOf()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new UnionShape
                {
                    Options =
                    [
                        ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                        ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.Integer }),
                    ],
                    Annotations =
                    [
                        new SchemaAnnotation("jsonSchema.unionSemantics", "anyOf"),
                        new SchemaAnnotation("jsonSchema.allOf", /*lang=json,strict*/ """[{"type":"object"}]"""),
                    ],
                })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);
        JsonElement root = result.Document.RootElement;

        _ = await Assert.That(root.GetProperty("anyOf").GetArrayLength()).IsEqualTo(2);
        _ = await Assert.That(root.GetProperty("allOf").GetArrayLength()).IsEqualTo(1);
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_EXPORT_ALLOF_PRESERVED" && diagnostic.Severity == SchemaDiagnosticSeverity.Warning)).IsTrue();
    }

    /// <summary>
    /// Fixture 5: string, numeric, array, and object constraints are exported.
    /// </summary>
    [Test]
    public async Task Fixture_5_constraints_should_export_back_to_json_schema_keywords()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new ObjectShape
                {
                    AdditionalPropertiesAllowed = false,
                    Constraints = new ConstraintSet
                    {
                        Entries =
                        [
                            new ConstraintEntry("minProperties", "1"),
                            new ConstraintEntry("maxProperties", "3"),
                        ],
                    },
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "name",
                            IsRequired = true,
                            Type = ShapeRef.FromInline(new ScalarShape
                            {
                                Kind = ScalarKind.String,
                                Constraints = new ConstraintSet
                                {
                                    Entries = [new ConstraintEntry("minLength", "2"), new ConstraintEntry("maxLength", "10"), new ConstraintEntry("pattern", "^[A-Z].+")],
                                },
                            }),
                        },
                        new PropertyShape
                        {
                            Name = "score",
                            Type = ShapeRef.FromInline(new ScalarShape
                            {
                                Kind = ScalarKind.Number,
                                Constraints = new ConstraintSet
                                {
                                    Entries = [new ConstraintEntry("minimum", "0"), new ConstraintEntry("exclusiveMaximum", "100"), new ConstraintEntry("multipleOf", "0.5")],
                                },
                            }),
                        },
                        new PropertyShape
                        {
                            Name = "tags",
                            Type = ShapeRef.FromInline(new ArrayShape
                            {
                                Items = ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                                Constraints = new ConstraintSet
                                {
                                    Entries = [new ConstraintEntry("minItems", "1"), new ConstraintEntry("maxItems", "5"), new ConstraintEntry("uniqueItems", "true")],
                                },
                            }),
                        },
                    ],
                })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);
        JsonElement root = result.Document.RootElement;
        JsonElement name = root.GetProperty("properties").GetProperty("name");
        JsonElement score = root.GetProperty("properties").GetProperty("score");
        JsonElement tags = root.GetProperty("properties").GetProperty("tags");

        _ = await Assert.That(root.GetProperty("additionalProperties").GetBoolean()).IsFalse();
        _ = await Assert.That(root.GetProperty("minProperties").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(name.GetProperty("minLength").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(score.GetProperty("exclusiveMaximum").GetInt32()).IsEqualTo(100);
        _ = await Assert.That(tags.GetProperty("maxItems").GetInt32()).IsEqualTo(5);
        _ = await Assert.That(tags.GetProperty("uniqueItems").GetBoolean()).IsTrue();
    }

    /// <summary>
    /// Export API returns JsonDocument with Draft 2020-12 dialect metadata.
    /// </summary>
    [Test]
    public async Task Export_should_return_result_document_and_diagnostics()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape("Root", new ScalarShape { Kind = ScalarKind.String })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(
            model,
            new JsonSchemaExportOptions
            {
                SchemaId = new Uri("urn:schema:root"),
                Dialect = JsonSchemaDialect.Draft202012,
            });

        _ = await Assert.That(result.Document.RootElement.GetProperty("$schema").GetString()).IsEqualTo(JsonSchemaDialectUris.Draft202012);
        _ = await Assert.That(result.Document.RootElement.GetProperty("$id").GetString()).IsEqualTo("urn:schema:root");
    }
}
