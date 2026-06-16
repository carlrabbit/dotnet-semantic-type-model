using System.Text.Json;
using SemanticTypeModel.Core.Semantics;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Domain;
using Model = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.JsonSchema.Derivation;

/// <summary>JSON Schema domain derivation entry points.</summary>
public static class JsonSchemaDerivationExtensions
{
    /// <summary>Derives a JSON Schema domain semantic model from a code-first canonical semantic model.</summary>
    public static SemanticDerivationResult<JsonSchemaSemanticModel> DeriveJsonSchemaModel(
        this Model.TypeSchemaModel model,
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

    private sealed class JsonSchemaDomainMapper(Uri? schemaId, IReadOnlyList<Model.SchemaDiagnostic> initialDiagnostics, IReadOnlyDictionary<string, JsonSchemaEnvelopeProjectionPolicy> envelopePolicies)
    {
        private readonly List<Model.SchemaDiagnostic> _diagnostics = [.. initialDiagnostics];
        private Model.TypeSchemaModel? _model;

        public JsonSchemaSemanticModel Map(Model.TypeSchemaModel model)
        {
            _model = model;
            Model.TypeDefinition rootType = ResolveRoot(model);
            if (rootType is Model.ObjectTypeDefinition rootObject && TryGetEnvelopePolicy(rootObject, out JsonSchemaEnvelopeProjectionPolicy? rootPolicy))
            {
                if (rootPolicy.RootPolicy == JsonSchemaEnvelopeRootPolicy.Ambiguous)
                {
                    AddDiagnostic("JSONSCHEMA_ENVELOPE_ROOT_AMBIGUOUS", $"Envelope '{rootObject.Name}' selected both envelope and payload roots without explicit policy.", $"/types/{rootObject.Id.Value}");
                }
                else if (rootPolicy.RootPolicy == JsonSchemaEnvelopeRootPolicy.PayloadAsRoot && ResolvePayload(rootObject, rootPolicy) is Model.PropertyDefinition payload && model.TryGetType(payload.Type.Id) is Model.TypeDefinition payloadRoot)
                {
                    rootType = payloadRoot;
                }
            }
            JsonSchemaNode root = MapType(rootType);
            Dictionary<string, JsonSchemaNode> definitions = new(StringComparer.Ordinal);

            foreach (Model.TypeDefinition type in model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
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

        private static Model.TypeDefinition ResolveRoot(Model.TypeSchemaModel model)
        {
            return model.TryGetType(new Model.TypeId(model.Id.Value))
                ?? model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal).FirstOrDefault()
                ?? throw new InvalidOperationException("Cannot derive a JSON Schema model from an empty semantic model.");
        }

        private JsonSchemaNode MapType(Model.TypeDefinition type)
        {
            return type switch
            {
                Model.ObjectTypeDefinition obj => MapObject(obj),
                Model.ScalarTypeDefinition scalar => MapScalar(scalar),
                Model.EnumTypeDefinition enumType => MapEnum(enumType),
                Model.ArrayTypeDefinition array => MapArray(array),
                Model.DictionaryTypeDefinition dictionary => MapDictionary(dictionary),
                Model.UnionTypeDefinition union => MapUnion(union),
                Model.ReferenceTypeDefinition reference => new JsonSchemaCompositionNode
                {
                    Name = type.Name,
                    Title = type.DisplayName,
                    Description = type.Description,
                    Kind = JsonSchemaCompositionKind.OneOf,
                    Alternatives = [JsonSchemaSchemaRef.FromReference(reference.Target.Id.Value)],
                },
                Model.IntersectionTypeDefinition intersection => UnsupportedNode(type, "JSONSCHEMA_DERIVE_UNSUPPORTED_ALLOF", $"Intersection '{intersection.Name}' cannot be represented by baseline JSON Schema projection."),
                _ => UnsupportedNode(type, "JSONSCHEMA_DERIVE_UNSUPPORTED_TYPE", $"Type kind '{type.Kind}' is not supported by baseline JSON Schema projection."),
            };
        }

        private JsonSchemaObjectNode MapObject(Model.ObjectTypeDefinition type)
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

        private JsonSchemaProperty MapProperty(Model.ObjectTypeDefinition owner, Model.PropertyDefinition property)
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

        private JsonSchemaSchemaRef MapEnvelopePayloadSchema(Model.PropertyDefinition property, JsonSchemaEnvelopeProjectionPolicy policy)
        {
            return policy.PayloadRepresentation switch
            {
                JsonSchemaEnvelopePayloadRepresentation.Inline when _model?.TryGetType(property.Type.Id) is Model.TypeDefinition payloadType => JsonSchemaSchemaRef.FromInline(MapType(payloadType)),
                JsonSchemaEnvelopePayloadRepresentation.JsonDocument => JsonSchemaSchemaRef.FromInline(new JsonSchemaScalarNode { Type = "object" }),
                JsonSchemaEnvelopePayloadRepresentation.SerializedJsonString => JsonSchemaSchemaRef.FromInline(new JsonSchemaScalarNode { Type = "string" }),
                JsonSchemaEnvelopePayloadRepresentation.Opaque => JsonSchemaSchemaRef.FromInline(new JsonSchemaScalarNode { Type = "object" }),
                JsonSchemaEnvelopePayloadRepresentation.StructuredReference => MapReference(property.Type, $"/properties/{property.Name}"),
                _ => MapReference(property.Type, $"/properties/{property.Name}"),
            };
        }

        private bool TryGetEnvelopePolicy(Model.ObjectTypeDefinition envelope, out JsonSchemaEnvelopeProjectionPolicy policy)
        {
            return envelopePolicies.TryGetValue(envelope.Name, out policy!) || envelopePolicies.TryGetValue(envelope.Id.Value, out policy!);
        }

        private static Model.PropertyDefinition? ResolvePayload(Model.ObjectTypeDefinition envelope, JsonSchemaEnvelopeProjectionPolicy policy)
        {
            return envelope.Properties.FirstOrDefault(property => string.Equals(property.Name, policy.PayloadPropertyName, StringComparison.Ordinal))
                ?? envelope.Properties.FirstOrDefault(IsEnvelopePayload);
        }

        private static bool IsEnvelopePayload(Model.PropertyDefinition property)
        {
            return property.Annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, CoreSemanticAnnotationKeys.EnvelopePayload, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static JsonSchemaScalarNode MapScalar(Model.ScalarTypeDefinition type)
        {
            var jsonType = type.ScalarKind switch
            {
                Model.ScalarKind.Boolean => "boolean",
                Model.ScalarKind.Integer => "integer",
                Model.ScalarKind.Number => "number",
                Model.ScalarKind.Decimal => "number",
                Model.ScalarKind.Json => "object",
                Model.ScalarKind.Unknown => "object",
                Model.ScalarKind.String => "string",
                Model.ScalarKind.Date => "string",
                Model.ScalarKind.Time => "string",
                Model.ScalarKind.DateTime => "string",
                Model.ScalarKind.DateTimeOffset => "string",
                Model.ScalarKind.Duration => "string",
                Model.ScalarKind.Guid => "string",
                Model.ScalarKind.Binary => "string",
                _ => "string",
            };

            var format = type.Format ?? type.ScalarKind switch
            {
                Model.ScalarKind.Date => "date",
                Model.ScalarKind.Time => "time",
                Model.ScalarKind.DateTime or Model.ScalarKind.DateTimeOffset => "date-time",
                Model.ScalarKind.Duration => "duration",
                Model.ScalarKind.Guid => "uuid",
                Model.ScalarKind.Binary => "binary",
                Model.ScalarKind.Boolean or Model.ScalarKind.String or Model.ScalarKind.Integer or Model.ScalarKind.Number or Model.ScalarKind.Decimal or Model.ScalarKind.Json or Model.ScalarKind.Unknown => null,
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

        private static JsonSchemaEnumNode MapEnum(Model.EnumTypeDefinition type)
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

        private JsonSchemaArrayNode MapArray(Model.ArrayTypeDefinition type)
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

        private JsonSchemaDictionaryNode MapDictionary(Model.DictionaryTypeDefinition type)
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

        private JsonSchemaCompositionNode MapUnion(Model.UnionTypeDefinition type)
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
                Kind = type.Semantics == Model.UnionSemantics.AnyOf ? JsonSchemaCompositionKind.AnyOf : JsonSchemaCompositionKind.OneOf,
                Alternatives = [.. type.Options.OrderBy(static option => option.Id.Value, StringComparer.Ordinal).Select(option => MapReference(option, $"/types/{type.Id.Value}/options"))],
                Annotations = MapProjectionAnnotations(type.Annotations),
            };
        }

        private JsonSchemaSchemaRef MapReference(Model.TypeRef typeRef, string path)
        {
            if (_model?.TryGetType(typeRef.Id) is null)
            {
                AddDiagnostic("JSONSCHEMA_DERIVE_UNRESOLVED_ALTERNATIVE", $"Referenced type '{typeRef.Id.Value}' was not found.", path);
            }

            return JsonSchemaSchemaRef.FromReference(typeRef.Id.Value);
        }

        private JsonSchemaScalarNode UnsupportedNode(Model.TypeDefinition type, string code, string message)
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
            _diagnostics.Add(new Model.SchemaDiagnostic
            {
                Severity = Model.SchemaDiagnosticSeverity.Warning,
                Code = code,
                Message = message,
                Stage = Model.SchemaDiagnosticStage.Projection,
                ModelPath = path,
                Source = path,
                ProjectionTarget = Model.ProjectionTarget.JsonSchema,
            });
        }

        private static JsonSchemaConstraintSet MapConstraints(Model.ConstraintSet constraints)
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

        private static Dictionary<string, JsonElement> MapProjectionAnnotations(Model.AnnotationBag bag)
        {
            return bag.Items
                .Where(static annotation => annotation.Scope == Model.AnnotationScope.Projection || annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal) || annotation.Key.Value.StartsWith("schema.", StringComparison.Ordinal))
                .OrderBy(static annotation => annotation.Key.Value, StringComparer.Ordinal)
                .ToDictionary(static annotation => annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal) ? annotation.Key.Value["jsonSchema.keyword.".Length..] : annotation.Key.Value, static annotation => ToJsonElement(annotation.Value), StringComparer.Ordinal);
        }

        private static bool HasBooleanAnnotation(Model.AnnotationBag bag, string key)
        {
            return bag.Items.Where(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal)).Select(static annotation => annotation.Value?.ToString()).LastOrDefault(static value => !string.IsNullOrWhiteSpace(value)) is string value
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetStringAnnotation(Model.AnnotationBag bag, string key)
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
