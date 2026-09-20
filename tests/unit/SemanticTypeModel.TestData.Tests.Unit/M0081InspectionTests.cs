#pragma warning disable IDE0007, IDE0022
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.TestData.Inspection;

namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, CA1707
public sealed class M0081InspectionTests
{
    [Test]
    public async Task Inspection_is_deterministic_and_orders_object_properties_by_canonical_id()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition root = Object("Root",
        [
            Property("z-property", "Zed", text.Id),
            Property("a-property", "Alpha", text.Id),
        ]);
        TypeSchemaModel model = Model(root, text);
        var first = new ObjectTestValue(root.Id, new Dictionary<PropertyId, SemanticTestValue>
        {
            [new PropertyId("z-property")] = new ScalarTestValue(text.Id, ScalarKind.String, "line\nvalue"),
            [new PropertyId("a-property")] = new ScalarTestValue(text.Id, ScalarKind.String, "alpha"),
        });
        var second = new ObjectTestValue(root.Id, new Dictionary<PropertyId, SemanticTestValue>
        {
            [new PropertyId("a-property")] = new ScalarTestValue(text.Id, ScalarKind.String, "alpha"),
            [new PropertyId("z-property")] = new ScalarTestValue(text.Id, ScalarKind.String, "line\nvalue"),
        });

        string firstText = first.ToSemanticText(model);
        _ = await Assert.That(firstText).IsEqualTo(second.ToSemanticText(model));
        _ = await Assert.That(firstText).Contains("Alpha [a-property]");
        _ = await Assert.That(firstText).Contains("\\nvalue");
        _ = await Assert.That(firstText.Contains('\r')).IsFalse();
    }

    [Test]
    public async Task Inspection_preserves_scalar_shapes_array_dictionary_enum_and_null()
    {
        ScalarTypeDefinition integer = Scalar("Integer", ScalarKind.Integer);
        ScalarTypeDefinition binary = Scalar("Binary", ScalarKind.Binary);
        ScalarTypeDefinition json = Scalar("Json", ScalarKind.Json);
        EnumTypeDefinition status = new()
        {
            Id = new("Status"),
            Name = "Status",
            Kind = TypeKind.Enum,
            Nullability = Nullability.NonNullable,
            Annotations = new(),
            StorageKind = EnumStorageKind.String,
            Values = [new EnumValueDefinition { Name = "Ready", Value = "ready", Annotations = new() }],
        };
        ArrayTypeDefinition numbers = new()
        {
            Id = new("Numbers"),
            Name = "Numbers",
            Kind = TypeKind.Array,
            Nullability = Nullability.NonNullable,
            Annotations = new(),
            ItemType = new TypeRef(integer.Id),
        };
        DictionaryTypeDefinition map = new()
        {
            Id = new("Map"),
            Name = "Map",
            Kind = TypeKind.Dictionary,
            Nullability = Nullability.NonNullable,
            Annotations = new(),
            KeyType = new TypeRef(integer.Id),
            ValueType = new TypeRef(json.Id),
        };
        ObjectTypeDefinition root = Object("Root", []);
        TypeSchemaModel model = Model(root, integer, binary, json, status, numbers, map);
        var value = new ObjectTestValue(new TypeId("Root"), new Dictionary<PropertyId, SemanticTestValue>());
        // Inspect the graph nodes directly so the test remains independent of CLR materialization.
        var array = new ArrayTestValue(numbers.Id,
        [new ScalarTestValue(integer.Id, ScalarKind.Integer, 7), new NullTestValue(integer.Id)]);
        var dictionary = new DictionaryTestValue(map.Id,
        [new(new ScalarTestValue(integer.Id, ScalarKind.Integer, 1), new ScalarTestValue(json.Id, ScalarKind.Json, JsonDocument.Parse("{\"ok\":true}").RootElement.Clone()))]);
        _ = await Assert.That(new EnumTestValue(status.Id, "ready").ToSemanticText(model)).Contains("(Enum): ready");
        _ = await Assert.That(array.ToSemanticText(model)).Contains("[1]:\nInteger (null): null");
        _ = await Assert.That(dictionary.ToSemanticText(model)).Contains("entry 0 key:");
        _ = await Assert.That(new ScalarTestValue(binary.Id, ScalarKind.Binary, new byte[] { 0x01, 0xFE }).ToSemanticText(model)).Contains("Af4=");
        _ = await Assert.That(value.ToSemanticText(model)).Contains("Root (Object)");
    }

    [Test]
    public async Task Inspection_rejects_missing_model_type_and_property()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        TypeSchemaModel model = Model(text);
        InvalidOperationException missingType = Assert.Throws<InvalidOperationException>(() => new ScalarTestValue(new TypeId("Missing"), ScalarKind.String, "x").ToSemanticText(model));
        _ = await Assert.That(missingType.Message).Contains("Missing");

        ObjectTypeDefinition root = Object("Root", [Property("Known", "Known", text.Id)]);
        model = Model(root, text);
        InvalidOperationException missingProperty = Assert.Throws<InvalidOperationException>(() => new ObjectTestValue(root.Id, new Dictionary<PropertyId, SemanticTestValue>
        {
            [new PropertyId("Missing")] = new ScalarTestValue(text.Id, ScalarKind.String, "x"),
        }).ToSemanticText(model));
        _ = await Assert.That(missingProperty.Message).Contains("Missing");
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind) => new()
    {
        Id = new(id),
        Name = id,
        Kind = TypeKind.Scalar,
        ScalarKind = kind,
        Nullability = Nullability.NonNullable,
        Annotations = new(),
    };

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties) => new()
    {
        Id = new(id),
        Name = id,
        Kind = TypeKind.Object,
        Nullability = Nullability.NonNullable,
        Annotations = new(),
        Keys = [],
        Properties = properties,
    };

    private static PropertyDefinition Property(string id, string name, TypeId type) => new()
    {
        Id = new(id),
        Name = name,
        Type = new TypeRef(type),
        Cardinality = new() { IsRequired = true },
        Constraints = new(),
        Annotations = new(),
    };

    private static TypeSchemaModel Model(params TypeDefinition[] types) => new()
    {
        Id = new("m0081-inspection"),
        Types = types,
        TypesById = types.ToDictionary(type => type.Id),
        Annotations = new(),
    };
}
#pragma warning restore IDE0007, IDE0022
