using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData;

internal sealed class GenerationSession
{
    private readonly Dictionary<(TypeId Owner, PropertyId Property), int> _batchSequences = [];
    private readonly Dictionary<(TypeId Owner, PropertyId Property), SemanticTestValue> _batchShared = [];
    private readonly Dictionary<(TypeId Owner, PropertyId Property), HashSet<string>> _batchUnique = [];
    internal RootGenerationState Root { get; private set; } = new();

    internal void BeginRoot()
    {
        Root = new RootGenerationState();
    }
    internal int NextSequence((TypeId Owner, PropertyId Property) key, TestDataValueScope scope)
    {
        Dictionary<(TypeId Owner, PropertyId Property), int> counters = scope == TestDataValueScope.Batch ? _batchSequences : Root.Sequences;
        var index = counters.TryGetValue(key, out var current) ? current : 0;
        counters[key] = index + 1;
        return index;
    }
    internal bool TryGetShared((TypeId Owner, PropertyId Property) key, TestDataValueScope scope, out SemanticTestValue value)
    { return (scope == TestDataValueScope.Batch ? _batchShared : Root.Shared).TryGetValue(key, out value!); }
    internal void SetShared((TypeId Owner, PropertyId Property) key, TestDataValueScope scope, SemanticTestValue value)
    { (scope == TestDataValueScope.Batch ? _batchShared : Root.Shared)[key] = value; }
    internal bool AddUnique((TypeId Owner, PropertyId Property) key, TestDataValueScope scope, string fingerprint)
    { return (scope == TestDataValueScope.Batch ? Get(_batchUnique, key) : Get(Root.Unique, key)).Add(fingerprint); }
    private static HashSet<string> Get(Dictionary<(TypeId Owner, PropertyId Property), HashSet<string>> map, (TypeId Owner, PropertyId Property) key)
    {
        if (!map.TryGetValue(key, out HashSet<string>? set)) { map[key] = set = []; }
        return set;
    }
}

internal sealed class RootGenerationState
{
    internal Dictionary<(TypeId Owner, PropertyId Property), int> Sequences { get; } = [];
    internal Dictionary<(TypeId Owner, PropertyId Property), SemanticTestValue> Shared { get; } = [];
    internal Dictionary<(TypeId Owner, PropertyId Property), HashSet<string>> Unique { get; } = [];
}
