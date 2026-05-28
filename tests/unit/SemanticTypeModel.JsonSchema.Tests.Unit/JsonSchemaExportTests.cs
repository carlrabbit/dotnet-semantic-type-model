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
    /// Fixture 1: display metadata and UI title/description precedence are deterministic.
    /// </summary>
    [Test]
    public async Task Fixture_1_ui_title_and_description_should_override_schema_text_when_enabled()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new ObjectShape
                {
                    Annotations =
                    [
                        new SchemaAnnotation("schema.title", "Canonical"),
                        new SchemaAnnotation("schema.description", "Canonical description"),
                        new SchemaAnnotation("ui.title", "\"UI Title\""),
                        new SchemaAnnotation("ui.description", "\"UI Description\""),
                    ],
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "name",
                            Type = ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                        },
                    ],
                })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions
        {
            UiHintOptions = new UiHintOptions { PreferUiTitleOverDisplayName = true },
        });

        _ = await Assert.That(result.Document.RootElement.GetProperty("title").GetString()).IsEqualTo("UI Title");
        _ = await Assert.That(result.Document.RootElement.GetProperty("description").GetString()).IsEqualTo("UI Description");
    }

    /// <summary>
    /// Fixture 2: ordered properties are deterministic and conflicting values are diagnosable.
    /// </summary>
    [Test]
    public async Task Fixture_2_property_order_should_be_deterministic_and_conflicts_diagnosed()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new ObjectShape
                {
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "b",
                            Type = ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                            Annotations = [new SchemaAnnotation("ui.order", "2")],
                        },
                        new PropertyShape
                        {
                            Name = "a",
                            Type = ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                            Annotations = [new SchemaAnnotation("jsonEditor.propertyOrder", "1"), new SchemaAnnotation("ui.order", "3")],
                        },
                    ],
                })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions
        {
            UiExport = new JsonSchemaUiExportOptions
            {
                UiMode = JsonSchemaUiMode.JsonEditorCompatible,
                IncludeJsonEditorCompatibilityAnnotations = true,
            },
        });

        JsonElement properties = result.Document.RootElement.GetProperty("properties");
        string[] names = [.. properties.EnumerateObject().Select(static property => property.Name)];

        _ = await Assert.That(names[0]).IsEqualTo("a");
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UI_PROPERTY_ORDER_CONFLICT")).IsTrue();
    }

    /// <summary>
    /// Fixture 3: enum label mismatch emits diagnostics.
    /// </summary>
    [Test]
    public async Task Fixture_3_enum_labels_should_emit_diagnostic_when_mismatched()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Status",
                new EnumShape
                {
                    Values = ["\"new\"", "\"done\""],
                    Annotations = [new SchemaAnnotation("ui.enumLabels", """["New"]""")],
                })
            .SetRoot("Status")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions
        {
            UiExport = new JsonSchemaUiExportOptions
            {
                UiMode = JsonSchemaUiMode.GenericExtensions,
                IncludeGenericUiAnnotations = true,
            },
        });

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UI_ENUM_LABEL_MISMATCH")).IsTrue();
    }

    /// <summary>
    /// Fixture 4: JSON-editor-compatible mode is opt-in.
    /// </summary>
    [Test]
    public async Task Fixture_4_json_editor_keywords_should_only_emit_in_compatibility_mode()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new ObjectShape
                {
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "name",
                            Type = ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.String }),
                            Annotations = [new SchemaAnnotation("jsonEditor.options", /*lang=json,strict*/ """{"grid_columns":6}"""), new SchemaAnnotation("ui.order", "1")],
                        },
                    ],
                })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult generic = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions
        {
            UiExport = new JsonSchemaUiExportOptions
            {
                UiMode = JsonSchemaUiMode.GenericExtensions,
                IncludeGenericUiAnnotations = true,
            },
        });

        JsonSchemaExportResult compatible = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions
        {
            UiExport = new JsonSchemaUiExportOptions
            {
                UiMode = JsonSchemaUiMode.JsonEditorCompatible,
                IncludeGenericUiAnnotations = true,
                IncludeJsonEditorCompatibilityAnnotations = true,
            },
        });

        JsonElement genericProperty = generic.Document.RootElement.GetProperty("properties").GetProperty("name");
        JsonElement compatibleProperty = compatible.Document.RootElement.GetProperty("properties").GetProperty("name");

        _ = await Assert.That(genericProperty.TryGetProperty("options", out _)).IsFalse();
        _ = await Assert.That(compatibleProperty.TryGetProperty("options", out _)).IsTrue();
    }

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
    /// Fixture 5: unsupported union semantics are diagnosed and fall back to oneOf.
    /// </summary>
    [Test]
    public async Task Fixture_5_unsupported_union_semantics_should_fall_back_to_oneOf_with_diagnostic()
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
                    Annotations = [new SchemaAnnotation("jsonSchema.unionSemantics", "allOf")],
                })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);
        JsonElement root = result.Document.RootElement;
        bool hasUnsupportedUnionDiagnostic = result.Diagnostics.Any(
            static diagnostic =>
                diagnostic.Code == "JSONSCHEMA_EXPORT_UNSUPPORTED_UNION_SEMANTICS" &&
                diagnostic.Severity == SchemaDiagnosticSeverity.Warning);

        _ = await Assert.That(root.TryGetProperty("oneOf", out _)).IsTrue();
        _ = await Assert.That(root.TryGetProperty("anyOf", out _)).IsFalse();
        _ = await Assert.That(hasUnsupportedUnionDiagnostic).IsTrue();
    }

    /// <summary>
    /// Fixture 6: string, numeric, array, and object constraints are exported.
    /// </summary>
    [Test]
    public async Task Fixture_6_constraints_should_export_back_to_json_schema_keywords()
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
    /// Fixture 7: $defs are emitted in deterministic ordinal name order.
    /// </summary>
    [Test]
    public async Task Fixture_7_defs_should_be_ordered_deterministically()
    {
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new ObjectShape
                {
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "z",
                            Type = ShapeRef.FromIdentifier("ZType"),
                        },
                        new PropertyShape
                        {
                            Name = "a",
                            Type = ShapeRef.FromIdentifier("AType"),
                        },
                    ],
                })
            .AddShape("ZType", new ScalarShape { Kind = ScalarKind.String })
            .AddShape("AType", new ScalarShape { Kind = ScalarKind.Integer })
            .SetRoot("Root")
            .Build();

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);
        JsonProperty[] defs = [.. result.Document.RootElement.GetProperty("$defs").EnumerateObject()];

        _ = await Assert.That(defs[0].Name).IsEqualTo("AType");
        _ = await Assert.That(defs[1].Name).IsEqualTo("ZType");
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
