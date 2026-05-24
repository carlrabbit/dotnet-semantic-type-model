using System.Text.Json;
using SemanticTypeModel.Abstractions.Contracts;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;

namespace SemanticTypeModel.JsonSchema.Import;

/// <summary>
/// Imports a JSON Schema Draft 2020-12 document into the canonical semantic type model.
/// Supports object schemas, scalar schemas, arrays, enums, required properties, nullable semantics,
/// <c>$defs</c>, <c>$ref</c>, <c>oneOf</c> baseline support, and annotations.
/// </summary>
public sealed class JsonSchemaImporter : ISchemaModelSource
{
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
        using var doc = JsonDocument.Parse(_json);
        return Import(doc.RootElement);
    }

    private static TypeSchemaModel Import(JsonElement root)
    {
        var builder = new TypeSchemaModelBuilder();
        var context = new ImportContext(builder);

        if (root.TryGetProperty("$defs", out JsonElement defs))
        {
            foreach (JsonProperty def in defs.EnumerateObject())
            {
                context.RegisterDef(def.Name, def.Value);
            }
        }

        context.ResolveAllDefs();

        var rootId = root.TryGetProperty("$id", out JsonElement idProp)
            ? idProp.GetString() ?? "#"
            : "#";

        TypeShape rootShape = context.ParseShape(root, rootId);
        _ = builder.AddShape(rootId, rootShape);
        _ = builder.SetRoot(rootId);

        return builder.Build();
    }

    private sealed class ImportContext(TypeSchemaModelBuilder builder)
    {
        private readonly Dictionary<string, JsonElement> _defElements = new(StringComparer.Ordinal);
        private readonly HashSet<string> _resolved = new(StringComparer.Ordinal);

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

        private void ResolveDef(string name, JsonElement element)
        {
            if (!_resolved.Add(name))
            {
                return;
            }

            TypeShape shape = ParseShape(element, name);
            _ = builder.AddShape(name, shape);
        }

        public TypeShape ParseShape(JsonElement element, string? identifier)
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
            ConstraintSet constraints = ParseConstraints(element);

            if (element.TryGetProperty("$ref", out JsonElement refProp))
            {
                return new UnionShape
                {
                    Identifier = identifier,
                    Options = [BuildShapeRef(refProp.GetString() ?? string.Empty)],
                    Annotations = annotations,
                    Constraints = constraints,
                };
            }

            if (element.TryGetProperty("oneOf", out JsonElement oneOfProp))
            {
                return ParseOneOf(oneOfProp, identifier, annotations, constraints);
            }

            if (element.TryGetProperty("enum", out JsonElement enumProp))
            {
                var values = enumProp.EnumerateArray().Select(static value => value.ToString()).ToList();
                return new EnumShape
                {
                    Identifier = identifier,
                    Values = values,
                    Annotations = annotations,
                    Constraints = constraints,
                };
            }

            return TryGetType(element, out var typeName, out var isNullable)
                ? typeName switch
                {
                    "object" => ParseObject(element, identifier, annotations, constraints),
                    "array" => ParseArray(element, identifier, annotations, constraints),
                    "string" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.String, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "integer" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Integer, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "number" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Number, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "boolean" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Boolean, IsNullable = isNullable, Annotations = annotations, Constraints = constraints },
                    "null" => new ScalarShape { Identifier = identifier, Kind = ScalarKind.Null, IsNullable = true, Annotations = annotations, Constraints = constraints },
                    _ => new ObjectShape { Identifier = identifier, Annotations = annotations, Constraints = constraints },
                }
                : new ObjectShape { Identifier = identifier, Annotations = annotations, Constraints = constraints };
        }

        private TypeShape ParseObject(JsonElement element, string? identifier, IReadOnlyList<SchemaAnnotation> annotations, ConstraintSet constraints)
        {
            var requiredSet = new HashSet<string>(StringComparer.Ordinal);
            if (element.TryGetProperty("required", out JsonElement requiredProp))
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
                ShapeRef values = BuildShapeRef(additionalPropertiesSchema, null);
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

            if (element.TryGetProperty("properties", out JsonElement propertiesProp))
            {
                foreach (JsonProperty property in propertiesProp.EnumerateObject())
                {
                    List<SchemaAnnotation> propertyAnnotations = ParseAnnotations(property.Value);
                    var propertyNullable = IsNullable(property.Value);
                    ShapeRef propertyType = BuildShapeRef(property.Value, null);

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

            if (element.TryGetProperty("additionalProperties", out JsonElement additionalPropFlag) &&
                additionalPropFlag.ValueKind == JsonValueKind.False)
            {
                additionalAllowed = false;
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

        private ArrayShape ParseArray(JsonElement element, string? identifier, IReadOnlyList<SchemaAnnotation> annotations, ConstraintSet constraints)
        {
            ShapeRef? items = null;
            if (element.TryGetProperty("items", out JsonElement itemsProp))
            {
                items = BuildShapeRef(itemsProp, null);
            }

            return new ArrayShape
            {
                Identifier = identifier,
                Items = items,
                Annotations = annotations,
                Constraints = constraints,
            };
        }

        private TypeShape ParseOneOf(JsonElement oneOfProp, string? identifier, IReadOnlyList<SchemaAnnotation> annotations, ConstraintSet constraints)
        {
            var options = oneOfProp.EnumerateArray().ToList();

            if (TryGetNullableUnionTarget(options, out JsonElement nonNullOption))
            {
                TypeShape innerShape = ParseShape(nonNullOption, identifier);
                return MakeNullable(innerShape, annotations, constraints);
            }

            var shapeRefs = options.Select(option => BuildShapeRef(option, null)).ToList();

            return new UnionShape
            {
                Identifier = identifier,
                Options = shapeRefs,
                Annotations = annotations,
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

        private ShapeRef BuildShapeRef(JsonElement element, string? inlineIdentifier)
        {
            if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return ShapeRef.FromInline(ParseShape(element, inlineIdentifier));
            }

            if (element.TryGetProperty("$ref", out JsonElement refProp))
            {
                return BuildShapeRef(refProp.GetString() ?? string.Empty);
            }

            if (element.TryGetProperty("oneOf", out JsonElement oneOfProp))
            {
                var options = oneOfProp.EnumerateArray().ToList();
                if (TryGetNullableUnionTarget(options, out JsonElement nonNullOption))
                {
                    if (nonNullOption.TryGetProperty("$ref", out JsonElement innerRef))
                    {
                        return BuildShapeRef(innerRef.GetString() ?? string.Empty);
                    }

                    TypeShape nullableInline = MakeNullable(
                        ParseShape(nonNullOption, inlineIdentifier),
                        ParseAnnotations(element),
                        ParseConstraints(element));
                    return ShapeRef.FromInline(nullableInline);
                }
            }

            return ShapeRef.FromInline(ParseShape(element, inlineIdentifier));
        }

        private ShapeRef BuildShapeRef(string refValue)
        {
            var resolvedId = ResolveRef(refValue);

            if (_defElements.TryGetValue(resolvedId, out JsonElement defElement) && !_resolved.Contains(resolvedId))
            {
                ResolveDef(resolvedId, defElement);
            }

            return ShapeRef.FromIdentifier(resolvedId);
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
                    : refValue;
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
                annotations.Add(new SchemaAnnotation("title", titleValue));
            }

            if (element.TryGetProperty("description", out JsonElement description) && description.GetString() is { } descriptionValue)
            {
                annotations.Add(new SchemaAnnotation("description", descriptionValue));
            }

            if (element.TryGetProperty("default", out JsonElement defaultValue))
            {
                annotations.Add(new SchemaAnnotation("default", defaultValue.GetRawText()));
            }

            if (element.TryGetProperty("examples", out JsonElement examplesValue))
            {
                annotations.Add(new SchemaAnnotation("examples", examplesValue.GetRawText()));
            }

            return annotations;
        }

        private static ConstraintSet ParseConstraints(JsonElement element)
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

            return entries.Count == 0 ? ConstraintSet.Empty : new ConstraintSet { Entries = entries };
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
