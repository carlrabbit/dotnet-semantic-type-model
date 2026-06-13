using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;
using Canonical = SemanticTypeModel.Abstractions.Canonical;
using Legacy = SemanticTypeModel.Abstractions.Model;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Canonical.SchemaDiagnosticSeverity;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies that the runtime JSON Schema baseline composes with the canonical semantic model transformation pipeline.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaPipelineCompositionTests
{
    /// <summary>
    /// Imports a JSON Schema fixture, runs it through the canonical semantic model transformation pipeline, and exports it again.
    /// </summary>
    [Test]
    public async Task Import_pipeline_export_should_compose_for_simple_json_schema_fixture()
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

        JsonSchemaImportResult imported = JsonSchemaImporter.Import(json);
        Canonical.TypeSchemaModel canonicalModel = LegacyJsonSchemaBridge.ToCanonical(imported.Model);

        Canonical.SchemaPipelineResult pipeline = await SchemaTransformationPipeline.Create()
            .Use(new NormalizeNamesTransformation())
            .Use(new NormalizeAnnotationsTransformation())
            .Use(new ValidateModelTransformation())
            .RunAsync(
                canonicalModel,
                new Canonical.SchemaPipelineOptions
                {
                    InitialDiagnostics = imported.Diagnostics,
                    ContinueOnError = true,
                });

        Legacy.TypeSchemaModel bridgedBack = LegacyJsonSchemaBridge.ToLegacy(pipeline.Model);
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(bridgedBack);
        JsonSchemaImportResult roundtripped = JsonSchemaImporter.Import(exported.Document);
        var root = (Legacy.ObjectShape)roundtripped.Model.Root!;

        _ = await Assert.That(imported.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(pipeline.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(exported.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(root.Properties.Single(static property => property.Name == "id").IsRequired).IsTrue();
        _ = await Assert.That(root.Properties.Single(static property => property.Name == "nickname").IsNullable).IsTrue();
        _ = await Assert.That(exported.Document.RootElement.GetProperty("$schema").GetString()).IsEqualTo(JsonSchemaDialectUris.Draft202012);
    }

    private static class LegacyJsonSchemaBridge
    {
        public static Canonical.TypeSchemaModel ToCanonical(Legacy.TypeSchemaModel legacyModel)
        {
            Legacy.ObjectShape root = legacyModel.Root as Legacy.ObjectShape
                ?? throw new InvalidOperationException("The composition test bridge currently expects an object root.");
            var rootId = legacyModel.RootIdentifier ?? "Root";
            List<Canonical.TypeDefinition> types = [];

            foreach (Legacy.PropertyShape property in root.Properties)
            {
                if (property.Type?.Resolve(legacyModel) is not Legacy.ScalarShape scalarShape)
                {
                    throw new InvalidOperationException("The composition test bridge currently expects scalar properties.");
                }

                var scalarIdValue = $"{rootId}_{property.Name}_Scalar";
                types.Add(new Canonical.ScalarTypeDefinition
                {
                    Id = new Canonical.TypeId(scalarIdValue),
                    Name = scalarIdValue,
                    Kind = Canonical.TypeKind.Scalar,
                    Nullability = scalarShape.IsNullable ? Canonical.Nullability.Nullable : Canonical.Nullability.NonNullable,
                    Annotations = ToCanonicalAnnotations(scalarShape.Annotations, Canonical.AnnotationScope.Type),
                    ScalarKind = ToCanonicalScalarKind(scalarShape.Kind),
                    Format = TryGetConstraintValue(scalarShape.Constraints, "format"),
                });
            }

            types.Add(new Canonical.ObjectTypeDefinition
            {
                Id = new Canonical.TypeId(rootId),
                Name = rootId,
                Kind = Canonical.TypeKind.Object,
                Nullability = Canonical.Nullability.NonNullable,
                Annotations = ToCanonicalAnnotations(root.Annotations, Canonical.AnnotationScope.Type),
                Properties =
                [
                    .. root.Properties.Select(property => new Canonical.PropertyDefinition
                    {
                        Id = new Canonical.PropertyId($"{rootId}_{property.Name}"),
                        Name = property.Name,
                        Type = new Canonical.TypeRef(new Canonical.TypeId($"{rootId}_{property.Name}_Scalar")),
                        Cardinality = new Canonical.Cardinality
                        {
                            IsRequired = property.IsRequired,
                            AllowsNull = property.IsNullable,
                        },
                        Mutability = Canonical.Mutability.Mutable,
                        Constraints = new Canonical.ConstraintSet(),
                        Annotations = ToCanonicalAnnotations(property.Annotations, Canonical.AnnotationScope.Member),
                    }),
                ],
                Keys = [],
                Relationships = [],
            });

            return new Canonical.TypeSchemaModel
            {
                Id = new Canonical.SchemaModelId(rootId),
                Types = types,
                TypesById = types.ToDictionary(static type => type.Id, static type => type),
                Annotations = new Canonical.AnnotationBag(),
            };
        }

        public static Legacy.TypeSchemaModel ToLegacy(Canonical.TypeSchemaModel model)
        {
            Canonical.ObjectTypeDefinition root = model.Types.OfType<Canonical.ObjectTypeDefinition>().Single(static type => type.Id.Value == type.Name);
            Legacy.ObjectShape legacyRoot = new()
            {
                Identifier = root.Id.Value,
                Annotations = ToLegacyAnnotations(root.Annotations),
                Properties =
                [
                    .. root.Properties.Select(property => new Legacy.PropertyShape
                    {
                        Name = property.Name,
                        IsRequired = property.Cardinality.IsRequired,
                        IsNullable = property.Cardinality.AllowsNull,
                        Type = Legacy.ShapeRef.FromInline(ToLegacyScalar((Canonical.ScalarTypeDefinition)model.GetType(property.Type.Id))),
                        Annotations = ToLegacyAnnotations(property.Annotations),
                    }),
                ],
            };

            return new Legacy.TypeSchemaModel(
                new Dictionary<string, Legacy.TypeShape>(StringComparer.Ordinal)
                {
                    [root.Id.Value] = legacyRoot,
                },
                root.Id.Value);
        }

        private static Legacy.ScalarShape ToLegacyScalar(Canonical.ScalarTypeDefinition scalar)
        {
            List<Legacy.SchemaAnnotation> annotations = [.. ToLegacyAnnotations(scalar.Annotations)];
            if (!string.IsNullOrWhiteSpace(scalar.Format))
            {
                annotations.Add(new Legacy.SchemaAnnotation("format", scalar.Format));
            }

            return new Legacy.ScalarShape
            {
                Kind = ToLegacyScalarKind(scalar.ScalarKind),
                IsNullable = scalar.Nullability.AllowsNull,
                Annotations = annotations,
            };
        }

        private static Canonical.AnnotationBag ToCanonicalAnnotations(IReadOnlyList<Legacy.SchemaAnnotation> annotations, Canonical.AnnotationScope scope)
        {
            return new Canonical.AnnotationBag
            {
                Items =
                [
                    .. annotations.Select(annotation => new Canonical.Annotation
                    {
                        Key = new Canonical.AnnotationKey(annotation.Key.Replace(':', '.')),
                        Value = TryParseJsonLiteral(annotation.Value),
                        Scope = scope,
                        Source = Canonical.AnnotationSource.Imported,
                    }),
                ],
            };
        }

        private static IReadOnlyList<Legacy.SchemaAnnotation> ToLegacyAnnotations(Canonical.AnnotationBag annotations)
        {
            return
            [
                .. annotations.Items.Select(annotation => new Legacy.SchemaAnnotation(annotation.Key.Value, ToLegacyAnnotationValue(annotation.Value))),
            ];
        }

        private static object? TryParseJsonLiteral(string value)
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                return document.RootElement.ValueKind switch
                {
                    JsonValueKind.String => document.RootElement.GetString(),
                    JsonValueKind.Number when document.RootElement.TryGetInt32(out var number) => number,
                    JsonValueKind.Number => document.RootElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.Array => value,
                    JsonValueKind.Object => value,
                    JsonValueKind.Undefined => value,
                    _ => value,
                };
            }
            catch (JsonException)
            {
                return value;
            }
        }

        private static string ToLegacyAnnotationValue(object? value)
        {
            return value switch
            {
                null => "null",
                string text => text,
                _ => JsonSerializer.Serialize(value),
            };
        }

        private static string? TryGetConstraintValue(Legacy.ConstraintSet constraints, string key)
        {
            Legacy.ConstraintEntry? entry = constraints.Entries.FirstOrDefault(entry => entry.Key == key);
            return entry?.Value;
        }

        private static Canonical.ScalarKind ToCanonicalScalarKind(Legacy.ScalarKind kind)
        {
            return kind switch
            {
                Legacy.ScalarKind.Boolean => Canonical.ScalarKind.Boolean,
                Legacy.ScalarKind.Integer => Canonical.ScalarKind.Integer,
                Legacy.ScalarKind.Number => Canonical.ScalarKind.Number,
                Legacy.ScalarKind.String => Canonical.ScalarKind.String,
                Legacy.ScalarKind.Null => Canonical.ScalarKind.Unknown,
                _ => Canonical.ScalarKind.Unknown,
            };
        }

        private static Legacy.ScalarKind ToLegacyScalarKind(Canonical.ScalarKind kind)
        {
            return kind switch
            {
                Canonical.ScalarKind.Boolean => Legacy.ScalarKind.Boolean,
                Canonical.ScalarKind.Integer => Legacy.ScalarKind.Integer,
                Canonical.ScalarKind.Number or Canonical.ScalarKind.Decimal => Legacy.ScalarKind.Number,
                Canonical.ScalarKind.String => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Date => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Time => Legacy.ScalarKind.String,
                Canonical.ScalarKind.DateTime => Legacy.ScalarKind.String,
                Canonical.ScalarKind.DateTimeOffset => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Duration => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Guid => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Binary => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Json => Legacy.ScalarKind.String,
                Canonical.ScalarKind.Unknown => Legacy.ScalarKind.String,
                _ => Legacy.ScalarKind.String,
            };
        }
    }
}
