using System.Text.Json;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Domain;
using Hardening = SemanticTypeModel.Abstractions.Hardening;
using Legacy = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.JsonSchema.Derivation;

/// <summary>JSON Schema domain derivation entry points.</summary>
public static class JsonSchemaDerivationExtensions
{
    /// <summary>Derives a JSON Schema domain semantic model from a hardened code-first canonical semantic model.</summary>
    public static SemanticDerivationResult<JsonSchemaSemanticModel> DeriveJsonSchemaModel(
        this Hardening.TypeSchemaModel model,
        Action<JsonSchemaDerivationOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        JsonSchemaDerivationOptions options = new();
        if (configure is null)
        {
            _ = options.UseDefaultTransformations();
        }
        else
        {
            configure(options);
        }

        SemanticModelTransformationResult transformed = options.Transformations.Run(model, options.PipelineOptions, cancellationToken);
        JsonSchemaDomainMapper mapper = new(options.SchemaId, transformed.Diagnostics);
        JsonSchemaSemanticModel domainModel = mapper.Map(transformed.Model);

        return new SemanticDerivationResult<JsonSchemaSemanticModel>
        {
            Model = domainModel,
            Diagnostics = domainModel.Diagnostics,
            Trace = transformed.Trace,
        };
    }

    /// <summary>Derives a JSON Schema domain semantic model from a legacy generated model by first adapting it to the hardened model.</summary>
    public static SemanticDerivationResult<JsonSchemaSemanticModel> DeriveJsonSchemaModel(
        this Legacy.TypeSchemaModel model,
        Action<JsonSchemaDerivationOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        TypeSchemaModelResult adapted = LegacyTypeSchemaModelAdapter.Adapt(model);
        SemanticDerivationResult<JsonSchemaSemanticModel> result = adapted.Model!.DeriveJsonSchemaModel(configure, cancellationToken);
        return result with { Diagnostics = [.. adapted.Diagnostics, .. result.Diagnostics], Model = result.Model with { Diagnostics = [.. adapted.Diagnostics, .. result.Model.Diagnostics] } };
    }

    private sealed class JsonSchemaDomainMapper(Uri? schemaId, IReadOnlyList<Hardening.SchemaDiagnostic> initialDiagnostics)
    {
        private readonly List<Hardening.SchemaDiagnostic> _diagnostics = [.. initialDiagnostics];
        private Hardening.TypeSchemaModel? _model;

        public JsonSchemaSemanticModel Map(Hardening.TypeSchemaModel model)
        {
            _model = model;
            Hardening.TypeDefinition rootType = ResolveRoot(model);
            JsonSchemaNode root = MapType(rootType);
            Dictionary<string, JsonSchemaNode> definitions = new(StringComparer.Ordinal);

            foreach (Hardening.TypeDefinition type in model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
            {
                if (type.Id == rootType.Id)
                {
                    continue;
                }

                definitions[type.Id.Value] = MapType(type);
            }

            return new JsonSchemaSemanticModel
            {
                DialectUri = JsonSchemaDialectUris.Draft202012,
                Id = schemaId ?? new Uri(rootType.Id.Value, UriKind.RelativeOrAbsolute),
                Root = root,
                Definitions = definitions,
                Diagnostics = [.. _diagnostics],
            };
        }

        private static Hardening.TypeDefinition ResolveRoot(Hardening.TypeSchemaModel model)
        {
            return model.TryGetType(new Hardening.TypeId(model.Id.Value))
                ?? model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal).FirstOrDefault()
                ?? throw new InvalidOperationException("Cannot derive a JSON Schema model from an empty semantic model.");
        }

        private JsonSchemaNode MapType(Hardening.TypeDefinition type)
        {
            return type switch
            {
                Hardening.ObjectTypeDefinition obj => MapObject(obj),
                Hardening.ScalarTypeDefinition scalar => MapScalar(scalar),
                Hardening.EnumTypeDefinition enumType => MapEnum(enumType),
                Hardening.ArrayTypeDefinition array => MapArray(array),
                Hardening.DictionaryTypeDefinition dictionary => MapDictionary(dictionary),
                Hardening.UnionTypeDefinition union => MapUnion(union),
                Hardening.ReferenceTypeDefinition reference => new JsonSchemaCompositionNode
                {
                    Name = type.Name,
                    Title = type.DisplayName,
                    Description = type.Description,
                    Kind = JsonSchemaCompositionKind.OneOf,
                    Alternatives = [JsonSchemaSchemaRef.FromReference(reference.Target.Id.Value)],
                },
                Hardening.IntersectionTypeDefinition intersection => UnsupportedNode(type, "JSONSCHEMA_DERIVE_UNSUPPORTED_ALLOF", $"Intersection '{intersection.Name}' cannot be represented by baseline JSON Schema projection."),
                _ => UnsupportedNode(type, "JSONSCHEMA_DERIVE_UNSUPPORTED_TYPE", $"Type kind '{type.Kind}' is not supported by baseline JSON Schema projection."),
            };
        }

        private JsonSchemaObjectNode MapObject(Hardening.ObjectTypeDefinition type)
        {
            var additionalAllowed = true;
            if (GetStringAnnotation(type.Annotations, "runtime.additionalPropertiesAllowed") is { } legacyAdditional)
            {
                additionalAllowed = string.Equals(legacyAdditional, "true", StringComparison.OrdinalIgnoreCase);
            }

            return new JsonSchemaObjectNode
            {
                Name = type.Name,
                Title = type.DisplayName ?? GetStringAnnotation(type.Annotations, "schema.title") ?? GetStringAnnotation(type.Annotations, "title"),
                Description = type.Description ?? GetStringAnnotation(type.Annotations, "schema.description") ?? GetStringAnnotation(type.Annotations, "description"),
                AdditionalPropertiesAllowed = additionalAllowed,
                Properties = [.. type.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal).Select(MapProperty)],
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaProperty MapProperty(Hardening.PropertyDefinition property)
        {
            return new JsonSchemaProperty
            {
                Name = property.Name,
                Schema = MapReference(property.Type, $"/properties/{property.Name}"),
                IsRequired = property.Cardinality.IsRequired,
                IsNullable = property.Cardinality.AllowsNull,
                Title = property.DisplayName ?? GetStringAnnotation(property.Annotations, "schema.title") ?? GetStringAnnotation(property.Annotations, "title"),
                Description = property.Description ?? GetStringAnnotation(property.Annotations, "schema.description") ?? GetStringAnnotation(property.Annotations, "description"),
                Constraints = MapConstraints(property.Constraints),
                Annotations = MapProjectionAnnotations(property.Annotations),
            };
        }

        private static JsonSchemaScalarNode MapScalar(Hardening.ScalarTypeDefinition type)
        {
            var jsonType = type.ScalarKind switch
            {
                Hardening.ScalarKind.Boolean => "boolean",
                Hardening.ScalarKind.Integer => "integer",
                Hardening.ScalarKind.Number => "number",
                Hardening.ScalarKind.Decimal => "number",
                Hardening.ScalarKind.Json => "object",
                Hardening.ScalarKind.Unknown => "object",
                Hardening.ScalarKind.String => "string",
                Hardening.ScalarKind.Date => "string",
                Hardening.ScalarKind.Time => "string",
                Hardening.ScalarKind.DateTime => "string",
                Hardening.ScalarKind.DateTimeOffset => "string",
                Hardening.ScalarKind.Duration => "string",
                Hardening.ScalarKind.Guid => "string",
                Hardening.ScalarKind.Binary => "string",
                _ => "string",
            };

            var format = type.Format ?? type.ScalarKind switch
            {
                Hardening.ScalarKind.Date => "date",
                Hardening.ScalarKind.Time => "time",
                Hardening.ScalarKind.DateTime or Hardening.ScalarKind.DateTimeOffset => "date-time",
                Hardening.ScalarKind.Duration => "duration",
                Hardening.ScalarKind.Guid => "uuid",
                Hardening.ScalarKind.Binary => "binary",
                Hardening.ScalarKind.Boolean or Hardening.ScalarKind.String or Hardening.ScalarKind.Integer or Hardening.ScalarKind.Number or Hardening.ScalarKind.Decimal or Hardening.ScalarKind.Json or Hardening.ScalarKind.Unknown => null,
                _ => null,
            };

            return new JsonSchemaScalarNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.Description,
                Type = jsonType,
                Format = format,
                IsNullable = type.Nullability.AllowsNull,
                Constraints = new JsonSchemaConstraintSet(),
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private static JsonSchemaEnumNode MapEnum(Hardening.EnumTypeDefinition type)
        {
            return new JsonSchemaEnumNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.Description,
                Values = [.. type.Values.Select(static value => ToJsonElement(value.Value))],
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaArrayNode MapArray(Hardening.ArrayTypeDefinition type)
        {
            return new JsonSchemaArrayNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.Description,
                Items = MapReference(type.ItemType, $"/types/{type.Id.Value}/items"),
                Constraints = new JsonSchemaConstraintSet { MinItems = type.MinItems, MaxItems = type.MaxItems, UniqueItems = type.UniqueItems },
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaDictionaryNode MapDictionary(Hardening.DictionaryTypeDefinition type)
        {
            return new JsonSchemaDictionaryNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.Description,
                Values = MapReference(type.ValueType, $"/types/{type.Id.Value}/values"),
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaCompositionNode MapUnion(Hardening.UnionTypeDefinition type)
        {
            if (type.Options.Count == 0)
            {
                AddDiagnostic("JSONSCHEMA_DERIVE_EMPTY_ALTERNATIVES", $"Union '{type.Name}' has no alternatives.", $"/types/{type.Id.Value}/options");
            }

            return new JsonSchemaCompositionNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.Description,
                Kind = type.Semantics == Hardening.UnionSemantics.AnyOf ? JsonSchemaCompositionKind.AnyOf : JsonSchemaCompositionKind.OneOf,
                Alternatives = [.. type.Options.OrderBy(static option => option.Id.Value, StringComparer.Ordinal).Select(option => MapReference(option, $"/types/{type.Id.Value}/options"))],
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaSchemaRef MapReference(Hardening.TypeRef typeRef, string path)
        {
            if (_model?.TryGetType(typeRef.Id) is null)
            {
                AddDiagnostic("JSONSCHEMA_DERIVE_UNRESOLVED_ALTERNATIVE", $"Referenced type '{typeRef.Id.Value}' was not found.", path);
            }

            return JsonSchemaSchemaRef.FromReference(typeRef.Id.Value);
        }

        private JsonSchemaScalarNode UnsupportedNode(Hardening.TypeDefinition type, string code, string message)
        {
            AddDiagnostic(code, message, $"/types/{type.Id.Value}");
            return new JsonSchemaScalarNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.Description,
                Type = "object",
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private void AddDiagnostic(string code, string message, string path)
        {
            _diagnostics.Add(new Hardening.SchemaDiagnostic
            {
                Severity = Hardening.SchemaDiagnosticSeverity.Warning,
                Code = code,
                Message = message,
                Stage = Hardening.SchemaDiagnosticStage.Projection,
                ModelPath = path,
                Source = path,
                ProjectionTarget = Hardening.ProjectionTarget.JsonSchema,
            });
        }

        private static JsonSchemaConstraintSet MapConstraints(Hardening.ConstraintSet constraints)
        {
            return new JsonSchemaConstraintSet
            {
                MinLength = constraints.String?.MinLength,
                MaxLength = constraints.String?.MaxLength,
                Pattern = constraints.String?.Pattern,
                Minimum = constraints.Numeric?.Minimum,
                Maximum = constraints.Numeric?.Maximum,
                ExclusiveMinimum = constraints.Numeric?.ExclusiveMinimum ?? false,
                ExclusiveMaximum = constraints.Numeric?.ExclusiveMaximum ?? false,
                MultipleOf = constraints.Numeric?.MultipleOf,
                MinItems = constraints.Array?.MinItems,
                MaxItems = constraints.Array?.MaxItems,
                UniqueItems = constraints.Array?.UniqueItems ?? false,
                MinProperties = constraints.Object?.MinProperties,
                MaxProperties = constraints.Object?.MaxProperties,
            };
        }

        private static Dictionary<string, JsonElement> MapProjectionAnnotations(Hardening.AnnotationBag bag)
        {
            return bag.Items
                .Where(static annotation => annotation.Scope == Hardening.AnnotationScope.Projection || annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal))
                .OrderBy(static annotation => annotation.Key.Value, StringComparer.Ordinal)
                .ToDictionary(static annotation => annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal) ? annotation.Key.Value["jsonSchema.keyword.".Length..] : annotation.Key.Value, static annotation => ToJsonElement(annotation.Value), StringComparer.Ordinal);
        }

        private static string? GetStringAnnotation(Hardening.AnnotationBag bag, string key)
        {
            return bag.Items.Where(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal)).Select(static annotation => annotation.Value?.ToString()).LastOrDefault(static value => !string.IsNullOrWhiteSpace(value));
        }


        private static JsonElement ToJsonElement(object? value)
        {
            if (value is JsonElement element)
            {
                return element.Clone();
            }

            if (value is string text)
            {
                try
                {
                    using var parsed = JsonDocument.Parse(text);
                    return parsed.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return JsonSerializer.SerializeToElement(text);
                }
            }

            return value is null
                ? JsonSerializer.SerializeToElement<object?>(null)
                : JsonSerializer.SerializeToElement(value, value.GetType());
        }
    }
}
