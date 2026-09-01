using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticTypeModel.SystemTextJson;

internal sealed class StrongScalarJsonConverterFactory(IEnumerable<(Type Wrapper, Type Value)> mappings) : JsonConverterFactory
{
    private readonly Dictionary<Type, Type> _wrappers = mappings.ToDictionary(static pair => pair.Wrapper, static pair => pair.Value);

    public override bool CanConvert(Type type)
    {
        return _wrappers.ContainsKey(type);
    }

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        Type valueType = _wrappers[type];
        return (JsonConverter)Activator.CreateInstance(typeof(StrongScalarJsonConverter<,>).MakeGenericType(type, valueType))!;
    }

    internal static Type? Resolve(string id)
    {
        var name = id.StartsWith("global::", StringComparison.Ordinal) ? id[8..] : id;
        return name switch
        {
            "bool" => typeof(bool),
            "byte" => typeof(byte),
            "sbyte" => typeof(sbyte),
            "short" => typeof(short),
            "ushort" => typeof(ushort),
            "int" => typeof(int),
            "uint" => typeof(uint),
            "long" => typeof(long),
            "ulong" => typeof(ulong),
            "nint" => typeof(nint),
            "nuint" => typeof(nuint),
            "float" => typeof(float),
            "double" => typeof(double),
            "decimal" => typeof(decimal),
            "char" => typeof(char),
            "string" => typeof(string),
            "object" => typeof(object),
            "byte[]" => typeof(byte[]),
            _ => Type.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false)).FirstOrDefault(static type => type is not null),
        };
    }
}

internal sealed class StrongScalarJsonConverter<TWrapper, TValue> : JsonConverter<TWrapper>
    where TWrapper : struct
{
    private static readonly ConstructorInfo Constructor = typeof(TWrapper).GetConstructor([typeof(TValue)])
        ?? throw new InvalidOperationException($"Strong Scalar '{typeof(TWrapper)}' must expose a constructor accepting '{typeof(TValue)}'.");

    public override TWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        TValue value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        return (TWrapper)Constructor.Invoke([value]);
    }

    public override void Write(Utf8JsonWriter writer, TWrapper value, JsonSerializerOptions options)
    {
        var underlying = (TValue)typeof(TWrapper).GetProperty("Value")!.GetValue(value)!;
        JsonSerializer.Serialize(writer, underlying, options);
    }
}
