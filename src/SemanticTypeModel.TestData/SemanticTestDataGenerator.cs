using System.Globalization;
using System.Text;
using System.Text.Json;
#pragma warning disable CS1591, IDE0007, IDE0011, IDE0022, IDE0046, IDE0048, IDE0060
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
    private static class Entropy
    {
        internal static ulong UInt64(int seed, int ordinal, string coordinate)
        {
            var hash = 14695981039346656037UL;
            Add(ref hash, seed.ToString(CultureInfo.InvariantCulture));
            Add(ref hash, ordinal.ToString(CultureInfo.InvariantCulture));
            Add(ref hash, coordinate);
            hash += 0x9E3779B97F4A7C15UL;
            hash = (hash ^ (hash >> 30)) * 0xBF58476D1CE4E5B9UL;
            hash = (hash ^ (hash >> 27)) * 0x94D049BB133111EBUL;
            return hash ^ (hash >> 31);
        }

        internal static int Index(int seed, int ordinal, string coordinate, int count) { return count == 0 ? 0 : (int)(UInt64(seed, ordinal, coordinate) % (uint)count); }
        internal static decimal Fraction(int seed, int ordinal, string coordinate) { return UInt64(seed, ordinal, coordinate) % 1_000_001UL / 1_000_000m; }
        internal static int Int(int seed, int ordinal, string coordinate, int count) { return Index(seed, ordinal, coordinate, count); }
        internal static string Token(int seed, int ordinal, string coordinate, int length = 16)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                _ = builder.Append(alphabet[(int)(UInt64(seed, ordinal + i, coordinate) % (uint)alphabet.Length)]);
            }
            return builder.ToString();
        }

        internal static Guid Guid(int seed, int ordinal, string coordinate)
        {
            var bytes = Bytes(seed, ordinal, coordinate, 16);
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            return new Guid(bytes);
        }

        internal static byte[] Bytes(int seed, int ordinal, string coordinate, int length)
        {
            var bytes = new byte[length];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(UInt64(seed, ordinal, coordinate + "/byte:" + i) & 0xFF);
            }
            return bytes;
        }

        private static void Add(ref ulong hash, string text)
        {
            foreach (var value in Encoding.UTF8.GetBytes(text))
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }
            hash ^= 0xFF;
            hash *= 1099511628211UL;
        }
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, TestDataSizeProfile profile = TestDataSizeProfile.Simple, int seed = 0)
    {
        return Generate(model, rootType, profile, seed, null, null);
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology)
    {
        ArgumentNullException.ThrowIfNull(terminology);
        return Generate(model, rootType, profile, seed, SemanticTerminologyProfileJson.ValidateForConsumption(model, terminology), null);
    }

    internal static TestDataGenerationResult Generate(TypeSchemaModel model, TypeId rootType, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology, SemanticTestDataOptions? options)
    {
        ArgumentNullException.ThrowIfNull(model);
        options?.Budgets.Validate();
        if (options?.Profile is not null && options.Profile.ModelId != model.Id)
            return new TestDataGenerationResult { Diagnostics = [new SchemaDiagnostic { Severity = SchemaDiagnosticSeverity.Error, Code = "TESTDATA_POLICY_MODEL_MISMATCH", Message = "The TestData Profile is bound to a different canonical model.", Stage = SchemaDiagnosticStage.Validation, ModelPath = ModelPath.ForType(rootType), PipelineStage = "TestData" }] };
        GenerationSession session = options?.Session ?? new GenerationSession();
        session.BeginRoot();
        var context = new Context(model, profile, seed, terminology, options, session);
        SemanticTestValue? value = context.Generate(rootType, new ConstraintSet(), ModelPath.ForType(rootType), "root", false, false, 0, []);
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
        return Generate(model, rootType, profile, seed, SemanticTerminologyProfileJson.ValidateForConsumption(model, terminology), null);
    }

    public static TestDataGenerationResult Generate(TypeSchemaModel model, string rootTypeId, TestDataSizeProfile profile = TestDataSizeProfile.Simple, int seed = 0)
    {
        return Generate(model, new TypeId(rootTypeId), profile, seed);
    }

    private sealed class Context(TypeSchemaModel model, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology, SemanticTestDataOptions? options, GenerationSession session)
    {
        private readonly int _seed = seed;
        private readonly int _rootOrdinal = options?.RootOrdinal ?? 0;
        private readonly TestDataBudgets _budgets = options?.Budgets ?? new();
        private readonly TestDataProfile? _testDataProfile = options?.Profile;
        private readonly GenerationSession _session = session;
        private int _nodes;
        internal List<SchemaDiagnostic> Diagnostics { get; } = [];

        internal SemanticTestValue? Generate(TypeId id, ConstraintSet useConstraints, string path, string coordinate, bool allowsNull, bool optional, int depth, HashSet<TypeId> ancestors, IReadOnlyList<JsonElement>? candidates = null, bool customCandidate = false, IReadOnlyList<JsonElement>? fallbackCandidates = null, TestDataPolicy? policy = null, bool coordinatedCandidate = false)
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
                ScalarTypeDefinition scalar => GenerateScalar(scalar, useConstraints, path, coordinate, candidates, customCandidate, fallbackCandidates, policy, coordinatedCandidate),
                EnumTypeDefinition @enum => GenerateEnum(@enum, path, coordinate, candidates, fallbackCandidates, customCandidate, coordinatedCandidate),
                ObjectTypeDefinition obj => GenerateObject(obj, useConstraints, path, coordinate, next, depth),
                ArrayTypeDefinition array => GenerateArray(array, useConstraints, path, coordinate, next, depth, policy),
                DictionaryTypeDefinition dictionary => GenerateDictionary(dictionary, useConstraints, path, coordinate, next, depth, policy),
                ReferenceTypeDefinition reference => GenerateReference(reference, useConstraints, path, coordinate, allowsNull, optional, depth, next, candidates, customCandidate, fallbackCandidates, policy),
                UnionTypeDefinition => Error("TESTDATA_UNSUPPORTED_TYPE", "Union generation is unsupported.", path),
                IntersectionTypeDefinition => Error("TESTDATA_UNSUPPORTED_TYPE", "Intersection generation is unsupported.", path),
                _ when type.Kind == TypeKind.Any => new ScalarTestValue(id, ScalarKind.Json, JsonValue(coordinate)),
                _ when type.Kind == TypeKind.Never => Error("TESTDATA_UNSUPPORTED_TYPE", "Never has no valid generated value.", path),
                _ => Error("TESTDATA_UNSUPPORTED_TYPE", $"Type kind '{type.Kind}' is unsupported.", path)
            };
        }

        private SemanticTestValue? GenerateReference(ReferenceTypeDefinition reference, ConstraintSet constraints, string path, string coordinate, bool allowsNull, bool optional, int depth, HashSet<TypeId> ancestors, IReadOnlyList<JsonElement>? candidates, bool customCandidate, IReadOnlyList<JsonElement>? fallbackCandidates, TestDataPolicy? policy)
        {
            return Generate(reference.Target.Id, constraints, path, coordinate + "/reference", allowsNull, optional, depth + 1, ancestors, candidates, customCandidate, fallbackCandidates, policy);
        }

        private SemanticTestValue? GenerateEnum(EnumTypeDefinition @enum, string path, string coordinate, IReadOnlyList<JsonElement>? candidates, IReadOnlyList<JsonElement>? fallbackCandidates, bool customCandidate, bool coordinatedCandidate)
        {
            if (@enum.Values.Count == 0)
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Enum has no usable declared value.", path);
            }

            if (TryEnumCandidates(@enum, candidates, coordinate, out EnumTestValue? supplied)
                || TryEnumCandidates(@enum, fallbackCandidates, coordinate + "/fallback", out supplied))
            {
                return supplied;
            }
            if (candidates is { Count: > 0 } && (customCandidate || coordinatedCandidate))
            {
                return Error(coordinatedCandidate ? "TESTDATA_COORDINATION_CANDIDATE_INVALID" : "TESTDATA_CUSTOM_CANDIDATE_INVALID", "The supplied enum candidate is not a declared enum value.", path);
            }

            EnumValueDefinition value = @enum.Values[Entropy.Index(_seed, _rootOrdinal, coordinate, @enum.Values.Count)];
            return new EnumTestValue(@enum.Id, value.Value);
        }

        private bool TryEnumCandidates(EnumTypeDefinition @enum, IReadOnlyList<JsonElement>? candidates, string coordinate, out EnumTestValue? result)
        {
            result = null;
            if (candidates is not { Count: > 0 }) return false;
            JsonElement[] legal = [.. candidates.Where(candidate => @enum.Values.Any(value => EnumCandidateMatches(candidate, value)))];
            if (legal.Length == 0) return false;
            EnumValueDefinition selected = @enum.Values.First(value => EnumCandidateMatches(legal[Entropy.Index(_seed, _rootOrdinal, coordinate + "/enum-candidate", legal.Length)], value));
            result = new EnumTestValue(@enum.Id, selected.Value);
            return true;
        }

        private static bool EnumCandidateMatches(JsonElement candidate, EnumValueDefinition value)
        {
            string? text = candidate.ValueKind == JsonValueKind.String ? candidate.GetString() : null;
            return candidate.GetRawText() == JsonSerializer.Serialize(value.Value)
                || string.Equals(text, value.Name, StringComparison.Ordinal)
                || string.Equals(text, Convert.ToString(value.Value, CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        private SemanticTestValue? GenerateObject(ObjectTypeDefinition obj, ConstraintSet constraints, string path, string coordinate, HashSet<TypeId> ancestors, int depth)
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
            foreach ((ObjectTypeDefinition owner, PropertyDefinition property) in OrderedProperties(obj, properties))
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
                var propertyCoordinate = coordinate + "/property:" + owner.Id.Value + ":" + property.Id.Value;
                TestDataPolicy effectivePolicy = _testDataProfile?.Resolve(obj.Id, owner.Id, property) ?? TestDataPolicy.Empty;
                if (!property.Cardinality.IsRequired && effectivePolicy.OptionalPresence is double presence && Entropy.Fraction(_seed, _rootOrdinal, propertyCoordinate + "/presence") > (decimal)presence)
                    continue;
                if (property.Cardinality.AllowsNull && effectivePolicy.NullProbability is double nullProbability && Entropy.Fraction(_seed, _rootOrdinal, propertyCoordinate + "/null") <= (decimal)nullProbability)
                {
                    result[property.Id] = new NullTestValue(property.Type.Id);
                    continue;
                }
                if (effectivePolicy.Coordination is { Shared: true, Scope: { } existingScope }
                    && _session.TryGetShared((owner.Id, property.Id), existingScope, out SemanticTestValue existingShared))
                {
                    result[property.Id] = existingShared;
                    continue;
                }
                TestDataGeneratorContext callbackContext = new(model, propertyType, property, logicalType, profile, unchecked((int)Entropy.UInt64(_seed, _rootOrdinal, propertyCoordinate)), _rootOrdinal);
                var customValue = options?.PropertyGenerator?.Invoke(owner, property, callbackContext);
                customValue ??= logicalType is null ? null : options?.LogicalTypeGenerator?.Invoke(logicalType, callbackContext);
                TestDataPolicy coordinatedPolicy = effectivePolicy;
                object? coordinatedValue = customValue is null ? ResolveCoordinatedValue(owner, property, coordinatedPolicy, result) : null;
                if (coordinatedValue is CoordinationFailure coordinationFailure)
                {
                    return Error(coordinationFailure.Code, coordinationFailure.Message, propertyPath);
                }
                customValue ??= coordinatedValue;
                var customCandidate = customValue is not null;
                var coordinatedCandidate = coordinatedValue is not null;
                (IReadOnlyList<JsonElement>? propertyCandidates, IReadOnlyList<JsonElement>? logicalCandidates) = terminology?.FindCandidateSources(owner, property) ?? ([], []);
                IReadOnlyList<JsonElement>? profilePropertyCandidates = ExpandWeighted(effectivePolicy.WeightedValues);
                IReadOnlyList<JsonElement>? profileLogicalCandidates = logicalType is null ? null : ExpandWeighted(_testDataProfile?.ResolveLogical(logicalType)?.WeightedValues);
                IReadOnlyList<JsonElement>? candidates = customValue is null ? profilePropertyCandidates ?? profileLogicalCandidates ?? propertyCandidates : [JsonSerializer.SerializeToElement(customValue)];
                IReadOnlyList<JsonElement>? fallbackCandidates = customValue is null && profilePropertyCandidates is null && profileLogicalCandidates is null ? logicalCandidates : null;
                SemanticTestValue? value = Generate(property.Type.Id, propertyConstraints, propertyPath, propertyCoordinate, property.Cardinality.AllowsNull, !property.Cardinality.IsRequired, depth + 1, ancestors, candidates, customCandidate, fallbackCandidates, effectivePolicy, coordinatedCandidate);
                if (value is null)
                {
                    if (property.Cardinality.IsRequired)
                    {
                        return Error("TESTDATA_RECURSION_UNTERMINATED", "Required property could not be finitely generated.", propertyPath);
                    }

                    continue;
                }
                if (coordinatedPolicy.Coordination is { Unique: true, Scope: { } retryScope } && value is not NullTestValue)
                {
                    var attempt = 0;
                    bool uniqueAccepted = _session.AddUnique((owner.Id, property.Id), retryScope, Fingerprint(value));
                    while (!uniqueAccepted && attempt++ < 100)
                    {
                        if (customCandidate)
                            return Error("TESTDATA_COORDINATION_UNIQUENESS_EXHAUSTED", "An explicit coordinated or programmatic value duplicated within its scope.", propertyPath);
                        value = Generate(property.Type.Id, propertyConstraints, propertyPath, propertyCoordinate + "/unique-attempt:" + attempt.ToString(CultureInfo.InvariantCulture), property.Cardinality.AllowsNull, !property.Cardinality.IsRequired, depth + 1, ancestors, candidates, false, fallbackCandidates, effectivePolicy);
                        if (value is null) break;
                        uniqueAccepted = _session.AddUnique((owner.Id, property.Id), retryScope, Fingerprint(value));
                    }
                    if (value is null || value is NullTestValue || !uniqueAccepted)
                        return Error("TESTDATA_COORDINATION_UNIQUENESS_EXHAUSTED", "Scoped TestData uniqueness was exhausted by a duplicate value.", propertyPath);
                }
                if (coordinatedPolicy.Coordination is { Shared: true, Scope: { } sharedScope } shared && _session.TryGetShared((owner.Id, property.Id), sharedScope, out SemanticTestValue sharedValue))
                    value = sharedValue;
                else if (coordinatedPolicy.Coordination is { Shared: true, Scope: { } storeScope } && value is not NullTestValue)
                    _session.SetShared((owner.Id, property.Id), storeScope, value);
                result[property.Id] = value;
            }
            return new ObjectTestValue(obj.Id, result);
        }

        private List<(ObjectTypeDefinition Owner, PropertyDefinition Property)> OrderedProperties(ObjectTypeDefinition obj, IReadOnlyList<(ObjectTypeDefinition Owner, PropertyDefinition Property)> properties)
        {
            var byId = properties.ToDictionary(static p => p.Property.Id);
            var ordered = new List<(ObjectTypeDefinition, PropertyDefinition)>();
            var visiting = new HashSet<PropertyId>();
            var visited = new HashSet<PropertyId>();
            void Visit((ObjectTypeDefinition Owner, PropertyDefinition Property) item)
            {
                if (visited.Contains(item.Property.Id)) return;
                if (!visiting.Add(item.Property.Id)) throw new InvalidOperationException("Coordinated TestData dependency cycle detected.");
                TestDataPolicy policy = _testDataProfile?.Resolve(obj.Id, item.Owner.Id, item.Property) ?? TestDataPolicy.Empty;
                foreach (PropertyId dependency in policy.Coordination?.Dependencies ?? [])
                    if (byId.TryGetValue(dependency, out (ObjectTypeDefinition Owner, PropertyDefinition Property) dependencyItem)) Visit(dependencyItem);
                _ = visiting.Remove(item.Property.Id); _ = visited.Add(item.Property.Id); ordered.Add(item);
            }
            foreach ((ObjectTypeDefinition Owner, PropertyDefinition Property) item in properties) Visit(item);
            return ordered;
        }

        private object? ResolveCoordinatedValue(ObjectTypeDefinition owner, PropertyDefinition property, TestDataPolicy policy, IReadOnlyDictionary<PropertyId, SemanticTestValue> values)
        {
            TestDataCoordination? coordination = policy.Coordination;
            if (coordination is null) return null;
            (TypeId Owner, PropertyId Property) key = (owner.Id, property.Id);
            if (coordination.Sequence is not null && coordination.Scope is { } sequenceScope)
            {
                int index = _session.NextSequence(key, sequenceScope);
                try { return coordination.Sequence(index) ?? new CoordinationFailure("TESTDATA_COORDINATION_CANDIDATE_INVALID", "A sequence callback must return a non-null candidate."); }
                catch (TestDataCoordinationException exception) { return new CoordinationFailure(exception.Code, exception.Message); }
                catch (Exception exception) { return new CoordinationFailure("TESTDATA_COORDINATION_CALLBACK_FAILED", exception.Message); }
            }
            if (coordination.Derived is not null)
            {
                var declared = (coordination.Dependencies ?? []).ToHashSet();
                try { return coordination.Derived(new TestDataDependencyContext(declared, values, _rootOrdinal)) ?? new CoordinationFailure("TESTDATA_COORDINATION_CANDIDATE_INVALID", "A derived callback must return a non-null candidate."); }
                catch (TestDataCoordinationException exception) { return new CoordinationFailure(exception.Code, exception.Message); }
                catch (Exception exception) { return new CoordinationFailure("TESTDATA_COORDINATION_CALLBACK_FAILED", exception.Message); }
            }
            return null;
        }

        private sealed record CoordinationFailure(string Code, string Message);

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

        private SemanticTestValue? GenerateArray(ArrayTypeDefinition array, ConstraintSet useConstraints, string path, string coordinate, HashSet<TypeId> ancestors, int depth, TestDataPolicy? policy)
        {
            ArrayConstraints effective = MergeArray(array.MinItems, array.MaxItems, array.UniqueItems, useConstraints.Array, useConstraints);
            if (!RangeValid(effective.MinItems, effective.MaxItems))
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Array item bounds are contradictory.", path);
            }

            var count = policy?.CollectionSize is { } size ? Count(size, effective.MinItems, effective.MaxItems, coordinate) : Target(effective.MinItems, effective.MaxItems, CollectionProfileTarget());
            if (policy?.ValueStrategy is TestDataValueStrategy.Boundary or TestDataValueStrategy.BoundaryMixed && policy.CollectionSize is null)
                count = BoundaryCount(effective.MinItems, effective.MaxItems, count, coordinate, policy.ValueStrategy == TestDataValueStrategy.BoundaryMixed);
            if (count > _budgets.MaxCollectionItems || effective.MinItems > _budgets.MaxCollectionItems)
            {
                return Error("TESTDATA_SIZE_BUDGET_EXHAUSTED", "Array generation exceeds the fixed item safety budget.", path);
            }

            var items = new List<SemanticTestValue>();
            var fingerprints = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                var itemCoordinate = coordinate + "/item:" + i.ToString(CultureInfo.InvariantCulture);
                SemanticTestValue? item = Generate(array.ItemType.Id, new ConstraintSet(), path + "/items/" + i.ToString(CultureInfo.InvariantCulture), itemCoordinate, false, false, depth + 1, ancestors);
                if (item is null)
                {
                    return null;
                }

                if (effective.UniqueItems && !fingerprints.Add(Fingerprint(item)))
                {
                    var attempts = 0;
                    while (item is not null && attempts++ < 100 && fingerprints.Contains(Fingerprint(item)))
                    {
                        item = Generate(array.ItemType.Id, new ConstraintSet(), path + "/items/" + i.ToString(CultureInfo.InvariantCulture), itemCoordinate + "/attempt:" + attempts, false, false, depth + 1, ancestors);
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

        private SemanticTestValue? GenerateDictionary(DictionaryTypeDefinition dictionary, ConstraintSet useConstraints, string path, string coordinate, HashSet<TypeId> ancestors, int depth, TestDataPolicy? policy)
        {
            ArrayConstraints? constraints = useConstraints.Array;
            if (!RangeValid(constraints?.MinItems, constraints?.MaxItems))
            {
                return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "Dictionary entry bounds are contradictory.", path);
            }

            var count = policy?.CollectionSize is { } size ? Count(size, constraints?.MinItems, constraints?.MaxItems, coordinate) : Target(constraints?.MinItems, constraints?.MaxItems, CollectionProfileTarget());
            if (policy?.ValueStrategy is TestDataValueStrategy.Boundary or TestDataValueStrategy.BoundaryMixed && policy.CollectionSize is null)
                count = BoundaryCount(constraints?.MinItems, constraints?.MaxItems, count, coordinate, policy.ValueStrategy == TestDataValueStrategy.BoundaryMixed);
            if (count > _budgets.MaxDictionaryEntries)
            {
                return Error("TESTDATA_SIZE_BUDGET_EXHAUSTED", "Dictionary generation exceeds the fixed entry safety budget.", path);
            }

            var entries = new List<KeyValuePair<SemanticTestValue, SemanticTestValue>>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                SemanticTestValue? key = Generate(dictionary.KeyType.Id, new ConstraintSet(), path + "/keys/" + i, coordinate + "/entry:" + i + "/key", false, false, depth + 1, ancestors);
                SemanticTestValue? value = Generate(dictionary.ValueType.Id, new ConstraintSet(), path + "/values/" + i, coordinate + "/entry:" + i + "/value", false, false, depth + 1, ancestors);
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

        private SemanticTestValue? GenerateScalar(ScalarTypeDefinition scalar, ConstraintSet constraints, string path, string coordinate, IReadOnlyList<JsonElement>? candidates = null, bool customCandidate = false, IReadOnlyList<JsonElement>? fallbackCandidates = null, TestDataPolicy? policy = null, bool coordinatedCandidate = false)
        {
            if (constraints.Custom.Count > 0)
            {
                return CustomError(path);
            }

            if (constraints.String?.Pattern is { Length: > 0 })
            {
                if (TryCandidate(scalar, constraints, candidates, coordinate, out SemanticTestValue? candidate)
                    || TryCandidate(scalar, constraints, fallbackCandidates, coordinate, out candidate))
                {
                    return candidate;
                }
                if (customCandidate)
                {
                    return Error(coordinatedCandidate ? "TESTDATA_COORDINATION_CANDIDATE_INVALID" : "TESTDATA_CUSTOM_CANDIDATE_INVALID", coordinatedCandidate ? "A coordinated candidate violates the canonical scalar contract." : "A custom generator supplied a candidate that violates the canonical pattern or scalar contract.", path);
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

            if (TryCandidate(scalar, constraints, candidates, coordinate, out SemanticTestValue? supplied)
                || TryCandidate(scalar, constraints, fallbackCandidates, coordinate, out supplied))
            {
                return supplied;
            }
            if (customCandidate)
            {
                return Error(coordinatedCandidate ? "TESTDATA_COORDINATION_CANDIDATE_INVALID" : "TESTDATA_CUSTOM_CANDIDATE_INVALID", coordinatedCandidate ? "A coordinated candidate violates the canonical scalar or constraint contract." : "A custom generator supplied a candidate that violates the canonical scalar or constraint contract.", path);
            }

            if (scalar.ScalarKind is ScalarKind.String or ScalarKind.Binary
                && constraints.String?.MinLength is int semanticMinimum
                && semanticMinimum > (scalar.ScalarKind == ScalarKind.Binary ? _budgets.MaxBinaryLength : _budgets.MaxStringLength))
            {
                return Error("TESTDATA_SIZE_BUDGET_EXHAUSTED", "The semantic minimum exceeds the applicable TestData safety budget.", path);
            }

            var length = Clamp(StringProfileTarget(), constraints.String?.MinLength ?? 0, constraints.String?.MaxLength, _budgets.MaxStringLength);
            var formattedValue = scalar.ScalarKind == ScalarKind.String && format is not null ? StringValue(format, coordinate, constraints.String?.MinLength) : null;
            if (formattedValue is not null)
            {
                if (formattedValue.Length < (constraints.String?.MinLength ?? 0) || formattedValue.Length > (constraints.String?.MaxLength ?? int.MaxValue))
                {
                    return Error("TESTDATA_UNSATISFIABLE_CONSTRAINTS", "The predefined format cannot satisfy the string length constraints.", path);
                }
                if (formattedValue.Length > _budgets.MaxStringLength)
                {
                    return Error("TESTDATA_SIZE_BUDGET_EXHAUSTED", "The predefined format exceeds the fixed string safety budget.", path);
                }
            }
            bool boundary = policy?.ValueStrategy == TestDataValueStrategy.Boundary || policy?.ValueStrategy == TestDataValueStrategy.BoundaryMixed && Entropy.Fraction(_seed, _rootOrdinal, coordinate + "/boundary") < 0.2m;
            object value = scalar.ScalarKind switch
            {
                ScalarKind.Boolean => Entropy.UInt64(_seed, _rootOrdinal, coordinate) % 2 == 0,
                ScalarKind.String => formattedValue ?? StringValue(null, coordinate, boundary ? BoundaryLength(constraints.String, length) : length),
                ScalarKind.Integer => NumericValue(scalar, constraints.Numeric, false, coordinate, boundary),
                ScalarKind.Number or ScalarKind.Decimal => NumericValue(scalar, constraints.Numeric, true, coordinate, boundary),
                ScalarKind.Date => DateValue(coordinate),
                ScalarKind.Time => TimeValue(coordinate),
                ScalarKind.DateTime => new DateTime(DateValue(coordinate), TimeValue(coordinate), DateTimeKind.Unspecified),
                ScalarKind.DateTimeOffset => new DateTimeOffset(DateValue(coordinate), TimeValue(coordinate), TimeSpan.Zero),
                ScalarKind.Duration => DurationValue(coordinate),
                ScalarKind.Guid => Entropy.Guid(_seed, _rootOrdinal, coordinate),
                ScalarKind.Binary => Entropy.Bytes(_seed, _rootOrdinal, coordinate, Clamp(boundary ? BoundaryLength(constraints.String, length) : length, constraints.String?.MinLength ?? 0, constraints.String?.MaxLength, _budgets.MaxBinaryLength)),
                ScalarKind.Json => JsonValue(coordinate),
                ScalarKind.Unknown => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException()
            };
            return new ScalarTestValue(scalar.Id, scalar.ScalarKind, value);
        }

        private bool TryCandidate(ScalarTypeDefinition scalar, ConstraintSet constraints, IReadOnlyList<JsonElement>? candidates, string coordinate, out SemanticTestValue? result)
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
                    if (value is string text && text.Length > _budgets.MaxStringLength)
                    {
                        continue;
                    }
                    if (value is byte[] bytes && bytes.Length > _budgets.MaxBinaryLength)
                    {
                        continue;
                    }
                    eligible.Add((candidate, value!));
                }
            }
            if (eligible.Count == 0)
            {
                return false;
            }
            (JsonElement Json, object Value) selected = eligible[Entropy.Index(_seed, _rootOrdinal, coordinate, eligible.Count)];
            if (scalar.ScalarKind is ScalarKind.String or ScalarKind.Binary)
            {
                var target = StringProfileTarget();
                var nearest = eligible.Min(candidate => Math.Abs(CandidateLength(candidate.Json, scalar.ScalarKind) - target));
                eligible = [.. eligible.Where(candidate => Math.Abs(CandidateLength(candidate.Json, scalar.ScalarKind) - target) == nearest)];
                selected = eligible[Entropy.Index(_seed, _rootOrdinal, coordinate + "/candidate", eligible.Count)];
            }
            result = new ScalarTestValue(scalar.Id, scalar.ScalarKind, selected.Value);
            return true;
        }

        private static List<JsonElement>? ExpandWeighted(IReadOnlyList<TestDataWeightedCandidate>? candidates)
        {
            if (candidates is null) return null;
            var expanded = new List<JsonElement>();
            foreach (TestDataWeightedCandidate candidate in candidates)
            {
                for (var i = 0; i < candidate.Weight && expanded.Count < 10_000; i++) expanded.Add(candidate.Value);
            }
            return expanded;
        }

        private static int CandidateLength(JsonElement candidate, ScalarKind kind)
        {
            return kind == ScalarKind.Binary ? candidate.GetBytesFromBase64().Length : candidate.GetString()!.Length;
        }

        private decimal NumericValue(ScalarTypeDefinition scalar, NumericConstraints? constraints, bool fractional, string coordinate, bool boundary = false)
        {
            var lower = constraints?.Minimum ?? -10_000m;
            var upper = constraints?.Maximum ?? 10_000m;
            lower = Math.Max(lower, -10_000m);
            upper = Math.Min(upper, 10_000m);
            var value = lower + (Entropy.Fraction(_seed, _rootOrdinal, coordinate) * (upper - lower));
            if (boundary && constraints is { } boundaryConstraints && (boundaryConstraints.Minimum is not null || boundaryConstraints.Maximum is not null))
            {
                bool chooseUpper = boundaryConstraints.Maximum is not null && (boundaryConstraints.Minimum is null || Entropy.Index(_seed, _rootOrdinal, coordinate + "/boundary-value", 2) == 1);
                value = chooseUpper ? boundaryConstraints.Maximum!.Value : boundaryConstraints.Minimum!.Value;
                if (chooseUpper && boundaryConstraints.ExclusiveMaximum) value -= fractional ? 0.01m : 1m;
                if (!chooseUpper && boundaryConstraints.ExclusiveMinimum) value += fractional ? 0.01m : 1m;
            }
            if (constraints?.Minimum is null && constraints?.Maximum is null && fractional)
            {
                value = Math.Round(value, 2, MidpointRounding.ToEven);
            }
            if (constraints?.ExclusiveMinimum == true)
            {
                value += fractional ? 0.01m : 1m;
            }

            if (constraints?.Minimum is decimal minimum && constraints.Maximum is decimal maximum && maximum >= minimum)
            {
                value = minimum + (Entropy.Fraction(_seed, _rootOrdinal, coordinate) * (maximum - minimum));
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

        private string StringValue(string? format, string coordinate, int? minimumLength = null)
        {
            var token = Entropy.Token(_seed, _rootOrdinal, coordinate);
            var value = format?.ToLowerInvariant() switch
            {
                "email" => "u" + token + "@example.test",
                "uri" => "https://example.test/resource/" + token,
                "uri-reference" => "resource/" + token,
                "hostname" => "h" + token + ".example.test",
                "ipv4" => "192.0.2." + (1 + (Entropy.UInt64(_seed, _rootOrdinal, coordinate) % 254)),
                "ipv6" => "2001:db8::" + (1 + (Entropy.UInt64(_seed, _rootOrdinal, coordinate) % 65534)).ToString("x", CultureInfo.InvariantCulture),
                "date" => DateValue(coordinate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "time" => TimeValue(coordinate).ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "Z",
                "date-time" => DateValue(coordinate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T" + TimeValue(coordinate).ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "Z",
                "duration" => System.Xml.XmlConvert.ToString(DurationValue(coordinate)),
                "uuid" => Entropy.Guid(_seed, _rootOrdinal, coordinate).ToString(),
                _ => null
            };
            if (value is null)
            {
                return Entropy.Token(_seed, _rootOrdinal, coordinate, minimumLength ?? 8);
            }
            if (minimumLength is int minimum && value.Length < minimum && format is "email" or "uri" or "uri-reference" or "hostname")
            {
                value = format == "email" ? "u" + Entropy.Token(_seed, _rootOrdinal, coordinate, Math.Max(1, minimum - 13)) + "@example.test" : value + Entropy.Token(_seed, _rootOrdinal, coordinate + "/suffix", minimum - value.Length);
            }
            return value;
        }

        private static bool SupportedFormat(string format)
        {
            return format.ToLowerInvariant() is "email" or "uri" or "uri-reference" or "hostname" or "ipv4" or "ipv6" or "date" or "time" or "date-time" or "duration" or "uuid";
        }

        private int StringProfileTarget()
        {
            return profile switch
            {
                TestDataSizeProfile.Simple => 8,
                TestDataSizeProfile.Moderate => 32,
                TestDataSizeProfile.Extreme => 1024,
                _ => 8
            };
        }

        private int CollectionProfileTarget()
        {
            return profile switch
            {
                TestDataSizeProfile.Simple => 1,
                TestDataSizeProfile.Moderate => 8,
                TestDataSizeProfile.Extreme => 100,
                _ => 1
            };
        }

        private DateOnly DateValue(string coordinate)
        {
            return DateOnly.FromDayNumber(Entropy.Int(_seed, _rootOrdinal, coordinate, DateOnly.FromDateTime(new DateTime(2039, 12, 31)).DayNumber - DateOnly.FromDateTime(new DateTime(2000, 1, 1)).DayNumber + 1) + DateOnly.FromDateTime(new DateTime(2000, 1, 1)).DayNumber);
        }

        private TimeOnly TimeValue(string coordinate)
        {
            return TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(Entropy.UInt64(_seed, _rootOrdinal, coordinate) % 86_400));
        }

        private TimeSpan DurationValue(string coordinate)
        {
            return TimeSpan.FromTicks((long)(Entropy.UInt64(_seed, _rootOrdinal, coordinate) % 2_592_001UL) * TimeSpan.TicksPerSecond);
        }

        private string JsonValue(string coordinate)
        {
            return "{\"value\":\"" + Entropy.Token(_seed, _rootOrdinal, coordinate, 12) + "\"}";
        }

        private int Count(TestDataCollectionSizePolicy policy, int? min, int? max, string coordinate)
        {
            int lower = Math.Max(policy.Minimum, min ?? 0);
            int upper = Math.Min(policy.Maximum, max ?? _budgets.MaxCollectionItems);
            if (lower > upper) throw new TestDataGenerationException("Collection-size policy is outside the legal canonical range.", [new SchemaDiagnostic { Severity = SchemaDiagnosticSeverity.Error, Code = "TESTDATA_POLICY_COLLECTION_SIZE_INVALID", Message = "Collection-size policy has no legal intersection with the canonical bounds.", Stage = SchemaDiagnosticStage.Validation, ModelPath = coordinate, PipelineStage = "TestData" }]);
            return policy.IsFixed ? lower : lower + Entropy.Index(_seed, _rootOrdinal, coordinate + "/count", upper - lower + 1);
        }

        private int BoundaryCount(int? min, int? max, int random, string coordinate, bool mixed)
        {
            if (mixed && Entropy.Fraction(_seed, _rootOrdinal, coordinate + "/boundary") >= 0.2m) return random;
            return Entropy.Index(_seed, _rootOrdinal, coordinate + "/boundary-value", 2) == 0 ? min ?? random : max ?? random;
        }

        private static int BoundaryLength(StringConstraints? constraints, int fallback)
        {
            if (constraints?.MinLength is int minimum) return minimum;
            if (constraints?.MaxLength is int maximum) return maximum;
            return fallback;
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
            int?[] minimums = [min, property?.MinItems, all.Array?.MinItems];
            int?[] maximums = [max, property?.MaxItems, all.Array?.MaxItems];
            return new()
            {
                MinItems = minimums.Any(static value => value.HasValue) ? minimums.Max(static value => value ?? 0) : null,
                MaxItems = maximums.Any(static value => value.HasValue) ? maximums.Min(static value => value ?? int.MaxValue) : null,
                UniqueItems = unique || property?.UniqueItems == true || all.Array?.UniqueItems == true
            };
        }

        private static string Fingerprint(SemanticTestValue value)
        {
            return value switch
            {
                ScalarTestValue { Value: byte[] bytes } scalar => $"{scalar.ScalarKind}:b64:{Convert.ToBase64String(bytes)}",
                ScalarTestValue scalar => $"{scalar.ScalarKind}:{Convert.ToString(scalar.Value, CultureInfo.InvariantCulture)}",
                EnumTestValue @enum => $"enum:{Convert.ToString(@enum.Value, CultureInfo.InvariantCulture)}",
                NullTestValue => "null",
                _ => value.ToString() ?? string.Empty
            };
        }

        private SemanticTestValue? Error(string code, string message, string path) { Diagnostics.Add(new() { Severity = SchemaDiagnosticSeverity.Error, Code = code, Message = message, Stage = SchemaDiagnosticStage.Validation, ModelPath = path, PipelineStage = "TestData" }); return null; }
        private SemanticTestValue? CustomError(string path)
        {
            return Error("TESTDATA_CUSTOM_CONSTRAINT_UNSUPPORTED", "Custom constraints require custom handling.", path);
        }
    }
}

#pragma warning restore CS1591, IDE0007, IDE0022, IDE0046, IDE0060
