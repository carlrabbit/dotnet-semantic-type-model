using System.Globalization;
using System.Text.Json;
#pragma warning disable CS1591, IDE0046, IDE0060
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData;

public enum TestDataSizeProfile { Simple, Moderate, Extreme }

public abstract record SemanticTestValue
{
    protected SemanticTestValue(TypeId typeId)
    {
        TypeId = typeId;
    }

    public TypeId TypeId { get; }
}

public sealed record ScalarTestValue(TypeId ScalarTypeId, ScalarKind ScalarKind, object? Value) : SemanticTestValue(ScalarTypeId);
public sealed record EnumTestValue(TypeId EnumTypeId, object Value) : SemanticTestValue(EnumTypeId);
public sealed record ObjectTestValue(TypeId ObjectTypeId, IReadOnlyDictionary<PropertyId, SemanticTestValue> Properties) : SemanticTestValue(ObjectTypeId);
public sealed record ArrayTestValue(TypeId ArrayTypeId, IReadOnlyList<SemanticTestValue> Items) : SemanticTestValue(ArrayTypeId);
public sealed record DictionaryTestValue(TypeId DictionaryTypeId, IReadOnlyList<KeyValuePair<SemanticTestValue, SemanticTestValue>> Entries) : SemanticTestValue(DictionaryTypeId);
public sealed record NullTestValue(TypeId NullableTypeId) : SemanticTestValue(NullableTypeId);

public sealed record TestDataGenerationResult
{
    public SemanticTestValue? Value { get; init; }
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; } = [];
    public bool Succeeded => Value is not null && Diagnostics.All(static d => d.Severity != SchemaDiagnosticSeverity.Error);
    public bool HasErrors => Diagnostics.Any(static d => d.Severity == SchemaDiagnosticSeverity.Error);
}

public static class SemanticTestDataGenerator
{
    public static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, TestDataSizeProfile profile = TestDataSizeProfile.Simple, int seed = 0)
    {
        return Generate(model, rootType, profile, seed, null);
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology)
    {
        return Generate(model, rootType, profile, seed, terminology, null);
    }

    internal static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology, SemanticTestDataOptions? options)
    {
        ArgumentNullException.ThrowIfNull(model);
        options?.Budgets.Validate();
        var context = new Context(model, profile, seed, terminology, options);
        SemanticTestValue? value = context.Generate(rootType, new ConstraintSet(), ModelPath.ForType(rootType), false, false, 0, []);
        return new TestDataGenerationResult { Value = value, Diagnostics = context.Diagnostics };
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, TypeDefinition rootType, TestDataSizeProfile profile = TestDataSizeProfile.Simple, int seed = 0)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        return Generate(model, rootType.Id, profile, seed);
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, SemanticTerminologyProfile terminology, TestDataSizeProfile profile = TestDataSizeProfile.Simple, int seed = 0)
    {
        ArgumentNullException.ThrowIfNull(terminology);
        return Generate(model, rootType, profile, seed, SemanticTerminologyProfileJson.ValidateForConsumption(model, terminology));
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, string rootTypeId, TestDataSizeProfile profile = TestDataSizeProfile.Simple, int seed = 0)
    {
        return Generate(model, new TypeId(rootTypeId), profile, seed);
    }

    private sealed class Context(TypeSchemaModel model, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology, SemanticTestDataOptions? options)
    {
        private readonly Random _random = new(seed);
        private readonly TestDataBudgets _budgets = options?.Budgets ?? new();
        private int _nodes;
        internal List<SchemaDiagnostic> Diagnostics { get; } = [];

        internal SemanticTestValue? Generate(TypeId id, ConstraintSet useConstraints, string path, bool allowsNull, bool optional, int depth, HashSet<TypeId> ancestors, IReadOnlyList<JsonElement>? candidates = null, bool customCandidate = false)
        {
            if (!model.TypesById.TryGetValue(id, out TypeDefinition? type))
            {
                return Error("TESTDATA_UNRESOLVED_REFERENCE", $"Type '{id.Value}' could not be resolved.", path);
            }

            if (++_nodes > _budgets.MaxNodes)
            {
                return Error("TESTDATA_NODE_BUDGET_EXHAUSTED", "Generation exceeded the total value-node budget.", path);
            }

            if (depth > _budgets.MaxDepth)
            {
                return Error("TESTDATA_DEPTH_BUDGET_EXHAUSTED", "Generation exceeded the nested-generation depth budget.", path);
            }

            if (ancestors.Contains(id))
            {
                return allowsNull
                    ? new NullTestValue(id)
                    : optional ? null : Error("TESTDATA_RECURSION_UNTERMINATED", "Recursive generation has no legal finite terminator.", path);
            }

            var next = new HashSet<TypeId>(ancestors) { id };
            return type switch
            {
                ScalarTypeDefinition scalar => GenerateScalar(scalar, useConstraints, path, candidates, customCandidate),
                EnumTypeDefinition @enum => GenerateEnum(@enum, path),
                ObjectTypeDefinition obj => GenerateObject(obj, useConstraints, path, next, depth),
                ArrayTypeDefinition array => GenerateArray(array, useConstraints, path, next, depth),
                DictionaryTypeDefinition dictionary => GenerateDictionary(dictionary, useConstraints, path, next, depth),
                ReferenceTypeDefinition reference => GenerateReference(reference, useConstraints, path, allowsNull, optional, depth, next, candidates, customCandidate),
                UnionTypeDefinition => Error("TESTDATA_UNSUPPORTED_TYPE", "Union generation is unsupported.", path),
                IntersectionTypeDefinition => Error("TESTDATA_UNSUPPORTED_TYPE", "Intersection generation is unsupported.", path),
                _ when type.Kind == TypeKind.Any => new ScalarTestValue(id, ScalarKind.Json, "{}"),
                _ when type.Kind == TypeKind.Never => Error("TESTDATA_UNSUPPORTED_TYPE", "Never has no valid generated value.", path),
                _ => Error("TESTDATA_UNSUPPORTED_TYPE", $"Type kind '{type.Kind}' is unsupported.", path)
            };
        }

        private SemanticTestValue? GenerateReference(ReferenceTypeDefinition reference, ConstraintSet constraints, string path, bool allowsNull, bool optional, int depth, HashSet<TypeId> ancestors, IReadOnlyList<JsonElement>? candidates, bool customCandidate)
        {
            return Generate(reference.Target.Id, constraints, path, allowsNull, optional, depth + 1, ancestors, candidates, customCandidate);
        }

        private SemanticTestValue? GenerateEnum(EnumTypeDefinition @enum, string path)
        {
            if (@enum.Values.Count == 0)
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Enum has no usable declared value.", path);
            }

            EnumValueDefinition value = @enum.Values[_random.Next(@enum.Values.Count)];
            return new EnumTestValue(@enum.Id, value.Value);
        }

        private SemanticTestValue? GenerateObject(ObjectTypeDefinition obj, ConstraintSet constraints, string path, HashSet<TypeId> ancestors, int depth)
        {
            if (constraints.Custom.Count > 0)
            {
                return CustomError(path);
            }

            ObjectConstraints? objectConstraints = constraints.Object;
            if (objectConstraints is not null && objectConstraints.MinProperties is int min && objectConstraints.MaxProperties is int max && min > max)
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Object property bounds are contradictory.", path);
            }

            IReadOnlyList<(ObjectTypeDefinition Owner, PropertyDefinition Property)> properties = EffectiveProperties(obj, ancestors);
            var count = properties.Count;
            if (objectConstraints?.MinProperties is int minProperties && count < minProperties)
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Minimum property count cannot be satisfied by modeled properties.", path);
            }

            if (objectConstraints?.MaxProperties is int maxProperties && count > maxProperties)
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Maximum property count conflicts with required modeled properties.", path);
            }

            var result = new Dictionary<PropertyId, SemanticTestValue>();
            foreach ((ObjectTypeDefinition owner, PropertyDefinition property) in properties)
            {
                var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
                ConstraintSet propertyConstraints = property.Constraints;
                if (model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? propertyType) && propertyType is ArrayTypeDefinition)
                {
                    propertyConstraints = propertyConstraints with
                    {
                        Array = new ArrayConstraints
                        {
                            MinItems = Max(propertyConstraints.Array?.MinItems, property.Cardinality.MinItems),
                            MaxItems = Min(propertyConstraints.Array?.MaxItems, property.Cardinality.MaxItems),
                            UniqueItems = propertyConstraints.Array?.UniqueItems == true
                        }
                    };
                }
                var logicalType = property.Annotations.Items.FirstOrDefault(a => a.Key.Value == "schema.logicalType")?.Value as string;
                TestDataGeneratorContext callbackContext = new(model, propertyType, property, logicalType, profile, _random.Next(), options?.RootOrdinal ?? 0);
                var customValue = options?.PropertyGenerator?.Invoke(owner, property, callbackContext);
                customValue ??= logicalType is null ? null : options?.LogicalTypeGenerator?.Invoke(logicalType, callbackContext);
                var customCandidate = customValue is not null;
                IReadOnlyList<JsonElement>? candidates = customValue is null
                    ? terminology?.FindCandidates(owner, property)
                    : [JsonSerializer.SerializeToElement(customValue)];
                SemanticTestValue? value = Generate(property.Type.Id, propertyConstraints, propertyPath, property.Cardinality.AllowsNull, !property.Cardinality.IsRequired, depth + 1, ancestors, candidates, customCandidate);
                if (value is null)
                {
                    if (property.Cardinality.IsRequired)
                    {
                        return Error("TESTDATA_RECURSION_UNTERMINATED", "Required property could not be finitely generated.", propertyPath);
                    }

                    continue;
                }
                result[property.Id] = value;
            }
            return new ObjectTestValue(obj.Id, result);
        }

        private IReadOnlyList<(ObjectTypeDefinition Owner, PropertyDefinition Property)> EffectiveProperties(ObjectTypeDefinition obj, HashSet<TypeId> ancestors)
        {
            var result = new List<(ObjectTypeDefinition, PropertyDefinition)>();
            foreach (TypeRef baseRef in obj.Composition.AllOf)
            {
                if (model.TypesById.TryGetValue(baseRef.Id, out TypeDefinition? baseType) && baseType is ObjectTypeDefinition baseObject && !ancestors.Contains(baseObject.Id))
                {
                    result.AddRange(EffectiveProperties(baseObject, ancestors));
                }
            }
            result.AddRange(obj.Properties.Select(property => (obj, property)));
            return [.. result.GroupBy(static pair => pair.Item2.Id).Select(static group => group.Last())];
        }

        private SemanticTestValue? GenerateArray(ArrayTypeDefinition array, ConstraintSet useConstraints, string path, HashSet<TypeId> ancestors, int depth)
        {
            ArrayConstraints effective = MergeArray(array.MinItems, array.MaxItems, array.UniqueItems, useConstraints.Array, useConstraints);
            if (!RangeValid(effective.MinItems, effective.MaxItems))
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Array item bounds are contradictory.", path);
            }

            var count = Target(effective.MinItems, effective.MaxItems, ProfileTarget());
            if (count > _budgets.MaxCollectionItems || effective.MinItems > _budgets.MaxCollectionItems)
            {
                return Error("TESTDATA_SIZE_BUDGET_EXHAUSTED", "Array generation exceeds the fixed item safety budget.", path);
            }

            var items = new List<SemanticTestValue>();
            var fingerprints = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                SemanticTestValue? item = Generate(array.ItemType.Id, new ConstraintSet(), path + "/items/" + i.ToString(CultureInfo.InvariantCulture), false, false, depth + 1, ancestors);
                if (item is null)
                {
                    return null;
                }

                if (effective.UniqueItems && !fingerprints.Add(Fingerprint(item)))
                {
                    var attempts = 0;
                    while (item is not null && attempts++ < 100 && fingerprints.Contains(Fingerprint(item)))
                    {
                        item = Generate(array.ItemType.Id, new ConstraintSet(), path + "/items/" + i.ToString(CultureInfo.InvariantCulture), false, false, depth + 1, ancestors);
                    }
                    if (item is null || !fingerprints.Add(Fingerprint(item)))
                    {
                        return Error("TESTDATA_UNIQUENESS_EXHAUSTED", "Unique item generation exhausted its finite domain.", path);
                    }
                }
                items.Add(item);
            }
            return new ArrayTestValue(array.Id, items);
        }

        private SemanticTestValue? GenerateDictionary(DictionaryTypeDefinition dictionary, ConstraintSet useConstraints, string path, HashSet<TypeId> ancestors, int depth)
        {
            ArrayConstraints? constraints = useConstraints.Array;
            if (!RangeValid(constraints?.MinItems, constraints?.MaxItems))
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Dictionary entry bounds are contradictory.", path);
            }

            var count = Target(constraints?.MinItems, constraints?.MaxItems, ProfileTarget());
            if (count > _budgets.MaxDictionaryEntries)
            {
                return Error("TESTDATA_SIZE_BUDGET_EXHAUSTED", "Dictionary generation exceeds the fixed entry safety budget.", path);
            }

            var entries = new List<KeyValuePair<SemanticTestValue, SemanticTestValue>>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                SemanticTestValue? key = Generate(dictionary.KeyType.Id, new ConstraintSet(), path + "/keys/" + i, false, false, depth + 1, ancestors);
                SemanticTestValue? value = Generate(dictionary.ValueType.Id, new ConstraintSet(), path + "/values/" + i, false, false, depth + 1, ancestors);
                if (key is null || value is null)
                {
                    return null;
                }

                if (!keys.Add(Fingerprint(key)))
                {
                    return Error("TESTDATA_UNIQUENESS_EXHAUSTED", "Dictionary key generation exhausted its finite domain.", path);
                }

                entries.Add(new(key, value));
            }
            return new DictionaryTestValue(dictionary.Id, entries);
        }

        private SemanticTestValue? GenerateScalar(ScalarTypeDefinition scalar, ConstraintSet constraints, string path, IReadOnlyList<JsonElement>? candidates = null, bool customCandidate = false)
        {
            if (constraints.Custom.Count > 0)
            {
                return CustomError(path);
            }

            if (constraints.String?.Pattern is { Length: > 0 })
            {
                if (TryCandidate(scalar, constraints, candidates, out SemanticTestValue? candidate))
                {
                    return candidate;
                }
                if (customCandidate)
                {
                    return Error("TESTDATA_CUSTOM_CANDIDATE_INVALID", "A custom generator supplied a candidate that violates the canonical pattern or scalar contract.", path);
                }
                return Error("TESTDATA_PATTERN_UNSUPPORTED", "Pattern-constrained strings require an external or custom value source.", path);
            }

            var format = scalar.Format;
            if (scalar.ScalarKind == ScalarKind.Unknown)
            {
                return Error("TESTDATA_UNSUPPORTED_SCALAR", "Unknown scalar generation is unsupported.", path);
            }

            if (format is not null && !SupportedFormat(format))
            {
                return Error("TESTDATA_FORMAT_UNSUPPORTED", $"Format '{format}' is not supported by built-in generation.", path);
            }

            if (constraints.Numeric is { } numeric && !NumericRangeValid(numeric))
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Numeric bounds are contradictory.", path);
            }

            if (constraints.String is { } text && !RangeValid(text.MinLength, text.MaxLength))
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "String length bounds are contradictory.", path);
            }

            if (TryCandidate(scalar, constraints, candidates, out SemanticTestValue? supplied))
            {
                return supplied;
            }
            if (customCandidate)
            {
                return Error("TESTDATA_CUSTOM_CANDIDATE_INVALID", "A custom generator supplied a candidate that violates the canonical scalar or constraint contract.", path);
            }

            var length = Clamp(ProfileTarget(), constraints.String?.MinLength ?? 0, constraints.String?.MaxLength, _budgets.MaxStringLength);
            object value = scalar.ScalarKind switch
            {
                ScalarKind.Boolean => _random.Next(2) == 0,
                ScalarKind.String => StringValue(format, length),
                ScalarKind.Integer => NumericValue(scalar, constraints.Numeric, false),
                ScalarKind.Number or ScalarKind.Decimal => NumericValue(scalar, constraints.Numeric, true),
                ScalarKind.Date => new DateOnly(2020, 1, 1),
                ScalarKind.Time => new TimeOnly(12, 0),
                ScalarKind.DateTime => new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Unspecified),
                ScalarKind.DateTimeOffset => new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.Zero),
                ScalarKind.Duration => TimeSpan.FromMinutes(1),
                ScalarKind.Guid => Guid.Parse("00000000-0000-4000-8000-000000000001"),
                ScalarKind.Binary => Enumerable.Repeat((byte)0x42, Clamp(ProfileTarget(), 0, null, _budgets.MaxBinaryLength)).ToArray(),
                ScalarKind.Json => "{}",
                ScalarKind.Unknown => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException()
            };
            if (scalar.Format is not null && value is string formatted)
            {
                if (formatted.Length < (constraints.String?.MinLength ?? 0) || formatted.Length > (constraints.String?.MaxLength ?? int.MaxValue))
                {
                    return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "The predefined format cannot satisfy the string length constraints.", path);
                }
            }

            return new ScalarTestValue(scalar.Id, scalar.ScalarKind, value);
        }

        private bool TryCandidate(ScalarTypeDefinition scalar, ConstraintSet constraints, IReadOnlyList<JsonElement>? candidates, out SemanticTestValue? result)
        {
            result = null;
            if (candidates is null || candidates.Count == 0)
            {
                return false;
            }
            var eligible = new List<(JsonElement Json, object Value)>();
            foreach (JsonElement candidate in candidates)
            {
                if (TerminologyCandidate.TryRead(candidate, scalar, constraints, out var value, out _))
                {
                    eligible.Add((candidate, value!));
                }
            }
            if (eligible.Count == 0)
            {
                return false;
            }
            (JsonElement Json, object Value) selected = eligible[_random.Next(eligible.Count)];
            if (scalar.ScalarKind is ScalarKind.String or ScalarKind.Binary)
            {
                var target = ProfileTarget();
                var nearest = eligible.Min(candidate => Math.Abs(CandidateLength(candidate.Json, scalar.ScalarKind) - target));
                eligible = [.. eligible.Where(candidate => Math.Abs(CandidateLength(candidate.Json, scalar.ScalarKind) - target) == nearest)];
                selected = eligible[_random.Next(eligible.Count)];
            }
            result = new ScalarTestValue(scalar.Id, scalar.ScalarKind, selected.Value);
            return true;
        }

        private static int CandidateLength(JsonElement candidate, ScalarKind kind)
        {
            return kind == ScalarKind.Binary ? candidate.GetBytesFromBase64().Length : candidate.GetString()!.Length;
        }

        private decimal NumericValue(ScalarTypeDefinition scalar, NumericConstraints? constraints, bool fractional)
        {
            var value = constraints?.Minimum ?? (constraints?.Maximum is decimal upper && upper < 0 ? upper : 0m);
            if (constraints?.ExclusiveMinimum == true)
            {
                value += fractional ? 0.01m : 1m;
            }

            if (constraints?.Minimum is decimal minimum && constraints.Maximum is decimal maximum && maximum >= minimum)
            {
                value = minimum + ((decimal)_random.NextDouble() * (maximum - minimum));
            }

            if (constraints?.MultipleOf is decimal multiple && multiple > 0)
            {
                value = Math.Ceiling(value / multiple) * multiple;
                if (constraints.ExclusiveMaximum && constraints.Maximum == value)
                {
                    value -= multiple;
                }
            }
            if (constraints?.Maximum is decimal max && constraints.ExclusiveMaximum && value >= max)
            {
                value = max - (fractional ? 0.01m : 1m);
            }

            if (constraints?.Maximum is decimal upperBound && value > upperBound)
            {
                value = upperBound;
            }
            return scalar.ScalarKind == ScalarKind.Integer ? decimal.Truncate(value) : value;
        }

        private static string StringValue(string? format, int length)
        {
            var value = format?.ToLowerInvariant() switch
            {
                "email" => "user@example.com",
                "uri" or "uri-reference" => "https://example.com/resource",
                "hostname" => "example.com",
                "ipv4" => "192.0.2.1",
                "ipv6" => "2001:db8::1",
                "date" => "2020-01-01",
                "time" => "12:00:00Z",
                "date-time" => "2020-01-01T12:00:00Z",
                "duration" => "PT1M",
                "uuid" => "00000000-0000-4000-8000-000000000001",
                _ => "test"
            };
            if (length < value.Length)
            {
                value = value[..length];
            }

            if (value.Length < length)
            {
                value += new string('x', length - value.Length);
            }

            return value;
        }

        private static bool SupportedFormat(string format)
        {
            return format.ToLowerInvariant() is "email" or "uri" or "uri-reference" or "hostname" or "ipv4" or "ipv6" or "date" or "time" or "date-time" or "duration" or "uuid";
        }

        private int ProfileTarget()
        {
            return profile switch
            {
                TestDataSizeProfile.Simple => 1,
                TestDataSizeProfile.Moderate => 8,
                TestDataSizeProfile.Extreme => 100,
                _ => 1
            };
        }

        private static int Target(int? min, int? max, int target)
        {
            return Clamp(target, min ?? 0, max, 10000);
        }

        private static int Clamp(int value, int min, int? max, int ceiling)
        {
            return Math.Min(ceiling, Math.Max(min, max is null ? value : Math.Min(value, max.Value)));
        }

        private static bool RangeValid(int? min, int? max)
        {
            return min is null || max is null || min <= max;
        }

        private static int? Max(int? left, int? right)
        {
            return left is null ? right : right is null ? left : Math.Max(left.Value, right.Value);
        }

        private static int? Min(int? left, int? right)
        {
            return left is null ? right : right is null ? left : Math.Min(left.Value, right.Value);
        }

        private static bool NumericRangeValid(NumericConstraints n)
        {
            return (n.Minimum is null || n.Maximum is null || n.Minimum <= n.Maximum) && !(n.ExclusiveMinimum && n.ExclusiveMaximum && n.Minimum == n.Maximum);
        }

        private static ArrayConstraints MergeArray(int? min, int? max, bool unique, ArrayConstraints? property, ConstraintSet all)
        {
            return new() { MinItems = new[] { min, property?.MinItems, all.Array?.MinItems }.Where(static x => x.HasValue).Select(static x => x!.Value).DefaultIfEmpty().Max(), MaxItems = new[] { max, property?.MaxItems, all.Array?.MaxItems }.Where(static x => x.HasValue).Select(static x => x!.Value).DefaultIfEmpty().Min(), UniqueItems = unique || property?.UniqueItems == true || all.Array?.UniqueItems == true };
        }

        private static string Fingerprint(SemanticTestValue value)
        {
            return value switch { ScalarTestValue s => $"{s.ScalarKind}:{s.Value}", EnumTestValue e => $"enum:{e.Value}", NullTestValue => "null", _ => value.ToString() ?? string.Empty };
        }

        private SemanticTestValue? Error(string code, string message, string path) { Diagnostics.Add(new() { Severity = SchemaDiagnosticSeverity.Error, Code = code, Message = message, Stage = SchemaDiagnosticStage.Validation, ModelPath = path, PipelineStage = "TestData" }); return null; }
        private SemanticTestValue? CustomError(string path)
        {
            return Error("TESTDATA_CUSTOM_CONSTRAINT_UNSUPPORTED", "Custom constraints require custom handling.", path);
        }
    }
}

#pragma warning restore CS1591, IDE0046, IDE0060
