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
                    Description = type.UserDescription,
                    Annotations = BuildTypeAnnotations(type),
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
            JsonSchemaSchemaRef? additionalPropertiesSchema = null;
            Model.PropertyDefinition? extensionData = type.Properties.FirstOrDefault(static property => HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData));
            if (extensionData is not null && _model?.TryGetType(extensionData.Type.Id) is Model.DictionaryTypeDefinition dictionary)
            {
                additionalPropertiesSchema = MapReference(dictionary.ValueType, $"/types/{type.Id.Value}/additionalProperties");
            }
            else if (extensionData is not null)
            {
                AddDiagnostic(
                    "JSONSCHEMA_EXTENSION_DATA_VALUE_UNREPRESENTABLE",
                    $"Extension-data member '{type.Name}.{extensionData.Name}' does not expose a representable dictionary value type; additional properties remain permissive.",
                    $"/types/{type.Id.Value}/properties/{extensionData.Name}");
            }
            if (GetStringAnnotation(type.Annotations, "runtime.additionalPropertiesAllowed") is { } legacyAdditional)
            {
                additionalAllowed = string.Equals(legacyAdditional, "true", StringComparison.OrdinalIgnoreCase);
            }

            List<Model.ConditionalConstraint> supported = [];
            foreach (Model.ConditionalConstraint constraint in type.Properties.SelectMany(static property => property.Constraints.Conditional))
            {
                if (constraint.Operator == Model.ConditionalConstraintOperator.Equals)
                {
                    supported.Add(constraint);
                }
                else
                {
                    AddDiagnostic("JSONSCHEMA_CONDITIONAL_OPERATOR_UNSUPPORTED", $"Conditional operator '{constraint.Operator}' on target '{constraint.TargetPropertyId.Value}' is not supported by JSON Schema projection.", $"/types/{type.Id.Value}/properties/{constraint.TargetPropertyId.Value}/constraints/conditional");
                }
            }

            return new JsonSchemaObjectNode
            {
                Name = type.Name,
                Title = type.DisplayName ?? GetStringAnnotation(type.Annotations, "schema.title") ?? GetStringAnnotation(type.Annotations, "title"),
                Description = type.UserDescription,
                AdditionalPropertiesAllowed = additionalAllowed,
                AdditionalPropertiesSchema = additionalPropertiesSchema,
                Properties = [.. type.Properties.Where(static property => !HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData)).Select(property => MapProperty(type, property)).OrderBy(static property => property.Order ?? int.MaxValue).ThenBy(static property => property.Name, StringComparer.Ordinal)],
                ConditionalConstraints = [.. supported.OrderBy(static constraint => constraint.TargetPropertyId.Value, StringComparer.Ordinal).Select(MapConditionalConstraint)],
                Annotations = BuildTypeAnnotations(type),
            };
        }

        private static JsonSchemaConditionalConstraint MapConditionalConstraint(Model.ConditionalConstraint constraint)
        {
            var value = constraint.Literal.Kind == Model.SemanticLiteralKind.EnumMember
                ? constraint.Literal.EnumMemberName
                : constraint.Literal.Value;
            return new JsonSchemaConditionalConstraint
            {
                SourceProperty = constraint.SourcePropertyName,
                TargetProperty = constraint.TargetPropertyId.Value[(constraint.TargetPropertyId.Value.LastIndexOf('.') + 1)..],
                Value = JsonSerializer.SerializeToElement(value),
            };
        }

        private JsonSchemaProperty MapProperty(Model.ObjectTypeDefinition owner, Model.PropertyDefinition property)
        {
            JsonSchemaSchemaRef schema = MapReference(property.Type, $"/properties/{property.Name}");
            Dictionary<string, JsonElement> annotations = BuildPropertyAnnotations(property);
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
                Order = GetIntAnnotation(property.Annotations, "ui.order"),
                Schema = schema,
                IsRequired = property.Cardinality.IsRequired,
                IsNullable = property.Cardinality.AllowsNull,
                Title = property.DisplayName ?? GetStringAnnotation(property.Annotations, "schema.title") ?? GetStringAnnotation(property.Annotations, "title"),
                Description = property.UserDescription,
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

        private JsonSchemaScalarNode MapScalar(Model.ScalarTypeDefinition type)
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
                Description = type.UserDescription,
                Type = jsonType,
                Format = format,
                IsNullable = type.Nullability.AllowsNull,
                Constraints = new JsonSchemaConstraintSet(),
                Annotations = BuildTypeAnnotations(type),
            };
        }

        private JsonSchemaEnumNode MapEnum(Model.EnumTypeDefinition type)
        {
            var enumValues = type.Values.Select(static value => new
            {
                Value = ToJsonElement(value.Value),
                Metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = value.Name,
                    ["displayName"] = value.DisplayName,
                    ["description"] = value.UserDescription,
                    ["technicalDescription"] = value.TechnicalDescription,
                },
            }).ToArray();
            var metadata = enumValues.Select(static item => item.Metadata.Any(pair => pair.Value is not null) ? item.Metadata.Where(static pair => pair.Value is not null).ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal) : null).ToArray();
            Dictionary<string, JsonElement> annotations = BuildTypeAnnotations(type);
            if (metadata.Any(static item => item is not null))
            {
                annotations["x-stm"] = MergeXStm(annotations, new Dictionary<string, object?> { ["enumValues"] = metadata });
            }
            return new JsonSchemaEnumNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.UserDescription,
                Values = [.. enumValues.Select(static value => value.Value)],
                Annotations = annotations,
            };
        }

        private JsonSchemaArrayNode MapArray(Model.ArrayTypeDefinition type)
        {
            return new JsonSchemaArrayNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.UserDescription,
                Items = MapReference(type.ItemType, $"/types/{type.Id.Value}/items"),
                Constraints = new JsonSchemaConstraintSet { MinItems = type.MinItems, MaxItems = type.MaxItems, UniqueItems = type.UniqueItems },
                Annotations = BuildTypeAnnotations(type),
            };
        }

        private JsonSchemaDictionaryNode MapDictionary(Model.DictionaryTypeDefinition type)
        {
            return new JsonSchemaDictionaryNode
            {
                Name = type.Name,
                Title = type.DisplayName,
                Description = type.UserDescription,
                Values = MapReference(type.ValueType, $"/types/{type.Id.Value}/values"),
                Annotations = BuildTypeAnnotations(type),
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
                Description = type.UserDescription,
                Kind = type.Semantics == Model.UnionSemantics.AnyOf ? JsonSchemaCompositionKind.AnyOf : JsonSchemaCompositionKind.OneOf,
                Alternatives = [.. type.Options.OrderBy(static option => option.Id.Value, StringComparer.Ordinal).Select(option => MapReference(option, $"/types/{type.Id.Value}/options"))],
                Annotations = BuildTypeAnnotations(type),
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
                Description = type.UserDescription,
                Type = "object",
                Annotations = BuildTypeAnnotations(type),
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

        private Dictionary<string, JsonElement> BuildTypeAnnotations(Model.TypeDefinition type)
        {
            Dictionary<string, JsonElement> annotations = MapProjectionAnnotations(type.Annotations);
            var stm = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            if (type is Model.ObjectTypeDefinition obj)
            {
                if (obj.Semantics.Role != Model.EntityRole.Unspecified)
                {
                    stm["role"] = obj.Semantics.Role.ToString().ToLowerInvariant();
                }

                if (obj.Semantics.IsAggregateRoot)
                {
                    stm["aggregateRoot"] = true;
                }

                if (obj.Mutability is { } mutability)
                {
                    stm["mutability"] = mutability.ToString().ToLowerInvariant();
                }

                if (obj.Keys.Count > 0)
                {
                    stm["keys"] = obj.Keys.Select(key => new SortedDictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["name"] = key.Name,
                        ["kind"] = key.Kind.ToString().ToLowerInvariant(),
                        ["properties"] = key.Properties.Select(reference => obj.Properties.FirstOrDefault(property => property.Id == reference.Id)?.Name ?? reference.Id.Value).ToArray(),
                        ["generated"] = key.IsGenerated ? true : null,
                    }.Where(static pair => pair.Value is not null).ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)).ToArray();
                }

                string[] displayIdentity = [.. obj.Properties
                    .Where(static property => GetIntAnnotation(property.Annotations, CoreSemanticAnnotationKeys.DisplayIdentity) is not null)
                    .OrderBy(property => GetIntAnnotation(property.Annotations, CoreSemanticAnnotationKeys.DisplayIdentity))
                    .ThenBy(static property => property.Name, StringComparer.Ordinal)
                    .Select(static property => property.Name)];
                if (displayIdentity.Length > 0)
                {
                    stm["displayIdentity"] = displayIdentity;
                }

                var accessPaths = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                foreach (Model.PropertyDefinition property in obj.Properties)
                {
                    foreach (Model.Annotation annotation in property.Annotations.Items.Where(static annotation => annotation.Key.Value.StartsWith(CoreSemanticAnnotationKeys.AccessPathPrefix, StringComparison.Ordinal)))
                    {
                        string name = annotation.Key.Value[CoreSemanticAnnotationKeys.AccessPathPrefix.Length..];
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }
                        if (!accessPaths.TryGetValue(name, out object? existing))
                        {
                            existing = new List<(int Order, string Name)>();
                            accessPaths[name] = existing;
                        }
                        if (existing is List<(int Order, string Name)> members && int.TryParse(annotation.Value?.ToString(), out int order))
                        {
                            members.Add((order, property.Name));
                        }
                    }
                }
                if (accessPaths.Count > 0)
                {
                    stm["accessPaths"] = accessPaths.ToDictionary(static pair => pair.Key, static pair => ((List<(int Order, string Name)>)pair.Value!).OrderBy(static item => item.Order).ThenBy(static item => item.Name, StringComparer.Ordinal).Select(static item => item.Name).ToArray(), StringComparer.Ordinal);
                }

                if (HasBooleanAnnotation(type.Annotations, CoreSemanticAnnotationKeys.Envelope))
                {
                    var envelope = new SortedDictionary<string, object?>(StringComparer.Ordinal);
                    string? purpose = GetStringAnnotation(type.Annotations, CoreSemanticAnnotationKeys.EnvelopePurpose);
                    if (!string.IsNullOrWhiteSpace(purpose))
                    {
                        envelope["purpose"] = purpose;
                    }

                    string? payload = obj.Properties.FirstOrDefault(static property => HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.EnvelopePayload))?.Name;
                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        envelope["payload"] = payload;
                    }

                    string[] metadata = [.. obj.Properties.Where(static property => HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.EnvelopeMetadata)).Select(static property => property.Name).Order(StringComparer.Ordinal)];
                    if (metadata.Length > 0)
                    {
                        envelope["metadata"] = metadata;
                    }

                    stm["envelope"] = envelope;
                }
                AddBooleanSemantic(stm, type.Annotations, CoreSemanticAnnotationKeys.Versioned, "versioned");
                AddBooleanSemantic(stm, type.Annotations, CoreSemanticAnnotationKeys.TemporalValidity, "temporalValidity");
                if (obj.Properties.Any(static property => HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData)))
                {
                    stm["extensionData"] = true;
                }
            }
            if (type is Model.ScalarTypeDefinition { Unit: { Length: > 0 } unit })
            {
                stm["unit"] = unit;
            }

            AddSharedSemantics(stm, type.TechnicalDescription, type.Annotations, $"/types/{type.Id.Value}");
            if (stm.Count > 0)
            {
                annotations["x-stm"] = JsonSerializer.SerializeToElement(stm);
            }

            return annotations;
        }

        private Dictionary<string, JsonElement> BuildPropertyAnnotations(Model.PropertyDefinition property)
        {
            Dictionary<string, JsonElement> annotations = MapProjectionAnnotations(property.Annotations);
            var stm = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            if (property.Mutability is { } mutability)
            {
                stm["mutability"] = mutability.ToString().ToLowerInvariant();
            }

            AddBooleanSemantic(stm, property.Annotations, CoreSemanticAnnotationKeys.Version, "version");
            AddBooleanSemantic(stm, property.Annotations, CoreSemanticAnnotationKeys.Revision, "revision");
            AddBooleanSemantic(stm, property.Annotations, CoreSemanticAnnotationKeys.CurrentVersion, "currentVersion");
            AddBooleanSemantic(stm, property.Annotations, CoreSemanticAnnotationKeys.ValidFrom, "validFrom");
            AddBooleanSemantic(stm, property.Annotations, CoreSemanticAnnotationKeys.ValidTo, "validTo");
            AddBooleanSemantic(stm, property.Annotations, CoreSemanticAnnotationKeys.LifecycleState, "lifecycleState");
            string? ownership = GetStringAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnershipKind)
                ?? (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedCollection) ? "collection" : HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedObject) ? "object" : null);
            if (ownership is not null)
            {
                stm["ownership"] = ownership.ToLowerInvariant();
            }

            AddSharedSemantics(stm, property.TechnicalDescription, property.Annotations, $"/properties/{property.Name}");
            if (stm.Count > 0)
            {
                annotations["x-stm"] = JsonSerializer.SerializeToElement(stm);
            }

            return annotations;
        }

        private void AddSharedSemantics(SortedDictionary<string, object?> stm, string? technicalDescription, Model.AnnotationBag bag, string path)
        {
            if (!string.IsNullOrWhiteSpace(technicalDescription))
            {
                stm["technicalDescription"] = technicalDescription;
            }

            var ui = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (Model.Annotation annotation in bag.Items.Where(static annotation => annotation.Key.Value.StartsWith("ui.", StringComparison.Ordinal)).OrderBy(static annotation => annotation.Key.Value, StringComparer.Ordinal))
            {
                try { ui[annotation.Key.Value[3..]] = ToJsonElement(annotation.Value); }
                catch (Exception exception) when (exception is NotSupportedException or JsonException) { AddDiagnostic("JSONSCHEMA_UI_VALUE_NOT_JSON_COMPATIBLE", $"UI annotation '{annotation.Key.Value}' is not JSON-compatible.", path); }
            }
            if (ui.Count > 0)
            {
                stm["ui"] = ui;
            }
        }

        private static Dictionary<string, JsonElement> MapProjectionAnnotations(Model.AnnotationBag bag)
        {
            return bag.Items
                .Where(static annotation => annotation.Key.Value.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal))
                .OrderBy(static annotation => annotation.Key.Value, StringComparer.Ordinal)
                .ToDictionary(static annotation => annotation.Key.Value["jsonSchema.keyword.".Length..], static annotation => ToJsonElement(annotation.Value), StringComparer.Ordinal);
        }

        private static bool HasBooleanAnnotation(Model.AnnotationBag bag, string key)
        {
            return bag.Items.Where(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal)).Select(static annotation => annotation.Value?.ToString()).LastOrDefault(static value => !string.IsNullOrWhiteSpace(value)) is string value
                && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static int? GetIntAnnotation(Model.AnnotationBag bag, string key)
        {
            string? value = GetStringAnnotation(bag, key);
            return int.TryParse(value, out int result) ? result : null;
        }

        private static void AddBooleanSemantic(SortedDictionary<string, object?> target, Model.AnnotationBag bag, string key, string outputKey)
        {
            if (HasBooleanAnnotation(bag, key))
            {
                target[outputKey] = true;
            }
        }

        private static JsonElement MergeXStm(Dictionary<string, JsonElement> annotations, Dictionary<string, object?> additions)
        {
            var merged = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
            if (annotations.TryGetValue("x-stm", out JsonElement existing) && existing.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in existing.EnumerateObject())
                {
                    merged[property.Name] = property.Value.Clone();
                }
            }
            foreach ((string key, object? value) in additions)
            {
                merged[key] = ToJsonElement(value);
            }
            return JsonSerializer.SerializeToElement(merged);
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
