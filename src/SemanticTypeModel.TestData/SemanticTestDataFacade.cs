using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
#pragma warning disable CS1591, CA1822, IDE0011, IDE0021, IDE0046, IDE0058, IDE0305
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData;

public sealed record TestDataBudgets
{
    public int MaxStringLength { get; init; } = 65_536;
    public int MaxBinaryLength { get; init; } = 65_536;
    public int MaxCollectionItems { get; init; } = 10_000;
    public int MaxDictionaryEntries { get; init; } = 10_000;
    public int MaxDepth { get; init; } = 32;
    public int MaxNodes { get; init; } = 100_000;

    public void Validate()
    {
        if (MaxStringLength < 0 || MaxBinaryLength < 0 || MaxCollectionItems < 0 || MaxDictionaryEntries < 0 || MaxDepth < 0 || MaxNodes <= 0)
            throw new ArgumentOutOfRangeException(nameof(TestDataBudgets), "TestData budgets must be non-negative, with MaxNodes positive.");
    }
}

public sealed class TestDataGenerationException(string message, IReadOnlyList<SchemaDiagnostic> diagnostics) : Exception(message)
{
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; } = diagnostics;
}

public sealed record TestDataGeneratorContext(TypeSchemaModel Model, TypeDefinition? Type, PropertyDefinition? Property, string? LogicalType, TestDataSizeProfile SizeProfile, int Seed, int RootOrdinal);

public sealed class SemanticTestDataOptions
{
    internal Func<ObjectTypeDefinition, PropertyDefinition, TestDataGeneratorContext, object?>? PropertyGenerator { get; init; }
    internal Func<string, TestDataGeneratorContext, object?>? LogicalTypeGenerator { get; init; }
    internal int RootOrdinal { get; init; }
    public TestDataBudgets Budgets { get; init; } = new();
}

public sealed class SemanticTestDataFacade
{
    private readonly TypeSchemaModel _model;
    private readonly TestDataSizeProfile _profile;
    private readonly int _seed;
    private readonly SemanticTerminologyProfile? _terminology;
    private readonly TestDataBudgets _budgets;
    private readonly Dictionary<(Type Clr, string Member), Func<TestDataGeneratorContext, object?>> _propertyGenerators = [];
    private readonly Dictionary<string, Func<TestDataGeneratorContext, object?>> _logicalGenerators = new(StringComparer.Ordinal);

    internal SemanticTestDataFacade(TypeSchemaModel model) : this(model, TestDataSizeProfile.Simple, 0, null, new()) { }
    private SemanticTestDataFacade(TypeSchemaModel model, TestDataSizeProfile profile, int seed, SemanticTerminologyProfile? terminology, TestDataBudgets budgets)
    {
        _model = model; _profile = profile; _seed = seed; _terminology = terminology; _budgets = budgets;
    }

    public SemanticTestDataFacade WithSizeProfile(TestDataSizeProfile profile)
    {
        return new SemanticTestDataFacade(_model, profile, _seed, _terminology, _budgets).Copy(this);
    }

    public SemanticTestDataFacade WithSeed(int seed)
    {
        return new SemanticTestDataFacade(_model, _profile, seed, _terminology, _budgets).Copy(this);
    }

    public SemanticTestDataFacade WithTerminology(SemanticTerminologyProfile profile) { ArgumentNullException.ThrowIfNull(profile); return new SemanticTestDataFacade(_model, _profile, _seed, profile, _budgets).Copy(this); }
    public SemanticTestDataFacade WithBudgets(TestDataBudgets budgets) { ArgumentNullException.ThrowIfNull(budgets); budgets.Validate(); return new SemanticTestDataFacade(_model, _profile, _seed, _terminology, budgets).Copy(this); }

    public SemanticTestDataFacade WithLogicalTypeGenerator(string logicalType, Func<TestDataGeneratorContext, object?> generator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalType); ArgumentNullException.ThrowIfNull(generator);
        SemanticTestDataFacade copy = Copy(this); copy._logicalGenerators[logicalType] = generator; return copy;
    }

    public SemanticTestDataFacade WithPropertyGenerator<T>(Expression<Func<T, object?>> property, Func<TestDataGeneratorContext, object?> generator)
    {
        ArgumentNullException.ThrowIfNull(property); ArgumentNullException.ThrowIfNull(generator);
        MemberExpression member = FindMember(property.Body) ?? throw new ArgumentException("The property expression must select one member.", nameof(property));
        SemanticTestDataFacade copy = Copy(this); copy._propertyGenerators[(typeof(T), member.Member.Name)] = generator; return copy;
    }

    public T Generate<T>()
    {
        TypeId root = ResolveRoot(typeof(T));
        TestDataGenerationResult result = GenerateSemantic(root, 0);
        if (!result.Succeeded) throw new TestDataGenerationException("TestData generation failed.", result.Diagnostics);
        try { return (T)SemanticTestDataMaterializer.Materialize(_model, result.Value!, typeof(T)); }
        catch (TestDataGenerationException) { throw; }
        catch (Exception exception) { throw new TestDataGenerationException(exception.Message, [MaterializationDiagnostic(exception.Message, ModelPath.ForType(root))]); }
    }

    public IReadOnlyList<T> GenerateMany<T>(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var values = new List<T>(count);
        for (var i = 0; i < count; i++)
        {
            TestDataGenerationResult result = GenerateSemantic(ResolveRoot(typeof(T)), i);
            if (!result.Succeeded) throw new TestDataGenerationException("TestData generation failed.", result.Diagnostics);
            values.Add((T)SemanticTestDataMaterializer.Materialize(_model, result.Value!, typeof(T)));
        }
        return values;
    }

    public T Materialize<T>(SemanticTestValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        try { return (T)SemanticTestDataMaterializer.Materialize(_model, value, typeof(T)); }
        catch (TestDataGenerationException) { throw; }
        catch (Exception exception) { throw new TestDataGenerationException(exception.Message, [MaterializationDiagnostic(exception.Message, ModelPath.ForType(value.TypeId))]); }
    }

    private TestDataGenerationResult GenerateSemantic(TypeId root, int ordinal)
    {
        return SemanticTestDataGenerator.Generate(_model, root, _profile, _seed + ordinal, _terminology, new SemanticTestDataOptions { Budgets = _budgets, RootOrdinal = ordinal, PropertyGenerator = (owner, property, context) => _propertyGenerators.TryGetValue((ResolveClrType(owner.Id) ?? typeof(object), MemberName(property)), out Func<TestDataGeneratorContext, object?>? generator) ? generator(context) : null, LogicalTypeGenerator = (name, context) => _logicalGenerators.TryGetValue(name, out Func<TestDataGeneratorContext, object?>? generator) ? generator(context) : null });
    }

    private TypeId ResolveRoot(Type type)
    {
        var id = "global::" + (type.FullName ?? type.Name);
        if (!_model.TypesById.ContainsKey(new TypeId(id))) throw new InvalidOperationException($"CLR type '{type.FullName}' was not found in the canonical model.");
        return new TypeId(id);
    }

    private static string MemberName(PropertyDefinition property)
    {
        return property.Annotations.Items.FirstOrDefault(a => a.Key.Value == "dotnet.memberName")?.Value as string ?? property.Name;
    }

    private Type? ResolveClrType(TypeId id)
    {
        return SemanticTestDataMaterializer.ResolveClrType(id.Value);
    }

    private SemanticTestDataFacade Copy(SemanticTestDataFacade source)
    {
        foreach (KeyValuePair<(Type Clr, string Member), Func<TestDataGeneratorContext, object?>> item in source._propertyGenerators) _propertyGenerators[item.Key] = item.Value;
        foreach (KeyValuePair<string, Func<TestDataGeneratorContext, object?>> item in source._logicalGenerators) _logicalGenerators[item.Key] = item.Value;
        return this;
    }
    private static MemberExpression? FindMember(Expression expression)
    {
        return expression is MemberExpression member ? member : expression is UnaryExpression unary ? FindMember(unary.Operand) : null;
    }

    private static SchemaDiagnostic MaterializationDiagnostic(string message, string path)
    {
        return new() { Severity = SchemaDiagnosticSeverity.Error, Code = "TESTDATA_MATERIALIZATION_FAILED", Message = message, Stage = SchemaDiagnosticStage.Validation, ModelPath = path, PipelineStage = "TestData" };
    }
}

public static class SemanticTestDataExtensions
{
    public static SemanticTestDataFacade TestData(this TypeSchemaModel model) { ArgumentNullException.ThrowIfNull(model); return new SemanticTestDataFacade(model); }
}

internal static class SemanticTestDataMaterializer
{
    internal static object Materialize(TypeSchemaModel model, SemanticTestValue value, Type targetType)
    {
        if (value is NullTestValue) return null!;
        if (value is ScalarTestValue scalar) return ConvertScalar(scalar.Value, targetType);
        if (value is EnumTestValue @enum) return ConvertEnum(@enum.Value, targetType);
        if (value is ArrayTestValue array) return ConvertCollection(model, array.Items, targetType);
        if (value is DictionaryTestValue dictionary) return ConvertDictionary(model, dictionary.Entries, targetType);
        if (value is ObjectTestValue obj) return ConvertObject(model, obj, targetType);
        throw new InvalidOperationException($"Semantic value '{value.GetType().Name}' cannot be materialized to '{targetType}'.");
    }

    internal static Type? ResolveClrType(string id)
    {
        var name = id.StartsWith("global::", StringComparison.Ordinal) ? id[8..] : id;
        return Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t is not null);
    }

    private static object ConvertObject(TypeSchemaModel model, ObjectTestValue value, Type targetType)
    {
        var values = new Dictionary<string, object?>();
        var definition = (ObjectTypeDefinition)model.GetType(value.ObjectTypeId);
        foreach (KeyValuePair<PropertyId, SemanticTestValue> pair in value.Properties)
        {
            PropertyDefinition property = definition.Properties.First(p => p.Id == pair.Key);
            var name = property.Annotations.Items.FirstOrDefault(a => a.Key.Value == "dotnet.memberName")?.Value as string ?? property.Name;
            MemberInfo? member = targetType.GetMember(name, BindingFlags.Public | BindingFlags.Instance).SingleOrDefault();
            Type memberType = member switch { PropertyInfo info => info.PropertyType, FieldInfo info => info.FieldType, _ => throw new InvalidOperationException($"Public member '{name}' was not found on '{targetType}'.") };
            values[name] = Materialize(model, pair.Value, memberType);
        }
        ConstructorInfo[] constructors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        ConstructorInfo? parameterless = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
        if (parameterless is not null && values.Keys.All(name => targetType.GetMember(name, BindingFlags.Public | BindingFlags.Instance).Any(m => m is PropertyInfo { CanWrite: true } or FieldInfo { IsInitOnly: false })))
        {
            var instance = parameterless.Invoke(null);
            foreach (KeyValuePair<string, object?> pair in values) Assign(instance, pair.Key, pair.Value);
            return instance;
        }
        ConstructorInfo[] eligible = constructors.Where(c => c.GetParameters().All(p => values.Keys.Any(name => string.Equals(name, p.Name, StringComparison.OrdinalIgnoreCase)))).ToArray();
        if (eligible.Length != 1) throw new InvalidOperationException($"Public construction of '{targetType}' is missing or ambiguous.");
        ConstructorInfo selected = eligible[0];
        var result = selected.Invoke(selected.GetParameters().Select(p => values.First(v => string.Equals(v.Key, p.Name, StringComparison.OrdinalIgnoreCase)).Value).ToArray());
        foreach (KeyValuePair<string, object?> pair in values.Where(v => selected.GetParameters().All(p => !string.Equals(p.Name, v.Key, StringComparison.OrdinalIgnoreCase)))) Assign(result, pair.Key, pair.Value);
        return result;
    }

    private static void Assign(object instance, string name, object? value)
    {
        if (instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { CanWrite: true } property) { property.SetValue(instance, value); return; }
        if (instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance) is { IsInitOnly: false } field) { field.SetValue(instance, value); return; }
        throw new InvalidOperationException($"Public member '{name}' on '{instance.GetType()}' is not assignable.");
    }

    private static object ConvertCollection(TypeSchemaModel model, IReadOnlyList<SemanticTestValue> items, Type targetType)
    {
        Type element = targetType.IsArray ? targetType.GetElementType()! : targetType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
        var values = items.Select(item => Materialize(model, item, element)).ToArray();
        if (targetType.IsArray) { var array = Array.CreateInstance(element, values.Length); for (var i = 0; i < values.Length; i++) array.SetValue(values[i], i); return array; }
        Type concrete = targetType.IsInterface || targetType.IsAbstract ? (targetType.GetGenericTypeDefinition() == typeof(ISet<>) ? typeof(HashSet<>) : typeof(List<>)).MakeGenericType(element) : targetType;
        var instance = Activator.CreateInstance(concrete) ?? throw new InvalidOperationException($"Collection '{targetType}' is unsupported.");
        if (instance is IList list)
        {
            foreach (var item in values) list.Add(item);
            return list;
        }
        MethodInfo? add = concrete.GetMethod("Add", [element]) ?? throw new InvalidOperationException($"Collection '{targetType}' is unsupported.");
        foreach (var item in values) add.Invoke(instance, [item]);
        return instance;
    }

    private static object ConvertDictionary(TypeSchemaModel model, IReadOnlyList<KeyValuePair<SemanticTestValue, SemanticTestValue>> entries, Type targetType)
    {
        Type[] args = targetType.GetGenericArguments();
        Type concrete = targetType.IsInterface || targetType.IsAbstract ? typeof(Dictionary<,>).MakeGenericType(args) : targetType;
        if (Activator.CreateInstance(concrete) is not IDictionary dictionary) throw new InvalidOperationException($"Dictionary '{targetType}' is unsupported.");
        foreach (KeyValuePair<SemanticTestValue, SemanticTestValue> entry in entries) dictionary.Add(Materialize(model, entry.Key, args[0]), Materialize(model, entry.Value, args[1]));
        return dictionary;
    }

    private static object ConvertScalar(object? value, Type targetType)
    {
        if (value is null) return null!;
        Type actual = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (actual == typeof(Uri)) return new Uri((string)value, UriKind.RelativeOrAbsolute);
        if (actual == typeof(byte[])) return value is byte[] bytes ? bytes : Convert.FromBase64String((string)value);
        if (actual == typeof(ReadOnlyMemory<byte>)) return new ReadOnlyMemory<byte>((byte[])ConvertScalar(value, typeof(byte[])));
        if (actual == typeof(char)) { var text = (string)value; if (text.Length != 1) throw new InvalidOperationException("A semantic string must contain exactly one character."); return text[0]; }
        if (actual == typeof(JsonElement)) return value is JsonElement element ? element : JsonSerializer.SerializeToElement(value);
        if (actual == typeof(JsonDocument)) return JsonDocument.Parse(JsonSerializer.Serialize(value));
        if (actual == typeof(JsonNode)) return JsonNode.Parse(JsonSerializer.Serialize(value))!;
        if (actual == typeof(DateOnly) && value is DateOnly date) return date;
        if (actual == typeof(TimeOnly) && value is TimeOnly time) return time;
        if (actual == typeof(Guid) && value is Guid guid) return guid;
        if (actual.IsEnum) return ConvertEnum(value, actual);
        return Convert.ChangeType(value, actual, CultureInfo.InvariantCulture)!;
    }

    private static object ConvertEnum(object value, Type targetType)
    {
        return Enum.Parse(Nullable.GetUnderlyingType(targetType) ?? targetType, Convert.ToString(value, CultureInfo.InvariantCulture)!, true);
    }
}
