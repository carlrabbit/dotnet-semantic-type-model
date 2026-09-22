using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
#pragma warning disable CS1591, IDE0007, IDE0008, IDE0011, IDE0022, IDE0046, IDE0047, IDE0048, IDE0060, IDE0270, IDE0305, CA2208, CA2264

namespace SemanticTypeModel.TestData;

public enum TestDataValueStrategy
{
    Random,
    Boundary,
    BoundaryMixed,
}

public sealed record TestDataCollectionSizePolicy
{
    private TestDataCollectionSizePolicy(int minimum, int maximum, bool isFixed)
    {
        Minimum = minimum;
        Maximum = maximum;
        IsFixed = isFixed;
    }

    public int Minimum { get; }
    public int Maximum { get; }
    public bool IsFixed { get; }

    public static TestDataCollectionSizePolicy Fixed(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new(count, count, true);
    }

    public static TestDataCollectionSizePolicy InclusiveRange(int minimum, int maximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimum);
        ArgumentOutOfRangeException.ThrowIfNegative(maximum);
        if (minimum > maximum) throw new ArgumentException("The collection-size range minimum cannot exceed its maximum.");
        return new(minimum, maximum, false);
    }
}

internal sealed record TestDataPolicy
{
    internal static readonly TestDataPolicy Empty = new();
    internal double? OptionalPresence { get; init; }
    internal double? NullProbability { get; init; }
    internal TestDataValueStrategy? ValueStrategy { get; init; }
    internal TestDataCollectionSizePolicy? CollectionSize { get; init; }
    internal IReadOnlyList<TestDataWeightedCandidate>? WeightedValues { get; init; }

    internal TestDataPolicy Overlay(TestDataPolicy other) => new()
    {
        OptionalPresence = other.OptionalPresence ?? OptionalPresence,
        NullProbability = other.NullProbability ?? NullProbability,
        ValueStrategy = other.ValueStrategy ?? ValueStrategy,
        CollectionSize = other.CollectionSize ?? CollectionSize,
        WeightedValues = other.WeightedValues ?? WeightedValues,
    };
}

internal sealed record TestDataWeightedCandidate(JsonElement Value, int Weight);

public sealed class TestDataProfile
{
    private readonly TypeSchemaModel _model;
    private readonly IReadOnlyDictionary<TypeId, TestDataPolicy> _objectRules;
    private readonly IReadOnlyDictionary<string, TestDataPolicy> _logicalRules;
    private readonly IReadOnlyDictionary<(TypeId Owner, PropertyId Property), TestDataPolicy> _propertyRules;

    internal TestDataProfile(TypeSchemaModel model, string name, TestDataPolicy defaults,
        IReadOnlyDictionary<TypeId, TestDataPolicy> objectRules,
        IReadOnlyDictionary<string, TestDataPolicy> logicalRules,
        IReadOnlyDictionary<(TypeId Owner, PropertyId Property), TestDataPolicy> propertyRules)
    {
        _model = model;
        ModelId = model.Id;
        Name = name;
        Defaults = defaults;
        _objectRules = objectRules;
        _logicalRules = logicalRules;
        _propertyRules = propertyRules;
    }

    public SchemaModelId ModelId { get; }
    public string Name { get; }
    internal TestDataPolicy Defaults { get; }

    public static TestDataProfileBuilder Create(TypeSchemaModel model, string name)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new(model, name);
    }

    public static TestDataProfile Compose(string name, params TestDataProfile[] profiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(profiles);
        if (profiles.Length == 0) throw new ArgumentException("At least one profile is required.", nameof(profiles));
        TestDataProfile first = profiles[0] ?? throw new ArgumentException("Profiles cannot contain null.", nameof(profiles));
        var defaults = TestDataPolicy.Empty;
        var objects = new Dictionary<TypeId, TestDataPolicy>();
        var logical = new Dictionary<string, TestDataPolicy>(StringComparer.Ordinal);
        var properties = new Dictionary<(TypeId, PropertyId), TestDataPolicy>();
        foreach (TestDataProfile profile in profiles)
        {
            ArgumentNullException.ThrowIfNull(profile);
            if (profile.ModelId != first.ModelId) throw new ArgumentException("All composed TestData Profiles must target the same model.", nameof(profiles));
            defaults = defaults.Overlay(profile.Defaults);
            OverlayInto(objects, profile._objectRules);
            OverlayInto(logical, profile._logicalRules);
            OverlayInto(properties, profile._propertyRules);
        }
        return new(first._model, name, defaults, objects, logical, properties);
    }

    internal TestDataPolicy Resolve(TypeId containingOwner, TypeId propertyOwner, PropertyDefinition property)
    {
        var policy = Defaults;
        if (_objectRules.TryGetValue(containingOwner, out TestDataPolicy? objectRule)) policy = policy.Overlay(objectRule);
        string? logical = property.Annotations.Items.FirstOrDefault(a => a.Key.Value == "schema.logicalType")?.Value as string;
        if (logical is not null && _logicalRules.TryGetValue(logical, out TestDataPolicy? logicalRule)) policy = policy.Overlay(logicalRule);
        if (_propertyRules.TryGetValue((propertyOwner, property.Id), out TestDataPolicy? propertyRule)) policy = policy.Overlay(propertyRule);
        return policy;
    }

    internal TestDataPolicy Resolve(TypeId owner, PropertyDefinition property) => Resolve(owner, owner, property);
    internal TestDataPolicy? ResolveLogical(string logicalType) => _logicalRules.TryGetValue(logicalType, out TestDataPolicy? policy) ? policy : null;

    private static void OverlayInto<TKey>(Dictionary<TKey, TestDataPolicy> destination, IReadOnlyDictionary<TKey, TestDataPolicy> source) where TKey : notnull
    {
        foreach ((TKey key, TestDataPolicy value) in source) destination[key] = destination.TryGetValue(key, out TestDataPolicy? previous) ? previous.Overlay(value) : value;
    }
}

public sealed class TestDataProfileBuilder
{
    private readonly TypeSchemaModel _model;
    private readonly string _name;
    private TestDataPolicy _defaults = TestDataPolicy.Empty;
    private readonly Dictionary<TypeId, TestDataPolicy> _objectRules = [];
    private readonly Dictionary<string, TestDataPolicy> _logicalRules = new(StringComparer.Ordinal);
    private readonly Dictionary<(TypeId, PropertyId), TestDataPolicy> _propertyRules = [];

    internal TestDataProfileBuilder(TypeSchemaModel model, string name) { _model = model; _name = name; }

    public TestDataProfileBuilder Defaults(Action<TestDataProfileRuleBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var rule = new TestDataProfileRuleBuilder(policy => _defaults = _defaults.Overlay(policy));
        configure(rule);
        _ = rule.Done();
        return this;
    }

    public TestDataProfileRuleBuilder For(TypeId typeId)
    {
        EnsureObject(typeId);
        TestDataProfileRuleBuilder? scope = null;
        scope = new(policy => Add(_objectRules, typeId, policy), property =>
        {
            (ObjectTypeDefinition owner, PropertyDefinition definition)? resolved = FindPropertyOwner(_model, (ObjectTypeDefinition)_model.TypesById[typeId], property.Id);
            if (resolved is null) throw new ArgumentException($"Property '{property.Id.Value}' was not found on object type '{typeId.Value}'.");
            return new(policy => Add(_propertyRules, (resolved.Value.owner.Id, resolved.Value.definition.Id), policy), parent: scope);
        }, propertyId =>
        {
            (ObjectTypeDefinition owner, PropertyDefinition definition)? resolved = FindPropertyOwner(_model, (ObjectTypeDefinition)_model.TypesById[typeId], propertyId);
            if (resolved is null) throw new ArgumentException($"Property '{propertyId.Value}' was not found on object type '{typeId.Value}'.");
            return new(policy => Add(_propertyRules, (resolved.Value.owner.Id, resolved.Value.definition.Id), policy), parent: scope);
        }, build: () => Build());
        return scope;
    }

    public TestDataProfileRuleBuilder For(ObjectTypeDefinition type) { ArgumentNullException.ThrowIfNull(type); return For(type.Id); }

    public TestDataProfileRuleBuilder ForLogicalType(string logicalType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalType);
        return new(policy => Add(_logicalRules, logicalType, policy), build: Build);
    }

    public TestDataProfile Build()
    {
        ValidatePolicy(_defaults, null, null);
        foreach ((TypeId owner, TestDataPolicy policy) in _objectRules) ValidatePolicy(policy, owner, null);
        foreach ((string logical, TestDataPolicy policy) in _logicalRules)
        {
            if (!_model.Types.OfType<ObjectTypeDefinition>().SelectMany(o => o.Properties).Any(p => p.Annotations.Items.Any(a => a.Key.Value == "schema.logicalType" && a.Value as string == logical)))
                throw new ArgumentException($"Logical Type '{logical}' was not found in the canonical model.", nameof(logical));
            ValidateLogicalPolicy(logical, policy);
        }
        foreach (((TypeId owner, PropertyId property) key, TestDataPolicy policy) in _propertyRules)
        {
            if (!_model.TypesById.TryGetValue(key.owner, out TypeDefinition? type) || type is not ObjectTypeDefinition objectType)
                throw new ArgumentException($"Object type '{key.owner.Value}' was not found in the canonical model.");
            PropertyDefinition? definition = FindProperty(_model, objectType, key.property);
            if (definition is null) throw new ArgumentException($"Property '{key.property.Value}' was not found on object type '{key.owner.Value}'.");
            ValidatePolicy(policy, key.owner, definition);
        }
        return new TestDataProfile(_model, _name, _defaults, new Dictionary<TypeId, TestDataPolicy>(_objectRules), new Dictionary<string, TestDataPolicy>(_logicalRules), new Dictionary<(TypeId, PropertyId), TestDataPolicy>(_propertyRules));
    }

    private void EnsureObject(TypeId id)
    {
        if (!_model.TypesById.TryGetValue(id, out TypeDefinition? type) || type is not ObjectTypeDefinition) throw new ArgumentException($"Object type '{id.Value}' was not found in the canonical model.", nameof(id));
    }

    private void ValidateLogicalPolicy(string logical, TestDataPolicy policy)
    {
        ValidatePolicy(policy, null, null);
        foreach (PropertyDefinition property in _model.Types.OfType<ObjectTypeDefinition>().SelectMany(o => o.Properties).Where(p => p.Annotations.Items.Any(a => a.Key.Value == "schema.logicalType" && a.Value as string == logical)))
            if (_model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? type) && type is (ScalarTypeDefinition or EnumTypeDefinition))
                ValidatePolicy(policy, null, property);
    }

    private static void Add<TKey>(Dictionary<TKey, TestDataPolicy> rules, TKey key, TestDataPolicy policy) where TKey : notnull => rules[key] = rules.TryGetValue(key, out TestDataPolicy? previous) ? previous.Overlay(policy) : policy;
    private static PropertyDefinition? FindProperty(TypeSchemaModel model, ObjectTypeDefinition owner, PropertyId id)
    {
        PropertyDefinition? direct = owner.Properties.FirstOrDefault(p => p.Id == id);
        if (direct is not null) return direct;
        foreach (TypeRef baseRef in owner.Composition.AllOf)
            if (model.TypesById.TryGetValue(baseRef.Id, out TypeDefinition? type) && type is ObjectTypeDefinition baseObject && FindProperty(model, baseObject, id) is PropertyDefinition inherited) return inherited;
        return null;
    }

    private static (ObjectTypeDefinition Owner, PropertyDefinition Definition)? FindPropertyOwner(TypeSchemaModel model, ObjectTypeDefinition owner, PropertyId id)
    {
        PropertyDefinition? direct = owner.Properties.FirstOrDefault(p => p.Id == id);
        if (direct is not null) return (owner, direct);
        foreach (TypeRef baseRef in owner.Composition.AllOf)
            if (model.TypesById.TryGetValue(baseRef.Id, out TypeDefinition? type) && type is ObjectTypeDefinition baseObject && FindPropertyOwner(model, baseObject, id) is { } inherited) return inherited;
        return null;
    }

    private void ValidatePolicy(TestDataPolicy policy, TypeId? ownerId, PropertyDefinition? property)
    {
        if (policy.OptionalPresence is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(policy.OptionalPresence));
        if (policy.NullProbability is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(policy.NullProbability));
        if (property is not null && property.Cardinality.IsRequired && policy.OptionalPresence is not null) throw new ArgumentException("Presence policy cannot target a required property.");
        if (property is not null && !property.Cardinality.AllowsNull && policy.NullProbability > 0) throw new ArgumentException("Positive null probability cannot target a non-nullable property.");
        if (property is not null && policy.CollectionSize is not null && _model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? collectionType) && collectionType is not (ArrayTypeDefinition or DictionaryTypeDefinition)) throw new ArgumentException("Collection-size policy requires an array or dictionary property.");
        if (property is not null && policy.CollectionSize is { } size && _model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? sizedType) && sizedType is ArrayTypeDefinition or DictionaryTypeDefinition)
        {
            int lower = Math.Max(size.Minimum, Math.Max(property.Cardinality.MinItems ?? 0, sizedType is ArrayTypeDefinition array ? array.MinItems ?? 0 : 0));
            int upper = Math.Min(size.Maximum, Math.Min(property.Cardinality.MaxItems ?? int.MaxValue, sizedType is ArrayTypeDefinition boundedArray ? boundedArray.MaxItems ?? int.MaxValue : int.MaxValue));
            if (property.Constraints.Array is { } constrained)
            {
                lower = Math.Max(lower, constrained.MinItems ?? 0);
                upper = Math.Min(upper, constrained.MaxItems ?? int.MaxValue);
            }
            if (upper > 10_000) upper = 10_000;
            if (lower > upper) throw new ArgumentException("Collection-size policy has no legal intersection with the canonical bounds.");
        }
        if (policy.WeightedValues is null) return;
        foreach (TestDataWeightedCandidate candidate in policy.WeightedValues)
        {
            if (candidate.Weight <= 0) throw new ArgumentOutOfRangeException(nameof(candidate.Weight));
            if (property is not null && _model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? candidateType)) ValidateCandidate(candidateType, property.Constraints, candidate.Value);
        }
    }

    private static void ValidateCandidate(TypeDefinition type, ConstraintSet constraints, JsonElement value)
    {
        if (type is ScalarTypeDefinition scalar)
        {
            if (!TerminologyCandidate.TryRead(value, scalar, constraints, out _, out string? error)) throw new ArgumentException($"Weighted candidate is invalid: {error}");
        }
        else if (type is EnumTypeDefinition @enum && !@enum.Values.Any(v => JsonSerializer.Serialize(v.Value) == value.GetRawText() || string.Equals(v.Name, value.GetString(), StringComparison.Ordinal)))
            throw new ArgumentException("Weighted enum candidate is not a declared enum value.");
        else throw new ArgumentException("Weighted values require a scalar or enum property.");
    }
}

public sealed class TestDataProfileRuleBuilder
{
    private readonly Action<TestDataPolicy> _commit;
    private readonly Func<PropertyDefinition, TestDataProfileRuleBuilder>? _propertyFactory;
    private readonly Func<PropertyId, TestDataProfileRuleBuilder>? _propertyIdFactory;
    private readonly TestDataProfileRuleBuilder? _parent;
    private readonly Func<TestDataProfile>? _build;
    internal TestDataProfileRuleBuilder(Action<TestDataPolicy> commit, Func<PropertyDefinition, TestDataProfileRuleBuilder>? propertyFactory = null, Func<PropertyId, TestDataProfileRuleBuilder>? propertyIdFactory = null, TestDataProfileRuleBuilder? parent = null, Func<TestDataProfile>? build = null)
    {
        _commit = commit;
        _propertyFactory = propertyFactory;
        _propertyIdFactory = propertyIdFactory;
        _parent = parent;
        _build = build;
    }
    private TestDataPolicy _policy = TestDataPolicy.Empty;

    public TestDataProfileRuleBuilder OptionalPresence(double probability) { _policy = _policy with { OptionalPresence = probability }; return this; }
    public TestDataProfileRuleBuilder Presence(double probability) => OptionalPresence(probability);
    public TestDataProfileRuleBuilder NullProbability(double probability) { _policy = _policy with { NullProbability = probability }; return this; }
    public TestDataProfileRuleBuilder ValueStrategy(TestDataValueStrategy strategy) { _policy = _policy with { ValueStrategy = strategy }; return this; }
    public TestDataProfileRuleBuilder CollectionSize(TestDataCollectionSizePolicy policy) { ArgumentNullException.ThrowIfNull(policy); _policy = _policy with { CollectionSize = policy }; return this; }
    public TestDataProfileRuleBuilder FixedCount(int count) => CollectionSize(TestDataCollectionSizePolicy.Fixed(count));
    public TestDataProfileRuleBuilder CountRange(int minimum, int maximum) => CollectionSize(TestDataCollectionSizePolicy.InclusiveRange(minimum, maximum));
    public TestDataProfileRuleBuilder Weighted(params (string Value, int Weight)[] values) => Weighted(values.Select(v => ((object?)v.Value, v.Weight)).ToArray());
    public TestDataProfileRuleBuilder Weighted(params (object? Value, int Weight)[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _policy = _policy with { WeightedValues = values.Select(v => new TestDataWeightedCandidate(JsonSerializer.SerializeToElement(v.Value), v.Weight)).ToArray() };
        return this;
    }
    public TestDataProfileRuleBuilder Property(PropertyDefinition property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return _propertyFactory is not null ? _propertyFactory(property) : _parent?.Property(property) ?? throw new InvalidOperationException("Property rules require an object scope.");
    }
    public TestDataProfileRuleBuilder Property(PropertyId propertyId)
    {
        return _propertyIdFactory is not null ? _propertyIdFactory(propertyId) : _parent?.Property(propertyId) ?? throw new InvalidOperationException("Property rules require an object scope.");
    }
    public TestDataProfileRuleBuilder Done() { _commit(_policy); return _parent ?? this; }
    public TestDataProfile Build()
    {
        if (_build is null) throw new InvalidOperationException("Build is only available on an object-scoped profile builder.");
        _commit(_policy);
        return _build();
    }
}
