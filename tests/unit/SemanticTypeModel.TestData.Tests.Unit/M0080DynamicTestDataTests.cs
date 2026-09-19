using SemanticTypeModel.Abstractions.Model;
#pragma warning disable CS1591, CA1707, IDE0055

namespace SemanticTypeModel.TestData.Tests.Unit;

public sealed class M0080DynamicTestDataTests
{
    [Test]
    public async Task Generates_semantic_values_by_canonical_root_id_and_many_is_deterministic()
    {
        TypeSchemaModel model = Model();
        IReadOnlyList<SemanticTestValue> first = model.TestData().WithSeed(17).GenerateMany(new TypeId("Root"), 2);
        IReadOnlyList<SemanticTestValue> second = model.TestData().WithSeed(17).GenerateMany(new TypeId("Root"), 2);

        _ = await Assert.That(first).IsEquivalentTo(second);
        _ = await Assert.That(first).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Canonical_property_generator_is_used_without_a_clr_type()
    {
        TypeSchemaModel model = Model();
        SemanticTestValue value = model.TestData()
            .WithPropertyGenerator(new TypeId("Root"), new PropertyId("Name"), _ => "authored")
            .Generate(new TypeId("Root"));

        var root = (ObjectTestValue)value;
        _ = await Assert.That(((ScalarTestValue)root.Properties[new PropertyId("Name")]).Value).IsEqualTo("authored");
    }

    private static TypeSchemaModel Model()
    {
        ScalarTypeDefinition text = new() { Id = new("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ObjectTypeDefinition root = new()
        {
            Id = new("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [],
            Properties = [new PropertyDefinition { Id = new("Name"), Name = "Name", Type = new TypeRef(text.Id), Cardinality = new() { IsRequired = true }, Constraints = new(), Annotations = new() }],
        };
        return new TypeSchemaModel { Id = new("m0080"), Types = [root, text], TypesById = new Dictionary<TypeId, TypeDefinition> { [root.Id] = root, [text.Id] = text }, Annotations = new() };
    }
}
