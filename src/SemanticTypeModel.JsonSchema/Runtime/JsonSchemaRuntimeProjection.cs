using System.Globalization;
using System.Text.Json;
using SemanticTypeModel.JsonSchema.Export;
using Canonical = SemanticTypeModel.Abstractions.Canonical;
using Legacy = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.JsonSchema.Runtime;

/// <summary>
/// Projects the runtime canonical semantic model to JSON Schema through the existing JSON Schema exporter.
/// </summary>
public sealed class JsonSchemaRuntimeProjection : Canonical.ISchemaProjection<JsonSchemaExportResult>, Canonical.IProjectionCapabilityProvider
{
    /// <inheritdoc />
    public JsonSchemaExportResult Project(Canonical.TypeSchemaModel model, Canonical.SchemaProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        Legacy.TypeSchemaModel legacyModel = LegacyRuntimeBridge.ToLegacy(model, context);
        JsonSchemaExportResult result = JsonSchemaExporter.Export(legacyModel);
        foreach (Canonical.SchemaDiagnostic diagnostic in result.Diagnostics)
        {
            context.Diagnostics.Add(diagnostic);
        }

        return result;
    }

    /// <inheritdoc />
    public Canonical.ProjectionCompatibilityContract GetCapabilities()
    {
        return Canonical.ProjectionCapabilityCatalog.ForTarget(Canonical.ProjectionTarget.JsonSchema);
    }

    private static class LegacyRuntimeBridge
    {
        public static Legacy.TypeSchemaModel ToLegacy(Canonical.TypeSchemaModel model, Canonical.SchemaProjectionContext context)
        {
            var rootIdentifier = ResolveRootIdentifier(model, context);
            Dictionary<string, Legacy.TypeShape> shapes = new(StringComparer.Ordinal);

            foreach (Canonical.TypeDefinition type in model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
            {
                shapes[type.Id.Value] = ConvertType(type, context);
            }

            return new Legacy.TypeSchemaModel(shapes, rootIdentifier);
        }

        private static string? ResolveRootIdentifier(Canonical.TypeSchemaModel model, Canonical.SchemaProjectionContext context)
        {
            if (model.TypesById.ContainsKey(new Canonical.TypeId(model.Id.Value)))
            {
                return model.Id.Value;
            }

            var fallback = model.Types.Count > 0 ? model.Types[0].Id.Value : null;
            if (fallback is not null)
            {
                context.Diagnostics.Add(new Canonical.SchemaDiagnostic
                {
                    Severity = Canonical.SchemaDiagnosticSeverity.Warning,
                    Code = "STM3201",
                    Message = $"Runtime model id '{model.Id.Value}' did not match a type id. The JSON Schema runtime projection used '{fallback}' as the root type.",
                    Stage = Canonical.SchemaDiagnosticStage.Projection,
                    ModelPath = "/",
                    ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
                });
            }

            return fallback;
        }

        private static Legacy.TypeShape ConvertType(Canonical.TypeDefinition type, Canonical.SchemaProjectionContext context)
        {
            IReadOnlyList<Legacy.SchemaAnnotation> annotations = ConvertAnnotations(type.Annotations);
            Legacy.ConstraintSet constraints = ConvertConstraints(type);

            return type switch
            {
                Canonical.ObjectTypeDefinition obj => ConvertObject(obj, annotations, constraints, context),
                Canonical.ScalarTypeDefinition scalar => new Legacy.ScalarShape
                {
                    Identifier = scalar.Id.Value,
                    Annotations = annotations,
                    Constraints = constraints,
                    Kind = ConvertScalarKind(scalar.ScalarKind),
                    IsNullable = scalar.Nullability.AllowsNull,
                },
                Canonical.EnumTypeDefinition @enum => new Legacy.EnumShape
                {
                    Identifier = @enum.Id.Value,
                    Annotations = annotations,
                    Constraints = constraints,
                    Values = [.. @enum.Values.Select(value => ConvertEnumValue(@enum, value, context))],
                },
                Canonical.ArrayTypeDefinition array => new Legacy.ArrayShape
                {
                    Identifier = array.Id.Value,
                    Annotations = annotations,
                    Constraints = constraints,
                    Items = Legacy.ShapeRef.FromIdentifier(array.ItemType.Id.Value),
                },
                Canonical.DictionaryTypeDefinition dictionary => ConvertDictionary(dictionary, annotations, constraints, context),
                Canonical.UnionTypeDefinition union => ConvertUnion(union, annotations, constraints, context),
                Canonical.ReferenceTypeDefinition reference => new Legacy.UnionShape
                {
                    Identifier = reference.Id.Value,
                    Annotations = annotations,
                    Constraints = constraints,
                    Options = [Legacy.ShapeRef.FromIdentifier(reference.Target.Id.Value)],
                },
                Canonical.IntersectionTypeDefinition intersection => ConvertIntersection(intersection, annotations, constraints, context),
                _ => ConvertUnsupported(type, annotations, constraints, context),
            };
        }

        private static Legacy.ObjectShape ConvertObject(
            Canonical.ObjectTypeDefinition obj,
            IReadOnlyList<Legacy.SchemaAnnotation> annotations,
            Legacy.ConstraintSet constraints,
            Canonical.SchemaProjectionContext context)
        {
            if (obj.Keys.Count > 0 || obj.Relationships.Count > 0 || obj.ComputedMembers.Count > 0 || obj.Composition.AllOf.Count > 0 || obj.Semantics.Role != Canonical.EntityRole.Unspecified || obj.Semantics.IsAggregateRoot || obj.Semantics.IsValueObject)
            {
                context.Diagnostics.Add(new Canonical.SchemaDiagnostic
                {
                    Severity = Canonical.SchemaDiagnosticSeverity.Warning,
                    Code = "STM3202",
                    Message = $"Object type '{obj.Id.Value}' contains semantic members that the current JSON Schema runtime projection does not project directly. JSON Schema export continues with object properties and annotations.",
                    Stage = Canonical.SchemaDiagnosticStage.Projection,
                    ModelPath = Canonical.ModelPath.ForType(obj.Id),
                    ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
                });
            }

            return new Legacy.ObjectShape
            {
                Identifier = obj.Id.Value,
                Annotations = annotations,
                Constraints = constraints,
                AdditionalPropertiesAllowed = TryGetAdditionalPropertiesAllowed(obj.Annotations) ?? true,
                Properties =
                [
                    .. obj.Properties.Select(property => new Legacy.PropertyShape
                    {
                        Name = property.Name,
                        IsRequired = property.Cardinality.IsRequired,
                        IsNullable = property.Cardinality.AllowsNull,
                        Type = Legacy.ShapeRef.FromIdentifier(property.Type.Id.Value),
                        Annotations = ConvertAnnotations(property.Annotations),
                    }),
                ],
            };
        }

        private static Legacy.DictionaryShape ConvertDictionary(
            Canonical.DictionaryTypeDefinition dictionary,
            IReadOnlyList<Legacy.SchemaAnnotation> annotations,
            Legacy.ConstraintSet constraints,
            Canonical.SchemaProjectionContext context)
        {
            context.Diagnostics.Add(new Canonical.SchemaDiagnostic
            {
                Severity = Canonical.SchemaDiagnosticSeverity.Info,
                Code = "STM3203",
                Message = $"Dictionary type '{dictionary.Id.Value}' projects to a JSON object with string keys. Non-string key metadata is not represented in the current JSON Schema runtime projection.",
                Stage = Canonical.SchemaDiagnosticStage.Projection,
                ModelPath = Canonical.ModelPath.ForType(dictionary.Id),
                ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
            });

            return new Legacy.DictionaryShape
            {
                Identifier = dictionary.Id.Value,
                Annotations = annotations,
                Constraints = constraints,
                Values = Legacy.ShapeRef.FromIdentifier(dictionary.ValueType.Id.Value),
            };
        }

        private static Legacy.UnionShape ConvertUnion(
            Canonical.UnionTypeDefinition union,
            IReadOnlyList<Legacy.SchemaAnnotation> annotations,
            Legacy.ConstraintSet constraints,
            Canonical.SchemaProjectionContext context)
        {
            if (union.Discriminator is not null || union.Semantics == Canonical.UnionSemantics.AnyOf)
            {
                context.Diagnostics.Add(new Canonical.SchemaDiagnostic
                {
                    Severity = Canonical.SchemaDiagnosticSeverity.Warning,
                    Code = "STM3204",
                    Message = $"Union type '{union.Id.Value}' includes discriminator or anyOf semantics that the current JSON Schema runtime projection approximates as oneOf.",
                    Stage = Canonical.SchemaDiagnosticStage.Projection,
                    ModelPath = Canonical.ModelPath.ForType(union.Id),
                    ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
                });
            }

            return new Legacy.UnionShape
            {
                Identifier = union.Id.Value,
                Annotations = annotations,
                Constraints = constraints,
                Options = [.. union.Options.Select(option => Legacy.ShapeRef.FromIdentifier(option.Id.Value))],
            };
        }

        private static Legacy.UnionShape ConvertIntersection(
            Canonical.IntersectionTypeDefinition intersection,
            IReadOnlyList<Legacy.SchemaAnnotation> annotations,
            Legacy.ConstraintSet constraints,
            Canonical.SchemaProjectionContext context)
        {
            context.Diagnostics.Add(new Canonical.SchemaDiagnostic
            {
                Severity = Canonical.SchemaDiagnosticSeverity.Warning,
                Code = "STM3205",
                Message = $"Intersection type '{intersection.Id.Value}' is approximated as a union because the current legacy JSON Schema exporter has no allOf-aware runtime adapter.",
                Stage = Canonical.SchemaDiagnosticStage.Projection,
                ModelPath = Canonical.ModelPath.ForType(intersection.Id),
                ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
            });

            return new Legacy.UnionShape
            {
                Identifier = intersection.Id.Value,
                Annotations = annotations,
                Constraints = constraints,
                Options = [.. intersection.Members.Select(member => Legacy.ShapeRef.FromIdentifier(member.Id.Value))],
            };
        }

        private static Legacy.ScalarShape ConvertUnsupported(
            Canonical.TypeDefinition type,
            IReadOnlyList<Legacy.SchemaAnnotation> annotations,
            Legacy.ConstraintSet constraints,
            Canonical.SchemaProjectionContext context)
        {
            context.Diagnostics.Add(new Canonical.SchemaDiagnostic
            {
                Severity = Canonical.SchemaDiagnosticSeverity.Error,
                Code = "STM3206",
                Message = $"Type '{type.Id.Value}' of kind '{type.Kind}' is not supported by the current JSON Schema runtime projection adapter.",
                Stage = Canonical.SchemaDiagnosticStage.Projection,
                ModelPath = Canonical.ModelPath.ForType(type.Id),
                ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
            });

            return new Legacy.ScalarShape
            {
                Identifier = type.Id.Value,
                Annotations = annotations,
                Constraints = constraints,
                Kind = Legacy.ScalarKind.String,
            };
        }

        private static string ConvertEnumValue(Canonical.EnumTypeDefinition type, Canonical.EnumValueDefinition value, Canonical.SchemaProjectionContext context)
        {
            if (value.Value is string text)
            {
                return text;
            }

            context.Diagnostics.Add(new Canonical.SchemaDiagnostic
            {
                Severity = Canonical.SchemaDiagnosticSeverity.Warning,
                Code = "STM3207",
                Message = $"Enum value '{value.Name}' on type '{type.Id.Value}' is not a string value. The JSON Schema runtime projection serialized it using its string representation.",
                Stage = Canonical.SchemaDiagnosticStage.Projection,
                ModelPath = Canonical.ModelPath.ForType(type.Id),
                ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
            });

            return Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? value.Name;
        }

        private static IReadOnlyList<Legacy.SchemaAnnotation> ConvertAnnotations(Canonical.AnnotationBag annotations)
        {
            return
            [
                .. annotations.Items.Select(annotation => new Legacy.SchemaAnnotation(annotation.Key.Value, ConvertAnnotationValue(annotation.Value))),
            ];
        }

        private static string ConvertAnnotationValue(object? value)
        {
            return value switch
            {
                null => "null",
                string text => text,
                _ => JsonSerializer.Serialize(value),
            };
        }

        private static Legacy.ConstraintSet ConvertConstraints(Canonical.TypeDefinition type)
        {
            List<Legacy.ConstraintEntry> entries = [];

            switch (type)
            {
                case Canonical.ScalarTypeDefinition scalar when !string.IsNullOrWhiteSpace(scalar.Format):
                    entries.Add(new Legacy.ConstraintEntry("format", scalar.Format));
                    break;
                case Canonical.ArrayTypeDefinition array:
                    if (array.MinItems is not null)
                    {
                        entries.Add(new Legacy.ConstraintEntry("minItems", array.MinItems.Value.ToString(CultureInfo.InvariantCulture)));
                    }

                    if (array.MaxItems is not null)
                    {
                        entries.Add(new Legacy.ConstraintEntry("maxItems", array.MaxItems.Value.ToString(CultureInfo.InvariantCulture)));
                    }

                    if (array.UniqueItems)
                    {
                        entries.Add(new Legacy.ConstraintEntry("uniqueItems", "true"));
                    }

                    break;
                case Canonical.ObjectTypeDefinition obj:
                    var additionalPropertiesAllowed = TryGetAdditionalPropertiesAllowed(obj.Annotations);
                    if (additionalPropertiesAllowed is not null)
                    {
                        entries.Add(new Legacy.ConstraintEntry("additionalProperties", additionalPropertiesAllowed.Value ? "true" : "false"));
                    }

                    break;
                default:
                    break;
            }

            return new Legacy.ConstraintSet { Entries = entries };
        }

        private static bool? TryGetAdditionalPropertiesAllowed(Canonical.AnnotationBag annotations)
        {
            Canonical.Annotation? annotation = annotations.Items.FirstOrDefault(static item => string.Equals(item.Key.Value, "runtime.additionalPropertiesAllowed", StringComparison.Ordinal));
            return annotation?.Value switch
            {
                bool value => value,
                _ => null,
            };
        }

        private static Legacy.ScalarKind ConvertScalarKind(Canonical.ScalarKind kind)
        {
            return kind switch
            {
                Canonical.ScalarKind.Boolean => Legacy.ScalarKind.Boolean,
                Canonical.ScalarKind.Integer => Legacy.ScalarKind.Integer,
                Canonical.ScalarKind.Number => Legacy.ScalarKind.Number,
                Canonical.ScalarKind.Decimal => Legacy.ScalarKind.Number,
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
