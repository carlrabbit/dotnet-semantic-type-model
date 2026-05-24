using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Import;
using Hardening = SemanticTypeModel.Abstractions.Hardening;
using Legacy = SemanticTypeModel.Abstractions.Model;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticSeverity;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies that the runtime JSON Schema baseline composes with the hardened transformation pipeline.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaPipelineCompositionTests
{
    /// <summary>
    /// Imports a JSON Schema fixture, runs it through the hardened transformation pipeline, and exports it again.
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
        Hardening.TypeSchemaModel hardeningModel = LegacyJsonSchemaBridge.ToHardening(imported.Model);

        Hardening.SchemaPipelineResult pipeline = await SchemaTransformationPipeline.Create()
            .Use(new NormalizeNamesTransformation())
            .Use(new NormalizeAnnotationsTransformation())
            .Use(new ValidateModelTransformation())
            .RunAsync(
                hardeningModel,
                new Hardening.SchemaPipelineOptions
                {
                    InitialDiagnostics = imported.Diagnostics,
                    ContinueOnError = true,
                });

        Legacy.TypeSchemaModel bridgedBack = LegacyJsonSchemaBridge.ToLegacy(pipeline.Model);
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(bridgedBack);
        JsonSchemaImportResult roundtripped = JsonSchemaImporter.Import(exported.Document);
        Legacy.ObjectShape root = (Legacy.ObjectShape)roundtripped.Model.Root!;

        _ = await Assert.That(imported.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(pipeline.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(exported.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(root.Properties.Single(static property => property.Name == "id").IsRequired).IsTrue();
        _ = await Assert.That(root.Properties.Single(static property => property.Name == "nickname").IsNullable).IsTrue();
        _ = await Assert.That(exported.Document.RootElement.GetProperty("$schema").GetString()).IsEqualTo(JsonSchemaDialectUris.Draft202012);
    }

    private static class LegacyJsonSchemaBridge
    {
        public static Hardening.TypeSchemaModel ToHardening(Legacy.TypeSchemaModel legacyModel)
        {
            Legacy.ObjectShape root = legacyModel.Root as Legacy.ObjectShape
                ?? throw new InvalidOperationException("The composition test bridge currently expects an object root.");
            string rootId = legacyModel.RootIdentifier ?? "Root";
            List<Hardening.TypeDefinition> types = [];

            foreach (Legacy.PropertyShape property in root.Properties)
            {
                if (property.Type?.Resolve(legacyModel) is not Legacy.ScalarShape scalarShape)
                {
                    throw new InvalidOperationException("The composition test bridge currently expects scalar properties.");
                }

                string scalarIdValue = $"{rootId}_{property.Name}_Scalar";
                types.Add(new Hardening.ScalarTypeDefinition
                {
                    Id = new Hardening.TypeId(scalarIdValue),
                    Name = scalarIdValue,
                    Kind = Hardening.TypeKind.Scalar,
                    Nullability = scalarShape.IsNullable ? Hardening.Nullability.Nullable : Hardening.Nullability.NonNullable,
                    Annotations = ToHardeningAnnotations(scalarShape.Annotations, Hardening.AnnotationScope.Type),
                    ScalarKind = ToHardeningScalarKind(scalarShape.Kind),
                    Format = TryGetConstraintValue(scalarShape.Constraints, "format"),
                });
            }

            types.Add(new Hardening.ObjectTypeDefinition
            {
                Id = new Hardening.TypeId(rootId),
                Name = rootId,
                Kind = Hardening.TypeKind.Object,
                Nullability = Hardening.Nullability.NonNullable,
                Annotations = ToHardeningAnnotations(root.Annotations, Hardening.AnnotationScope.Type),
                Properties =
                [
                    .. root.Properties.Select(property => new Hardening.PropertyDefinition
                    {
                        Id = new Hardening.PropertyId($"{rootId}_{property.Name}"),
                        Name = property.Name,
                        Type = new Hardening.TypeRef(new Hardening.TypeId($"{rootId}_{property.Name}_Scalar")),
                        Cardinality = new Hardening.Cardinality
                        {
                            IsRequired = property.IsRequired,
                            AllowsNull = property.IsNullable,
                        },
                        Mutability = Hardening.Mutability.Mutable,
                        Constraints = new Hardening.ConstraintSet(),
                        Annotations = ToHardeningAnnotations(property.Annotations, Hardening.AnnotationScope.Member),
                    }),
                ],
                Keys = [],
                Relationships = [],
            });

            return new Hardening.TypeSchemaModel
            {
                Id = new Hardening.SchemaModelId(rootId),
                Types = types,
                TypesById = types.ToDictionary(static type => type.Id, static type => type),
                Annotations = new Hardening.AnnotationBag(),
            };
        }

        public static Legacy.TypeSchemaModel ToLegacy(Hardening.TypeSchemaModel model)
        {
            Hardening.ObjectTypeDefinition root = model.Types.OfType<Hardening.ObjectTypeDefinition>().Single(static type => type.Id.Value == type.Name);
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
                        Type = Legacy.ShapeRef.FromInline(ToLegacyScalar((Hardening.ScalarTypeDefinition)model.GetType(property.Type.Id))),
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

        private static Legacy.ScalarShape ToLegacyScalar(Hardening.ScalarTypeDefinition scalar)
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

        private static Hardening.AnnotationBag ToHardeningAnnotations(IReadOnlyList<Legacy.SchemaAnnotation> annotations, Hardening.AnnotationScope scope)
        {
            return new Hardening.AnnotationBag
            {
                Items =
                [
                    .. annotations.Select(annotation => new Hardening.Annotation
                    {
                        Key = new Hardening.AnnotationKey(annotation.Key.Replace(':', '.')),
                        Value = TryParseJsonLiteral(annotation.Value),
                        Scope = scope,
                        Source = Hardening.AnnotationSource.Imported,
                    }),
                ],
            };
        }

        private static IReadOnlyList<Legacy.SchemaAnnotation> ToLegacyAnnotations(Hardening.AnnotationBag annotations)
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
                using JsonDocument document = JsonDocument.Parse(value);
                return document.RootElement.ValueKind switch
                {
                    JsonValueKind.String => document.RootElement.GetString(),
                    JsonValueKind.Number when document.RootElement.TryGetInt32(out int number) => number,
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

        private static Hardening.ScalarKind ToHardeningScalarKind(Legacy.ScalarKind kind)
        {
            return kind switch
            {
                Legacy.ScalarKind.Boolean => Hardening.ScalarKind.Boolean,
                Legacy.ScalarKind.Integer => Hardening.ScalarKind.Integer,
                Legacy.ScalarKind.Number => Hardening.ScalarKind.Number,
                Legacy.ScalarKind.String => Hardening.ScalarKind.String,
                Legacy.ScalarKind.Null => Hardening.ScalarKind.Unknown,
                _ => Hardening.ScalarKind.Unknown,
            };
        }

        private static Legacy.ScalarKind ToLegacyScalarKind(Hardening.ScalarKind kind)
        {
            return kind switch
            {
                Hardening.ScalarKind.Boolean => Legacy.ScalarKind.Boolean,
                Hardening.ScalarKind.Integer => Legacy.ScalarKind.Integer,
                Hardening.ScalarKind.Number or Hardening.ScalarKind.Decimal => Legacy.ScalarKind.Number,
                Hardening.ScalarKind.String => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Date => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Time => Legacy.ScalarKind.String,
                Hardening.ScalarKind.DateTime => Legacy.ScalarKind.String,
                Hardening.ScalarKind.DateTimeOffset => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Duration => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Guid => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Binary => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Json => Legacy.ScalarKind.String,
                Hardening.ScalarKind.Unknown => Legacy.ScalarKind.String,
                _ => Legacy.ScalarKind.String,
            };
        }
    }
}
