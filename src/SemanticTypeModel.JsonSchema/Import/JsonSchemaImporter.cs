using System.Text.Json;
using SemanticTypeModel.Abstractions.Contracts;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;
using ProjectionTarget = SemanticTypeModel.Abstractions.Hardening.ProjectionTarget;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnostic;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticSeverity;
using SchemaDiagnosticStage = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticStage;

namespace SemanticTypeModel.JsonSchema.Import;

/// <summary>
/// Imports a JSON Schema Draft 2020-12 document into the canonical semantic type model.
/// </summary>
public sealed class JsonSchemaImporter : ISchemaModelSource
{
    private static readonly HashSet<string> SupportedKeywords =
    [
        "$schema", "$id", "$defs", "$ref",
        "type", "properties", "required", "additionalProperties",
        "items", "enum", "const", "oneOf", "anyOf", "allOf",
        "title", "description", "default",
        "format", "pattern",
        "minLength", "maxLength",
        "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum", "multipleOf",
        "minItems", "maxItems", "uniqueItems",
        "minProperties", "maxProperties",
    ];

    private readonly string _json;

    /// <summary>
    /// Initializes a new importer for the given JSON Schema string.
    /// </summary>
    /// <param name="json">The JSON Schema Draft 2020-12 document to import.</param>
    public JsonSchemaImporter(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        _json = json;
    }

    /// <summary>
    /// Parses the JSON Schema document and returns the canonical <see cref="TypeSchemaModel"/>.
    /// </summary>
    public TypeSchemaModel Load()
    {
        return Import(_json).Model;
    }

    /// <summary>
    /// Imports a JSON Schema document from a JSON string.
    /// </summary>
    public static JsonSchemaImportResult Import(string json, JsonSchemaImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        using var document = JsonDocument.Parse(json);
        return Import(document.RootElement, options);
    }

    /// <summary>
    /// Imports a JSON Schema document from a stream.
    /// </summary>
    public static JsonSchemaImportResult Import(Stream stream, JsonSchemaImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var document = JsonDocument.Parse(stream);
        return Import(document.RootElement, options);
    }

    /// <summary>
    /// Imports a JSON Schema document from a JSON document.
    /// </summary>
    public static JsonSchemaImportResult Import(JsonDocument document, JsonSchemaImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Import(document.RootElement, options);
    }

    /// <summary>
    /// Imports a JSON Schema document from a JSON element.
    /// </summary>
    public static JsonSchemaImportResult Import(JsonElement root, JsonSchemaImportOptions? options = null)
    {
        options ??= JsonSchemaImportOptions.Default;

        var rootId = root.TryGetProperty("$id", out JsonElement idProp)
            ? idProp.GetString() ?? "#"
            : options.BaseUri?.ToString() ?? "#";

        var diagnostics = new List<SchemaDiagnostic>();
        var builder = new TypeSchemaModelBuilder();
        var context = new ImportContext(builder, options, diagnostics, rootId);

        context.ValidateDialect(root, "/");

        if (root.TryGetProperty("$defs", out JsonElement defs) && defs.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty def in defs.EnumerateObject())
            {
                context.RegisterDef(def.Name, def.Value);
            }
        }

        context.ResolveAllDefs();

        TypeShape rootShape = context.ParseShape(root, rootId, "/");
        _ = builder.AddShape(rootId, rootShape);
        _ = builder.SetRoot(rootId);

        TypeSchemaModel model;
        try
        {
            model = builder.Build();
        }
        catch (InvalidOperationException ex)
        {
            diagnostics.Add(Diagnostic(
                SchemaDiagnosticSeverity.Error,
                "JSONSCHEMA_IMPORT_BUILD_FAILED",
                ex.Message,
                "/",
                ProjectionTarget.JsonSchema));
            model = new TypeSchemaModel(new Dictionary<string, TypeShape>(StringComparer.Ordinal), null);
        }

        return new JsonSchemaImportResult(model, diagnostics);
    }

    private static SchemaDiagnostic Diagnostic(
        SchemaDiagnosticSeverity severity,
        string code,
        string message,
        string modelPath,
        ProjectionTarget target)
    {
        return new SchemaDiagnostic
        {
            Severity = severity,
            Code = code,
            Message = message,
            Stage = SchemaDiagnosticStage.Import,
            ModelPath = modelPath,
            ProjectionTarget = target,
            Source = modelPath,
        };
    }

    private sealed class ImportContext(
        TypeSchemaModelBuilder builder,
        JsonSchemaImportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string rootIdentifier)
    {
        private readonly Dictionary<string, JsonElement> _defElements = new(StringComparer.Ordinal);
        private readonly HashSet<string> _resolved = new(StringComparer.Ordinal);
        private readonly HashSet<string> _syntheticRefs = new(StringComparer.Ordinal);

        public void RegisterDef(string name, JsonElement element)
        {
            _defElements[name] = element;
        }

        public void ResolveAllDefs()
        {
            foreach ((var name, JsonElement element) in _defElements)
            {
                if (!_resolved.Contains(name))
                {
                    ResolveDef(name, element);
                }
            }
        }

        public void ValidateDialect(JsonElement root, string pointer)
        {
            if (!root.TryGetProperty("$schema", out JsonElement schemaElement))
            {
                return;
            }

            var schema = schemaElement.GetString();
            if (!string.Equals(schema, JsonSchemaDialectUris.Draft202012, StringComparison.Ordinal))
            {
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Error,
                    "JSONSCHEMA_UNSUPPORTED_DIALECT",
                    $"Only JSON Schema Draft 2020-12 is supported. Found '{schema ?? "<null>"}'.",
                    pointer,
                    ProjectionTarget.JsonSchema));
            }
        }

        public TypeShape ParseShape(JsonElement element, string? identifier, string pointer)
        {
            if (element.ValueKind == JsonValueKind.True)
            {
                return new ScalarShape { Identifier = identifier, Kind = ScalarKind.String };
            }

            if (element.ValueKind == JsonValueKind.False)
            {
                return new ScalarShape { Identifier = identifier, Kind = ScalarKind.Null, IsNullable = true };
            }

            List<SchemaAnnotation> annotations = ParseAnnotations(element);
            CaptureUnsupportedKeywords(element, pointer, annotations);
            ConstraintSet constraints = ParseConstraints(element, annotations);

            if (element.TryGetProperty("allOf", out JsonElement allOf))
            {
                annotations.Add(new SchemaAnnotation("jsonSchema.allOf", allOf.GetRawText()));
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Warning,
                    "JSONSCHEMA_UNSUPPORTED_ALLOF",
                    "Keyword 'allOf' is preserved as annotation because intersection projection is deferred.",
                    pointer,
                    ProjectionTarget.JsonSchema));
            }

            if (element.TryGetProperty("$ref", out JsonElement refProp))
            {
                return new UnionShape
                {
                    Identifier = identifier,
                    Options = [BuildShapeRef(refProp.GetString() ?? string.Empty, pointer)],
                    Annotations = annotations,
                    Constraints = constraints,
                };
            }

            if (element.TryGetProperty("oneOf", out JsonElement oneOfProp))
            {
                return ParseUnion(oneOfProp, "oneOf", identifier, annotations, constraints, pointer);
            }

            if (element.TryGetProperty("anyOf", out JsonElement anyOfProp))
            {
                return ParseUnion(anyOfProp, "anyOf", identifier, annotations, constraints, pointer);
            }

            if (element.TryGetProperty("enum", out JsonElement enumProp) && enumProp.ValueKind == JsonValueKind.Array)
            {
                var values = enumProp.EnumerateArray().Select(static value => value.GetRawText()).ToList();
                return new EnumShape
                {
                    Identifier = identifier,
                    Values = values,
                    Annotations = annotations,
                    Constraints = constraints,
                };
            }

            if (element.TryGetProperty("type", out JsonElement typeProp))
            {
                if (typeProp.ValueKind == JsonValueKind.Array)
                {
                    var types = typeProp.EnumerateArray()
                        .Select(static typeElement => typeElement.GetString())
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .ToList();

                    var nonNullTypes = types.Where(static typeName => !string.Equals(typeName, "null", StringComparison.Ordinal)).ToList();
                    var hasNull = types.Count != nonNullTypes.Count;

                    if (nonNullTypes.Count > 1)
                    {
                        return new UnionShape
                        {
                            Identifier = identifier,
                            Options = BuildUnionOptions(nonNullTypes, hasNull),
                            Annotations = [.. annotations, new SchemaAnnotation("jsonSchema.unionSemantics", "anyOf")],
                            Constraints = constraints,
                        };
                    }
                }
            }

            return TryGetType(element, out var typeName, out var isNullable)
                ? typeName switch
                {
                    "object" => ParseObject(element, identifier, annotations, constraints, pointer),
                    "array" => ParseArray(element, identifier, annotations, constraints, pointer),
                    "string" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.String, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "integer" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Integer, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "number" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Number, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "boolean" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Boolean, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "null" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Null, IsNullable = true, Annotations = annotations, Constraints = constraints },
                    _ => ParseAmbiguousType(identifier, annotations, constraints, pointer, typeName),
                }
                : new ObjectShape { Identifier = identifier, Annotations = annotations, Constraints = constraints };
        }

        private ObjectShape ParseAmbiguousType(
            string? identifier,
            IReadOnlyList<SchemaAnnotation> annotations,
            ConstraintSet constraints,
            string pointer,
            string? typeName)
        {
            diagnostics.Add(Diagnostic(
                SchemaDiagnosticSeverity.Warning,
                "JSONSCHEMA_INVALID_OR_AMBIGUOUS_TYPE",
                $"Type value '{typeName ?? "<null>"}' at '{pointer}' is invalid or not explicitly supported and was normalized to object.",
                pointer,
                ProjectionTarget.JsonSchema));

            return new ObjectShape
            {
                Identifier = identifier,
                Annotations = annotations,
                Constraints = constraints,
            };
        }

        private static List<ShapeRef> BuildUnionOptions(List<string?> nonNullTypes, bool hasNull)
        {
            var options = new List<ShapeRef>(nonNullTypes.Count + (hasNull ? 1 : 0));
            foreach (var typeName in nonNullTypes)
            {
                options.Add(ShapeRef.FromInline(CreateShapeForType(typeName)));
            }

            if (hasNull)
            {
                options.Add(ShapeRef.FromInline(new ScalarShape { Kind = ScalarKind.Null, IsNullable = true }));
            }

            return options;
        }

        private static TypeShape CreateShapeForType(string? typeName)
        {
            return typeName switch
            {
                "string" => new ScalarShape { Kind = ScalarKind.String },
                "integer" => new ScalarShape { Kind = ScalarKind.Integer },
                "number" => new ScalarShape { Kind = ScalarKind.Number },
                "boolean" => new ScalarShape { Kind = ScalarKind.Boolean },
                "null" => new ScalarShape { Kind = ScalarKind.Null, IsNullable = true },
                "array" => new ArrayShape(),
                "object" => new ObjectShape(),
                _ => new ObjectShape(),
            };
        }

        private TypeShape ParseObject(
            JsonElement element,
            string? identifier,
            IReadOnlyList<SchemaAnnotation> annotations,
            ConstraintSet constraints,
            string pointer)
        {
            var requiredSet = new HashSet<string>(StringComparer.Ordinal);
            if (element.TryGetProperty("required", out JsonElement requiredProp) && requiredProp.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement required in requiredProp.EnumerateArray())
                {
                    if (required.GetString() is { } name)
                    {
                        _ = requiredSet.Add(name);
                    }
                }
            }

            if (!element.TryGetProperty("properties", out _) &&
                element.TryGetProperty("additionalProperties", out JsonElement additionalPropertiesSchema) &&
                additionalPropertiesSchema.ValueKind is JsonValueKind.Object or JsonValueKind.True or JsonValueKind.False)
            {
                ShapeRef values = BuildShapeRef(additionalPropertiesSchema, null, pointer + "/additionalProperties");
                return new DictionaryShape
                {
                    Identifier = identifier,
                    Values = values,
                    Annotations = annotations,
                    Constraints = constraints,
                };
            }

            var properties = new List<PropertyShape>();
            var additionalAllowed = true;

            if (element.TryGetProperty("properties", out JsonElement propertiesProp) && propertiesProp.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in propertiesProp.EnumerateObject())
                {
                    List<SchemaAnnotation> propertyAnnotations = ParseAnnotations(property.Value);
                    CaptureUnsupportedKeywords(property.Value, $"{pointer}/properties/{EscapePointer(property.Name)}", propertyAnnotations);
                    var propertyNullable = IsNullable(property.Value);
                    ShapeRef propertyType = BuildShapeRef(property.Value, null, $"{pointer}/properties/{EscapePointer(property.Name)}");

                    properties.Add(new PropertyShape
                    {
                        Name = property.Name,
                        IsRequired = requiredSet.Contains(property.Name),
                        IsNullable = propertyNullable,
                        Type = propertyType,
                        Annotations = propertyAnnotations,
                    });
                }
            }

            if (element.TryGetProperty("additionalProperties", out JsonElement additionalPropValue))
            {
                if (additionalPropValue.ValueKind == JsonValueKind.False)
                {
                    additionalAllowed = false;
                }
                else if (additionalPropValue.ValueKind == JsonValueKind.Object && properties.Count > 0)
                {
                    diagnostics.Add(Diagnostic(
                        SchemaDiagnosticSeverity.Info,
                        "JSONSCHEMA_ADDITIONAL_PROPERTIES_SCHEMA_PRESERVED",
                        "Schema-valued 'additionalProperties' on object-with-properties is preserved as annotation.",
                        pointer + "/additionalProperties",
                        ProjectionTarget.JsonSchema));
                }
            }

            return new ObjectShape
            {
                Identifier = identifier,
                Properties = properties,
                AdditionalPropertiesAllowed = additionalAllowed,
                Annotations = annotations,
                Constraints = constraints,
            };
        }

        private ArrayShape ParseArray(
            JsonElement element,
            string? identifier,
            IReadOnlyList<SchemaAnnotation> annotations,
            ConstraintSet constraints,
            string pointer)
        {
            ShapeRef? items = null;
            if (element.TryGetProperty("items", out JsonElement itemsProp))
            {
                items = BuildShapeRef(itemsProp, null, pointer + "/items");
            }

            if (element.TryGetProperty("prefixItems", out _))
            {
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Warning,
                    "JSONSCHEMA_UNSUPPORTED_PREFIX_ITEMS",
                    "Keyword 'prefixItems' is not modeled in baseline runtime import and was preserved as annotation when configured.",
                    pointer + "/prefixItems",
                    ProjectionTarget.JsonSchema));
            }

            return new ArrayShape
            {
                Identifier = identifier,
                Items = items,
                Annotations = annotations,
                Constraints = constraints,
            };
        }

        private TypeShape ParseUnion(
            JsonElement unionProp,
            string unionKeyword,
            string? identifier,
            IReadOnlyList<SchemaAnnotation> annotations,
            ConstraintSet constraints,
            string pointer)
        {
            if (unionProp.ValueKind != JsonValueKind.Array)
            {
                return new ObjectShape
                {
                    Identifier = identifier,
                    Annotations = annotations,
                    Constraints = constraints,
                };
            }

            var options = unionProp.EnumerateArray().ToList();
            if (TryGetNullableUnionTarget(options, out JsonElement nonNullOption))
            {
                TypeShape innerShape = ParseShape(nonNullOption, identifier, pointer);
                return MakeNullable(innerShape, annotations, constraints);
            }

            var shapeRefs = options
                .Select((option, index) => BuildShapeRef(option, null, $"{pointer}/{unionKeyword}/{index}"))
                .ToList();

            return new UnionShape
            {
                Identifier = identifier,
                Options = shapeRefs,
                Annotations = [.. annotations, new SchemaAnnotation("jsonSchema.unionSemantics", unionKeyword)],
                Constraints = constraints,
            };
        }

        private static TypeShape MakeNullable(TypeShape shape, IReadOnlyList<SchemaAnnotation> annotations, ConstraintSet constraints)
        {
            return shape switch
            {
                ScalarShape scalar => scalar with { IsNullable = true, Annotations = annotations, Constraints = constraints },
                ObjectShape obj => obj with { Annotations = annotations, Constraints = constraints },
                EnumShape enumShape => enumShape with { Annotations = annotations, Constraints = constraints },
                ArrayShape array => array with { Annotations = annotations, Constraints = constraints },
                DictionaryShape dictionary => dictionary with { Annotations = annotations, Constraints = constraints },
                UnionShape union => union with { Annotations = annotations, Constraints = constraints },
                _ => shape,
            };
        }

        private ShapeRef BuildShapeRef(JsonElement element, string? inlineIdentifier, string pointer)
        {
            if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return ShapeRef.FromInline(ParseShape(element, inlineIdentifier, pointer));
            }

            if (element.TryGetProperty("$ref", out JsonElement refProp))
            {
                return BuildShapeRef(refProp.GetString() ?? string.Empty, pointer);
            }

            if (element.TryGetProperty("oneOf", out JsonElement oneOfProp))
            {
                var options = oneOfProp.EnumerateArray().ToList();
                if (TryGetNullableUnionTarget(options, out JsonElement nonNullOption))
                {
                    if (nonNullOption.TryGetProperty("$ref", out JsonElement innerRef))
                    {
                        return BuildShapeRef(innerRef.GetString() ?? string.Empty, pointer);
                    }

                    TypeShape nullableInline = MakeNullable(
                        ParseShape(nonNullOption, inlineIdentifier, pointer),
                        ParseAnnotations(element),
                        ParseConstraints(element, []));
                    return ShapeRef.FromInline(nullableInline);
                }
            }

            return ShapeRef.FromInline(ParseShape(element, inlineIdentifier, pointer));
        }

        private ShapeRef BuildShapeRef(string refValue, string pointer)
        {
            if (IsRemoteRef(refValue))
            {
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Warning,
                    "JSONSCHEMA_REMOTE_REF_UNSUPPORTED",
                    $"Remote reference '{refValue}' is out of scope for runtime baseline import.",
                    pointer,
                    ProjectionTarget.JsonSchema));

                var syntheticId = $"__remoteRef:{refValue}";
                EnsureSyntheticPlaceholder(syntheticId, "jsonSchema.remoteRef");
                return ShapeRef.FromIdentifier(syntheticId);
            }

            var resolvedId = ResolveRef(refValue);
            if (resolvedId == "#")
            {
                resolvedId = rootIdentifier;
            }

            if (_defElements.TryGetValue(resolvedId, out JsonElement defElement) && !_resolved.Contains(resolvedId))
            {
                ResolveDef(resolvedId, defElement);
            }

            if (resolvedId != rootIdentifier &&
                !_defElements.ContainsKey(resolvedId) &&
                !_resolved.Contains(resolvedId) &&
                !_syntheticRefs.Contains(resolvedId))
            {
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Error,
                    "JSONSCHEMA_UNRESOLVED_LOCAL_REF",
                    $"Local reference '{refValue}' cannot be resolved.",
                    pointer,
                    ProjectionTarget.JsonSchema));

                EnsureSyntheticPlaceholder(resolvedId, "jsonSchema.unresolvedRef");
            }

            return ShapeRef.FromIdentifier(resolvedId);
        }

        private void ResolveDef(string name, JsonElement element)
        {
            if (!_resolved.Add(name))
            {
                return;
            }

            TypeShape shape = ParseShape(element, name, $"/$defs/{EscapePointer(name)}");
            _ = builder.AddShape(name, shape);
        }

        private void EnsureSyntheticPlaceholder(string identifier, string annotationKey)
        {
            if (!_syntheticRefs.Add(identifier))
            {
                return;
            }

            _ = builder.AddShape(
                identifier,
                new ObjectShape
                {
                    Identifier = identifier,
                    Annotations = [new SchemaAnnotation(annotationKey, identifier)],
                });
        }

        private static bool IsRemoteRef(string refValue)
        {
            return !string.IsNullOrWhiteSpace(refValue) &&
                !refValue.StartsWith('#');
        }

        private static bool TryGetNullableUnionTarget(List<JsonElement> options, out JsonElement nonNullOption)
        {
            nonNullOption = default;
            if (options.Count != 2)
            {
                return false;
            }

            var firstIsNull = IsNullSchema(options[0]);
            var secondIsNull = IsNullSchema(options[1]);

            if (firstIsNull == secondIsNull)
            {
                return false;
            }

            nonNullOption = firstIsNull ? options[1] : options[0];
            return true;
        }

        private static bool IsNullSchema(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.False)
            {
                return true;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (element.TryGetProperty("type", out JsonElement typeProp))
            {
                if (typeProp.ValueKind == JsonValueKind.String)
                {
                    return string.Equals(typeProp.GetString(), "null", StringComparison.Ordinal);
                }

                if (typeProp.ValueKind == JsonValueKind.Array)
                {
                    return typeProp.EnumerateArray().Any(static item => string.Equals(item.GetString(), "null", StringComparison.Ordinal));
                }
            }

            return false;
        }

        private static bool IsNullable(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.False ||
            (TryGetType(element, out _, out var isNullable)
                ? isNullable
                : element.TryGetProperty("oneOf", out JsonElement oneOf) && oneOf.EnumerateArray().Any(IsNullSchema));
        }

        private static bool TryGetType(JsonElement element, out string? typeName, out bool isNullable)
        {
            typeName = null;
            isNullable = false;

            if (!element.TryGetProperty("type", out JsonElement typeProp))
            {
                return false;
            }

            if (typeProp.ValueKind == JsonValueKind.String)
            {
                typeName = typeProp.GetString();
                isNullable = string.Equals(typeName, "null", StringComparison.Ordinal);
                return true;
            }

            if (typeProp.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement typeElement in typeProp.EnumerateArray())
            {
                var value = typeElement.GetString();
                if (string.Equals(value, "null", StringComparison.Ordinal))
                {
                    isNullable = true;
                    continue;
                }

                typeName ??= value;
            }

            return typeName is not null || isNullable;
        }

        private static string ResolveRef(string refValue)
        {
            return refValue.StartsWith("#/$defs/", StringComparison.Ordinal)
                ? refValue["#/$defs/".Length..]
                : refValue.StartsWith("#/definitions/", StringComparison.Ordinal)
                    ? refValue["#/definitions/".Length..]
                    : refValue == "#" ? "#" : refValue;
        }

        private static List<SchemaAnnotation> ParseAnnotations(JsonElement element)
        {
            var annotations = new List<SchemaAnnotation>();

            if (element.ValueKind != JsonValueKind.Object)
            {
                return annotations;
            }

            if (element.TryGetProperty("title", out JsonElement title) && title.GetString() is { } titleValue)
            {
                annotations.Add(new SchemaAnnotation("schema.title", titleValue));
            }

            if (element.TryGetProperty("description", out JsonElement description) && description.GetString() is { } descriptionValue)
            {
                annotations.Add(new SchemaAnnotation("schema.description", descriptionValue));
            }

            if (element.TryGetProperty("default", out JsonElement defaultValue))
            {
                annotations.Add(new SchemaAnnotation("schema.default", defaultValue.GetRawText()));
            }

            return annotations;
        }

        private ConstraintSet ParseConstraints(JsonElement element, IReadOnlyList<SchemaAnnotation> annotations)
        {
            var entries = new List<ConstraintEntry>();

            TryAddConstraint(element, "minLength", entries);
            TryAddConstraint(element, "maxLength", entries);
            TryAddConstraint(element, "pattern", entries);
            TryAddConstraint(element, "format", entries);
            TryAddConstraint(element, "minimum", entries);
            TryAddConstraint(element, "maximum", entries);
            TryAddConstraint(element, "exclusiveMinimum", entries);
            TryAddConstraint(element, "exclusiveMaximum", entries);
            TryAddConstraint(element, "multipleOf", entries);
            TryAddConstraint(element, "minItems", entries);
            TryAddConstraint(element, "maxItems", entries);
            TryAddConstraint(element, "uniqueItems", entries);
            TryAddConstraint(element, "minProperties", entries);
            TryAddConstraint(element, "maxProperties", entries);

            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("const", out JsonElement constValue))
            {
                entries.Add(new ConstraintEntry("const", constValue.GetRawText()));
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Info,
                    "JSONSCHEMA_CONST_MODELED_AS_CONSTRAINT",
                    "Keyword 'const' was modeled as a canonical constraint entry.",
                    "/",
                    ProjectionTarget.JsonSchema));
            }

            if (element.TryGetProperty("type", out JsonElement typeElement) &&
                typeElement.ValueKind == JsonValueKind.Array &&
                typeElement.EnumerateArray().Count(static t => !string.Equals(t.GetString(), "null", StringComparison.Ordinal)) > 1)
            {
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Info,
                    "JSONSCHEMA_TYPE_ARRAY_MAPPED_TO_UNION",
                    "Multi-type array in 'type' was normalized to union semantics.",
                    "/",
                    ProjectionTarget.JsonSchema));
            }

            if (element.TryGetProperty("required", out JsonElement requiredElement) &&
                requiredElement.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Add(Diagnostic(
                    SchemaDiagnosticSeverity.Warning,
                    "JSONSCHEMA_INVALID_REQUIRED",
                    "Keyword 'required' must be an array.",
                    "/required",
                    ProjectionTarget.JsonSchema));
            }

            if (annotations.Any(static a => a.Key == "schema.default"))
            {
                entries.Add(new ConstraintEntry("hasDefault", "true"));
            }

            return entries.Count == 0 ? ConstraintSet.Empty : new ConstraintSet { Entries = entries };
        }

        private void CaptureUnsupportedKeywords(JsonElement element, string pointer, List<SchemaAnnotation> annotations)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (SupportedKeywords.Contains(property.Name))
                {
                    continue;
                }

                if (TryMapUiEditorKeyword(property.Name, out var mappedAnnotationKey))
                {
                    annotations.Add(new SchemaAnnotation(mappedAnnotationKey, property.Value.GetRawText()));
                    diagnostics.Add(Diagnostic(
                        SchemaDiagnosticSeverity.Info,
                        "JSONSCHEMA_UI_HINT_IMPORTED",
                        $"UI/editor keyword '{property.Name}' was mapped to annotation '{mappedAnnotationKey}'.",
                        $"{pointer}/{EscapePointer(property.Name)}",
                        ProjectionTarget.JsonSchema));
                    continue;
                }

                var annotationKey = ToUnsupportedAnnotationKey(property.Name);
                switch (options.UnsupportedKeywordBehavior)
                {
                    case UnsupportedKeywordBehavior.PreserveAsAnnotation:
                        if (options.PreserveUnsupportedKeywordsAsAnnotations)
                        {
                            annotations.Add(new SchemaAnnotation(annotationKey, property.Value.GetRawText()));
                        }

                        diagnostics.Add(Diagnostic(
                            SchemaDiagnosticSeverity.Info,
                            "JSONSCHEMA_UNSUPPORTED_KEYWORD_PRESERVED",
                            $"Unsupported keyword '{property.Name}' was preserved as annotation '{annotationKey}'.",
                            $"{pointer}/{EscapePointer(property.Name)}",
                            ProjectionTarget.JsonSchema));
                        break;
                    case UnsupportedKeywordBehavior.IgnoreWithWarning:
                        diagnostics.Add(Diagnostic(
                            SchemaDiagnosticSeverity.Warning,
                            "JSONSCHEMA_UNSUPPORTED_KEYWORD_IGNORED",
                            $"Unsupported keyword '{property.Name}' was ignored.",
                            $"{pointer}/{EscapePointer(property.Name)}",
                            ProjectionTarget.JsonSchema));
                        break;
                    case UnsupportedKeywordBehavior.RejectWithError:
                        diagnostics.Add(Diagnostic(
                            SchemaDiagnosticSeverity.Error,
                            "JSONSCHEMA_UNSUPPORTED_KEYWORD_REJECTED",
                            $"Unsupported keyword '{property.Name}' is not permitted by import options.",
                            $"{pointer}/{EscapePointer(property.Name)}",
                            ProjectionTarget.JsonSchema));
                        break;
                    default:
                        break;
                }
            }
        }

        private static string ToUnsupportedAnnotationKey(string keyword)
        {
            return keyword.StartsWith("ui:", StringComparison.Ordinal)
                ? $"ui.{keyword["ui:".Length..]}"
                : $"jsonSchema.keyword.{keyword.Replace(':', '.')}";
        }

        private static bool TryMapUiEditorKeyword(string keyword, out string annotationKey)
        {
            if (keyword.StartsWith("ui:", StringComparison.Ordinal))
            {
                annotationKey = $"ui.{keyword["ui:".Length..]}";
                return true;
            }

            if (keyword.StartsWith("jsonEditor:", StringComparison.Ordinal))
            {
                annotationKey = $"jsonEditor.{keyword["jsonEditor:".Length..]}";
                return true;
            }

            annotationKey = keyword switch
            {
                "propertyOrder" => "jsonEditor.propertyOrder",
                "options" => "jsonEditor.options",
                "watch" => "jsonEditor.watch",
                "template" => "jsonEditor.template",
                _ => string.Empty,
            };

            return annotationKey.Length > 0;
        }

        private static string EscapePointer(string value)
        {
            return value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
        }

        private static void TryAddConstraint(JsonElement element, string key, List<ConstraintEntry> entries)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out JsonElement value))
            {
                entries.Add(new ConstraintEntry(key, value.ToString()));
            }
        }
    }
}
