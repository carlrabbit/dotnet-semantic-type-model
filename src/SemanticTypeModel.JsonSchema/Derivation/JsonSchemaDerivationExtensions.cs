using System.Text.Json;
using SemanticTypeModel.Core.Semantics;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Domain;
using Canonical = SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.JsonSchema.Derivation;

/// <summary>JSON Schema domain derivation entry points.</summary>
public static class JsonSchemaDerivationExtensions
{
    /// <summary>Derives a JSON Schema domain semantic model from a code-first canonical semantic model.</summary>
    public static SemanticDerivationResult<JsonSchemaSemanticModel> DeriveJsonSchemaModel(
        this Canonical.TypeSchemaModel model,
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
        JsonSchemaDomainMapper mapper = new(options.SchemaId, transformed.Diagnostics, options.Envelopes.Policies);
        JsonSchemaSemanticModel domainModel = mapper.Map(transformed.Model);

        return new SemanticDerivationResult<JsonSchemaSemanticModel>
        {
            Model = domainModel,
            Diagnostics = domainModel.Diagnostics,
            Trace = transformed.Trace,
        };
    }

    private sealed class JsonSchemaDomainMapper(Uri? schemaId, IReadOnlyList<Canonical.SchemaDiagnostic> initialDiagnostics, IReadOnlyDictionary<string, JsonSchemaEnvelopeProjectionPolicy> envelopePolicies)
    {
        private readonly List<Canonical.SchemaDiagnostic> _diagnostics = [.. initialDiagnostics];
        private Canonical.TypeSchemaModel? _model;

        public JsonSchemaSemanticModel Map(Canonical.TypeSchemaModel model)
        {
            _model = model;
            Canonical.TypeDefinition rootType = ResolveRoot(model);
            if (rootType is Canonical.ObjectTypeDefinition rootObject && TryGetEnvelopePolicy(rootObject, out JsonSchemaEnvelopeProjectionPolicy? rootPolicy))
            {
                if (rootPolicy.RootPolicy == JsonSchemaEnvelopeRootPolicy.Ambiguous)
                {
                    AddDiagnostic("JSONSCHEMA_ENVELOPE_ROOT_AMBIGUOUS", $"Envelope '{rootObject.Name}' selected both envelope and payload roots without explicit policy.", $"/types/{rootObject.Id.Value}");
                }
                else if (rootPolicy.RootPolicy == JsonSchemaEnvelopeRootPolicy.PayloadAsRoot && ResolvePayload(rootObject, rootPolicy) is Canonical.PropertyDefinition payload && model.TryGetType(payload.Type.Id) is Canonical.TypeDefinition payloadRoot)
                {
                    rootType = payloadRoot;
                }
            }
            JsonSchemaNode root = MapType(rootType);
            Dictionary<string, JsonSchemaNode> definitions = new(StringComparer.Ordinal);

            foreach (Canonical.TypeDefinition type in model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
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

        private static Canonical.TypeDefinition ResolveRoot(Canonical.TypeSchemaModel model)
        {
            return model.TryGetType(new Canonical.TypeId(model.Id.Value))
                ?? model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal).FirstOrDefault()
                ?? throw new InvalidOperationException("Cannot derive a JSON Schema model from an empty semantic model.");
        }

        private JsonSchemaNode MapType(Canonical.TypeDefinition type)
        {
            return type switch
            {
                Canonical.ObjectTypeDefinition obj => MapObject(obj),
                Canonical.ScalarTypeDefinition scalar => MapScalar(scalar),
                Canonical.EnumTypeDefinition enumType => MapEnum(enumType),
                Canonical.ArrayTypeDefinition array => MapArray(array),
                Canonical.DictionaryTypeDefinition dictionary => MapDictionary(dictionary),
                Canonical.UnionTypeDefinition union => MapUnion(union),
                Canonical.ReferenceTypeDefinition reference => new JsonSchemaCompositionNode
                {
                    Name = type.Name,
                    Title = type.DisplayName,
                    Description = type.Description,
                    Kind = JsonSchemaCompositionKind.OneOf,
                    Alternatives = [JsonSchemaSchemaRef.FromReference(reference.Target.Id.Value)],
                },
                Canonical.IntersectionTypeDefinition intersection => UnsupportedNode(type, "JSONSCHEMA_DERIVE_UNSUPPORTED_ALLOF", $"Intersection '{intersection.Name}' cannot be represented by baseline JSON Schema projection."),
                _ => UnsupportedNode(type, "JSONSCHEMA_DERIVE_UNSUPPORTED_TYPE", $"Type kind '{type.Kind}' is not supported by baseline JSON Schema projection."),
            };
        }

        private JsonSchemaObjectNode MapObject(Canonical.ObjectTypeDefinition type)
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
                Properties = [.. type.Properties.Where(static property => !HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData)).OrderBy(static property => property.Name, StringComparer.Ordinal).Select(property => MapProperty(type, property))],
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaProperty MapProperty(Canonical.ObjectTypeDefinition owner, Canonical.PropertyDefinition property)
        {
            JsonSchemaSchemaRef schema = MapReference(property.Type, $"/properties/{property.Name}");
            Dictionary<string, JsonElement> annotations = MapProjectionAnnotations(property.Annotations);
            if (IsEnvelopePayload(property) && TryGetEnvelopePolicy(owner, out JsonSchemaEnvelopeProjectionPolicy? policy))
            {
                schema = MapEnvelopePayloadSchema(property, policy);
                if (policy.PayloadRepresentation == JsonSchemaEnvelopePayloadRepresentation.SerializedJsonString)
                {
                    annotations["contentMediaType"] = ToJsonElement("application/json");
                }
            }

            return new JsonSchemaProperty
            {
                Name = property.Name,
                Schema = schema,
                IsRequired = property.Cardinality.IsRequired,
                IsNullable = property.Cardinality.AllowsNull,
                Title = property.DisplayName ?? GetStringAnnotation(property.Annotations, "schema.title") ?? GetStringAnnotation(property.Annotations, "title"),
                Description = property.Description ?? GetStringAnnotation(property.Annotations, "schema.description") ?? GetStringAnnotation(property.Annotations, "description"),
                Constraints = MapConstraints(property.Constraints),
                Annotations = annotations,
            };
        }

        private JsonSchemaSchemaRef MapEnvelopePayloadSchema(Canonical.PropertyDefinition property, JsonSchemaEnvelopeProjectionPolicy policy)
        {
            return policy.PayloadRepresentation switch
            {
                JsonSchemaEnvelopePayloadRepresentation.Inline when _model?.TryGetType(property.Type.Id) is Canonical.TypeDefinition payloadType => JsonSchemaSchemaRef.FromInline(MapType(payloadType)),
                JsonSchemaEnvelopePayloadRepresentation.JsonDocument => JsonSchemaSchemaRef.FromInline(new JsonSchemaScalarNode { Type = "object" }),
                JsonSchemaEnvelopePayloadRepresentation.SerializedJsonString => JsonSchemaSchemaRef.FromInline(new JsonSchemaScalarNode { Type = "string" }),
                JsonSchemaEnvelopePayloadRepresentation.Opaque => JsonSchemaSchemaRef.FromInline(new JsonSchemaScalarNode { Type = "object" }),
                JsonSchemaEnvelopePayloadRepresentation.StructuredReference => MapReference(property.Type, $"/properties/{property.Name}"),
                _ => MapReference(property.Type, $"/properties/{property.Name}"),
            };
        }

        private bool TryGetEnvelopePolicy(Canonical.ObjectTypeDefinition envelope, out JsonSchemaEnvelopeProjectionPolicy policy)
        {
            return envelopePolicies.TryGetValue(envelope.Name, out policy!) || envelopePolicies.TryGetValue(envelope.Id.Value, out policy!);
        }

        private static Canonical.PropertyDefinition? ResolvePayload(Canonical.ObjectTypeDefinition envelope, JsonSchemaEnvelopeProjectionPolicy policy)
        {
            return envelope.Properties.FirstOrDefault(property => string.Equals(property.Name, policy.PayloadPropertyName, StringComparison.Ordinal))
                ?? envelope.Properties.FirstOrDefault(IsEnvelopePayload);
        }

        private static bool IsEnvelopePayload(Canonical.PropertyDefinition property)
        {
            return property.Annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, CoreSemanticAnnotationKeys.EnvelopePayload, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static JsonSchemaScalarNode MapScalar(Canonical.ScalarTypeDefinition type)
        {
            var jsonType = type.ScalarKind switch
            {
                Canonical.ScalarKind.Boolean => "boolean",
                Canonical.ScalarKind.Integer => "integer",
                Canonical.ScalarKind.Number => "number",
                Canonical.ScalarKind.Decimal => "number",
                Canonical.ScalarKind.Json => "object",
                Canonical.ScalarKind.Unknown => "object",
                Canonical.ScalarKind.String => "string",
                Canonical.ScalarKind.Date => "string",
                Canonical.ScalarKind.Time => "string",
                Canonical.ScalarKind.DateTime => "string",
                Canonical.ScalarKind.DateTimeOffset => "string",
                Canonical.ScalarKind.Duration => "string",
                Canonical.ScalarKind.Guid => "string",
                Canonical.ScalarKind.Binary => "string",
                _ => "string",
            };

            var format = type.Format ?? type.ScalarKind switch
            {
                Canonical.ScalarKind.Date => "date",
                Canonical.ScalarKind.Time => "time",
                Canonical.ScalarKind.DateTime or Canonical.ScalarKind.DateTimeOffset => "date-time",
                Canonical.ScalarKind.Duration => "duration",
                Canonical.ScalarKind.Guid => "uuid",
                Canonical.ScalarKind.Binary => "binary",
                Canonical.ScalarKind.Boolean or Canonical.ScalarKind.String or Canonical.ScalarKind.Integer or Canonical.ScalarKind.Number or Canonical.ScalarKind.Decimal or Canonical.ScalarKind.Json or Canonical.ScalarKind.Unknown => null,
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

        private static JsonSchemaEnumNode MapEnum(Canonical.EnumTypeDefinition type)
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

        private JsonSchemaArrayNode MapArray(Canonical.ArrayTypeDefinition type)
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

        private JsonSchemaDictionaryNode MapDictionary(Canonical.DictionaryTypeDefinition type)
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

        private JsonSchemaCompositionNode MapUnion(Canonical.UnionTypeDefinition type)
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
                Kind = type.Semantics == Canonical.UnionSemantics.AnyOf ? JsonSchemaCompositionKind.AnyOf : JsonSchemaCompositionKind.OneOf,
                Alternatives = [.. type.Options.OrderBy(static option => option.Id.Value, StringComparer.Ordinal).Select(option => MapReference(option, $"/types/{type.Id.Value}/options"))],
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaSchemaRef MapReference(Canonical.TypeRef typeRef, string path)
        {
            if (_model?.TryGetType(typeRef.Id) is null)
            {
                AddDiagnostic("JSONSCHEMA_DERIVE_UNRESOLVED_ALTERNATIVE", $"Referenced type '{typeRef.Id.Value}' was not found.", path);
            }

            return JsonSchemaSchemaRef.FromReference(typeRef.Id.Value);
        }

        private JsonSchemaScalarNode UnsupportedNode(Canonical.TypeDefinition type, string code, string message)
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
            _diagnostics.Add(new Canonical.SchemaDiagnostic
            {
                Severity = Canonical.SchemaDiagnosticSeverity.Warning,
                Code = code,
                Message = message,
                Stage = Canonical.SchemaDiagnosticStage.Projection,
                ModelPath = path,
                Source = path,
                ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
            });
        }

        private static JsonSchemaConstraintSet MapConstraints(Canonical.ConstraintSet constraints)
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

        private static Dictionary<string, JsonElement> MapProjectionAnnotations(Canonical.AnnotationBag bag)
        {
            return bag.Items
                .Where(static annotation => annotation.Scope == Canonical.AnnotationScope.Projection || annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal) || annotation.Key.Value.StartsWith("schema.", StringComparison.Ordinal))
                .OrderBy(static annotation => annotation.Key.Value, StringComparer.Ordinal)
                .ToDictionary(static annotation => annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal) ? annotation.Key.Value["jsonSchema.keyword.".Length..] : annotation.Key.Value, static annotation => ToJsonElement(annotation.Value), StringComparer.Ordinal);
        }

        private static bool HasBooleanAnnotation(Canonical.AnnotationBag bag, string key)
        {
            return bag.Items.Where(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal)).Select(static annotation => annotation.Value?.ToString()).LastOrDefault(static value => !string.IsNullOrWhiteSpace(value)) is string value
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetStringAnnotation(Canonical.AnnotationBag bag, string key)
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
