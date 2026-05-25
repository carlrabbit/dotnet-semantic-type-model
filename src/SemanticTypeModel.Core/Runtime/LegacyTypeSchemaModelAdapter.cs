using System.Globalization;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Abstractions.Runtime;
using Legacy = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Runtime;

/// <summary>
/// Adapts legacy canonical models to the hardened runtime model surface used by the runtime API.
/// </summary>
public static class LegacyTypeSchemaModelAdapter
{
    /// <summary>
    /// Converts a legacy <see cref="Legacy.TypeSchemaModel"/> into the hardened canonical runtime model.
    /// </summary>
    public static TypeSchemaModelResult Adapt(Legacy.TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new ConversionState().Convert(model);
    }

    private sealed class ConversionState
    {
        private const string UnknownTypeIdValue = "__unknown";
        private const string StringKeyTypeIdValue = "__stringKey";

        private readonly Dictionary<string, TypeDefinition> _typesById = new(StringComparer.Ordinal);
        private readonly Queue<PendingShape> _pending = new();
        private readonly List<SchemaDiagnostic> _diagnostics = [];
        private int _inlineCounter;

        public TypeSchemaModelResult Convert(Legacy.TypeSchemaModel model)
        {
            foreach ((var identifier, Legacy.TypeShape shape) in model.Shapes.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                Enqueue(identifier, shape, $"/shapes/{Escape(identifier)}");
            }

            while (_pending.Count > 0)
            {
                PendingShape pending = _pending.Dequeue();
                if (_typesById.ContainsKey(pending.Identifier))
                {
                    continue;
                }

                _typesById[pending.Identifier] = ConvertShape(pending.Identifier, pending.Shape, pending.Path);
            }

            var modelId = ResolveModelId(model.RootIdentifier);
            List<TypeDefinition> types = [.. _typesById.OrderBy(static pair => pair.Key, StringComparer.Ordinal).Select(static pair => pair.Value)];

            return new TypeSchemaModelResult
            {
                Model = new TypeSchemaModel
                {
                    Id = new SchemaModelId(modelId),
                    Types = types,
                    TypesById = types.ToDictionary(static type => type.Id, static type => type),
                    Annotations = new AnnotationBag(),
                },
                Diagnostics = [.. _diagnostics],
            };
        }

        private string ResolveModelId(string? rootIdentifier)
        {
            if (!string.IsNullOrWhiteSpace(rootIdentifier) && _typesById.ContainsKey(rootIdentifier))
            {
                return rootIdentifier;
            }

            if (!string.IsNullOrWhiteSpace(rootIdentifier))
            {
                _diagnostics.Add(CreateDiagnostic(
                    code: "STM3101",
                    message: $"Legacy root identifier '{rootIdentifier}' was not found in the legacy model. The hardened runtime adapter selected the first available type instead.",
                    modelPath: "/"));
            }

            return _typesById.Keys.FirstOrDefault() ?? "Model";
        }

        private void Enqueue(string identifier, Legacy.TypeShape shape, string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(identifier);
            ArgumentNullException.ThrowIfNull(shape);
            _pending.Enqueue(new PendingShape(identifier, shape, path));
        }

        private TypeDefinition ConvertShape(string identifier, Legacy.TypeShape shape, string path)
        {
            TypeId typeId = new(identifier);
            AnnotationBag annotations = ConvertAnnotations(shape.Annotations, AnnotationScope.Type);
            ConstraintSet constraints = ConvertConstraints(shape.Constraints);

            return shape switch
            {
                Legacy.ObjectShape obj => new ObjectTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    DisplayName = TryGetAnnotationText(shape.Annotations, "title"),
                    Description = TryGetAnnotationText(shape.Annotations, "description"),
                    Kind = TypeKind.Object,
                    Nullability = Nullability.NonNullable,
                    Annotations = WithBooleanAnnotation(annotations, "runtime.additionalPropertiesAllowed", obj.AdditionalPropertiesAllowed),
                    Properties =
                    [
                        .. obj.Properties.Select(property => new PropertyDefinition
                        {
                            Id = new PropertyId($"{identifier}_{property.Name}"),
                            Name = property.Name,
                            DisplayName = TryGetAnnotationText(property.Annotations, "title"),
                            Description = TryGetAnnotationText(property.Annotations, "description"),
                            Type = ResolveRef(property.Type, $"{path}/properties/{Escape(property.Name)}"),
                            Cardinality = new Cardinality
                            {
                                IsRequired = property.IsRequired,
                                AllowsNull = property.IsNullable,
                            },
                            Mutability = Mutability.Mutable,
                            Constraints = new ConstraintSet(),
                            Annotations = ConvertAnnotations(property.Annotations, AnnotationScope.Member),
                        }),
                    ],
                    Keys = [],
                    Relationships = [],
                    Composition = new ObjectComposition(),
                },
                Legacy.ScalarShape scalar => new ScalarTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    DisplayName = TryGetAnnotationText(shape.Annotations, "title"),
                    Description = TryGetAnnotationText(shape.Annotations, "description"),
                    Kind = TypeKind.Scalar,
                    Nullability = scalar.IsNullable || scalar.Kind == Legacy.ScalarKind.Null ? Nullability.Nullable : Nullability.NonNullable,
                    Annotations = annotations,
                    ScalarKind = ConvertScalarKind(identifier, scalar.Kind, path),
                    Format = TryGetConstraintValue(shape.Constraints, "format"),
                },
                Legacy.EnumShape @enum => new EnumTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    DisplayName = TryGetAnnotationText(shape.Annotations, "title"),
                    Description = TryGetAnnotationText(shape.Annotations, "description"),
                    Kind = TypeKind.Enum,
                    Nullability = Nullability.NonNullable,
                    Annotations = annotations,
                    StorageKind = EnumStorageKind.String,
                    Values =
                    [
                        .. @enum.Values.Select(value => new EnumValueDefinition
                        {
                            Name = value,
                            Value = value,
                            Annotations = new AnnotationBag(),
                        }),
                    ],
                },
                Legacy.ArrayShape array => new ArrayTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    DisplayName = TryGetAnnotationText(shape.Annotations, "title"),
                    Description = TryGetAnnotationText(shape.Annotations, "description"),
                    Kind = TypeKind.Array,
                    Nullability = Nullability.NonNullable,
                    Annotations = annotations,
                    ItemType = ResolveRef(array.Items, $"{path}/items"),
                    UniqueItems = constraints.Array?.UniqueItems ?? false,
                    MinItems = constraints.Array?.MinItems,
                    MaxItems = constraints.Array?.MaxItems,
                },
                Legacy.DictionaryShape dictionary => new DictionaryTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    DisplayName = TryGetAnnotationText(shape.Annotations, "title"),
                    Description = TryGetAnnotationText(shape.Annotations, "description"),
                    Kind = TypeKind.Dictionary,
                    Nullability = Nullability.NonNullable,
                    Annotations = annotations,
                    KeyType = EnsureStringKeyType(),
                    ValueType = ResolveRef(dictionary.Values, $"{path}/values"),
                },
                Legacy.UnionShape union => new UnionTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    DisplayName = TryGetAnnotationText(shape.Annotations, "title"),
                    Description = TryGetAnnotationText(shape.Annotations, "description"),
                    Kind = TypeKind.Union,
                    Nullability = Nullability.NonNullable,
                    Annotations = annotations,
                    Semantics = UnionSemantics.OneOf,
                    Options = [.. union.Options.Select((option, index) => ResolveRef(option, $"{path}/options/{index}"))],
                },
                _ => new ScalarTypeDefinition
                {
                    Id = typeId,
                    Name = identifier,
                    Kind = TypeKind.Scalar,
                    Nullability = Nullability.NonNullable,
                    Annotations = annotations,
                    ScalarKind = ScalarKind.Unknown,
                },
            };
        }

        private TypeRef ResolveRef(Legacy.ShapeRef? shapeRef, string path)
        {
            if (shapeRef?.Identifier is not null)
            {
                return new TypeRef(new TypeId(shapeRef.Identifier));
            }

            if (shapeRef?.Inline is not null)
            {
                var inlineIdentifier = $"__inline_{++_inlineCounter}";
                Enqueue(inlineIdentifier, shapeRef.Inline, path);
                return new TypeRef(new TypeId(inlineIdentifier));
            }

            _diagnostics.Add(CreateDiagnostic(
                code: "STM3102",
                message: "A legacy shape reference was missing. The hardened runtime adapter substituted an unknown scalar type.",
                modelPath: path));

            return EnsureUnknownType();
        }

        private TypeRef EnsureUnknownType()
        {
            if (!_typesById.ContainsKey(UnknownTypeIdValue) && !_pending.Any(static pending => pending.Identifier == UnknownTypeIdValue))
            {
                Enqueue(UnknownTypeIdValue, new Legacy.ScalarShape { Kind = Legacy.ScalarKind.String }, $"/shapes/{UnknownTypeIdValue}");
            }

            return new TypeRef(new TypeId(UnknownTypeIdValue));
        }

        private TypeRef EnsureStringKeyType()
        {
            if (!_typesById.ContainsKey(StringKeyTypeIdValue) && !_pending.Any(static pending => pending.Identifier == StringKeyTypeIdValue))
            {
                Enqueue(StringKeyTypeIdValue, new Legacy.ScalarShape { Kind = Legacy.ScalarKind.String }, $"/shapes/{StringKeyTypeIdValue}");
            }

            return new TypeRef(new TypeId(StringKeyTypeIdValue));
        }

        private ScalarKind ConvertScalarKind(string identifier, Legacy.ScalarKind scalarKind, string path)
        {
            return scalarKind switch
            {
                Legacy.ScalarKind.Boolean => ScalarKind.Boolean,
                Legacy.ScalarKind.Integer => ScalarKind.Integer,
                Legacy.ScalarKind.Number => ScalarKind.Number,
                Legacy.ScalarKind.String => ScalarKind.String,
                Legacy.ScalarKind.Null => ReportNullScalar(identifier, path),
                _ => ScalarKind.Unknown,
            };
        }

        private static AnnotationBag ConvertAnnotations(IReadOnlyList<Legacy.SchemaAnnotation> annotations, AnnotationScope scope)
        {
            return new AnnotationBag
            {
                Items =
                [
                    .. annotations.Select(annotation => new Annotation
                    {
                        Key = new AnnotationKey(annotation.Key.Replace(':', '.')),
                        Value = ParseAnnotationValue(annotation.Value),
                        Scope = scope,
                        Source = AnnotationSource.Imported,
                    }),
                ],
            };
        }

        private static AnnotationBag WithBooleanAnnotation(AnnotationBag annotations, string key, bool value)
        {
            return annotations with
            {
                Items =
                [
                    .. annotations.Items,
                    new Annotation
                    {
                        Key = new AnnotationKey(key),
                        Value = value,
                        Scope = AnnotationScope.Type,
                        Source = AnnotationSource.Imported,
                    },
                ],
            };
        }

        private static ConstraintSet ConvertConstraints(Legacy.ConstraintSet constraints)
        {
            ArgumentNullException.ThrowIfNull(constraints);

            StringConstraints? stringConstraints = null;
            NumericConstraints? numericConstraints = null;
            ArrayConstraints? arrayConstraints = null;
            ObjectConstraints? objectConstraints = null;
            List<CustomConstraint> customConstraints = [];

            foreach (Legacy.ConstraintEntry entry in constraints.Entries)
            {
                switch (entry.Key)
                {
                    case "minLength":
                        stringConstraints ??= new StringConstraints();
                        stringConstraints = stringConstraints with { MinLength = ParseNullableInt(entry.Value) };
                        break;
                    case "maxLength":
                        stringConstraints ??= new StringConstraints();
                        stringConstraints = stringConstraints with { MaxLength = ParseNullableInt(entry.Value) };
                        break;
                    case "pattern":
                        stringConstraints ??= new StringConstraints();
                        stringConstraints = stringConstraints with { Pattern = entry.Value };
                        break;
                    case "minimum":
                        numericConstraints ??= new NumericConstraints();
                        numericConstraints = numericConstraints with { Minimum = ParseNullableDecimal(entry.Value) };
                        break;
                    case "maximum":
                        numericConstraints ??= new NumericConstraints();
                        numericConstraints = numericConstraints with { Maximum = ParseNullableDecimal(entry.Value) };
                        break;
                    case "exclusiveMinimum":
                        numericConstraints ??= new NumericConstraints();
                        numericConstraints = numericConstraints with { ExclusiveMinimum = ParseNullableBoolean(entry.Value) ?? true };
                        break;
                    case "exclusiveMaximum":
                        numericConstraints ??= new NumericConstraints();
                        numericConstraints = numericConstraints with { ExclusiveMaximum = ParseNullableBoolean(entry.Value) ?? true };
                        break;
                    case "multipleOf":
                        numericConstraints ??= new NumericConstraints();
                        numericConstraints = numericConstraints with { MultipleOf = ParseNullableDecimal(entry.Value) };
                        break;
                    case "minItems":
                        arrayConstraints ??= new ArrayConstraints();
                        arrayConstraints = arrayConstraints with { MinItems = ParseNullableInt(entry.Value) };
                        break;
                    case "maxItems":
                        arrayConstraints ??= new ArrayConstraints();
                        arrayConstraints = arrayConstraints with { MaxItems = ParseNullableInt(entry.Value) };
                        break;
                    case "uniqueItems":
                        arrayConstraints ??= new ArrayConstraints();
                        arrayConstraints = arrayConstraints with { UniqueItems = ParseNullableBoolean(entry.Value) ?? true };
                        break;
                    case "minProperties":
                        objectConstraints ??= new ObjectConstraints();
                        objectConstraints = objectConstraints with { MinProperties = ParseNullableInt(entry.Value) };
                        break;
                    case "maxProperties":
                        objectConstraints ??= new ObjectConstraints();
                        objectConstraints = objectConstraints with { MaxProperties = ParseNullableInt(entry.Value) };
                        break;
                    case "additionalProperties":
                        objectConstraints ??= new ObjectConstraints();
                        objectConstraints = objectConstraints with
                        {
                            AdditionalProperties = ParseNullableBoolean(entry.Value) == false
                                ? AdditionalPropertiesPolicy.Disallow
                                : AdditionalPropertiesPolicy.Allow,
                        };
                        break;
                    default:
                        customConstraints.Add(new CustomConstraint
                        {
                            Name = entry.Key,
                            Value = entry.Value,
                            Annotations = new AnnotationBag(),
                        });
                        break;
                }
            }

            return new ConstraintSet
            {
                String = stringConstraints,
                Numeric = numericConstraints,
                Array = arrayConstraints,
                Object = objectConstraints,
                Custom = customConstraints,
            };
        }

        private static string? TryGetConstraintValue(Legacy.ConstraintSet constraints, string key)
        {
            return constraints.Entries.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.Ordinal))?.Value;
        }

        private static string? TryGetAnnotationText(IReadOnlyList<Legacy.SchemaAnnotation> annotations, string key)
        {
            return annotations.FirstOrDefault(annotation => string.Equals(annotation.Key, key, StringComparison.Ordinal))?.Value;
        }

        private static object? ParseAnnotationValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            try
            {
                using var document = JsonDocument.Parse(value);
                return document.RootElement.ValueKind switch
                {
                    JsonValueKind.String => document.RootElement.GetString(),
                    JsonValueKind.Number when document.RootElement.TryGetInt64(out var integer) => integer,
                    JsonValueKind.Number => document.RootElement.GetDecimal(),
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

        private static int? ParseNullableInt(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static decimal? ParseNullableDecimal(string value)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static bool? ParseNullableBoolean(string value)
        {
            return bool.TryParse(value, out var parsed)
                ? parsed
                : null;
        }

        private static SchemaDiagnostic CreateDiagnostic(string code, string message, string modelPath)
        {
            return new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = code,
                Message = message,
                Stage = SchemaDiagnosticStage.Import,
                ModelPath = modelPath,
            };
        }

        private static string Escape(string segment)
        {
            return segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
        }

        private ScalarKind ReportNullScalar(string identifier, string path)
        {
            _diagnostics.Add(CreateDiagnostic(
                code: "STM3103",
                message: $"Legacy scalar '{identifier}' used the explicit null scalar kind. The hardened runtime adapter mapped it to an unknown nullable scalar.",
                modelPath: path));

            return ScalarKind.Unknown;
        }

        private sealed record PendingShape(string Identifier, Legacy.TypeShape Shape, string Path);
    }
}
