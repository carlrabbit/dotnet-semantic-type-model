using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, IDE0005, IDE0007, IDE0022, IDE0055, IDE0300, IDE0305, CA1707
public class M0084CoordinatedGenerationTests
{
    [Test]
    public async Task Batch_sequence_and_shared_values_use_one_invocation_session()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition number = root.Properties.Single(p => p.Id == new PropertyId("Number"));
        PropertyDefinition tenant = root.Properties.Single(p => p.Id == new PropertyId("Tenant"));
        TestDataProfile profile = TestDataProfile.Create(model, "coordinated")
            .For(root).Property(number).Sequence(TestDataValueScope.Batch, i => $"N{i}").Done()
            .Property(tenant).Shared(TestDataValueScope.Batch).Done().Build();

        IReadOnlyList<SemanticTestValue> values = model.TestData().WithProfile(profile).WithSeed(4).GenerateMany(new TypeId("Root"), 3);
        ObjectTestValue[] roots = values.Cast<ObjectTestValue>().ToArray();
        _ = await Assert.That(((ScalarTestValue)roots[0].Properties[number.Id]).Value).IsEqualTo("N0");
        _ = await Assert.That(((ScalarTestValue)roots[2].Properties[number.Id]).Value).IsEqualTo("N2");
        _ = await Assert.That(((ScalarTestValue)roots[0].Properties[tenant.Id]).Value).IsEqualTo(((ScalarTestValue)roots[2].Properties[tenant.Id]).Value);
        IReadOnlyList<SemanticTestValue> replay = model.TestData().WithProfile(profile).WithSeed(4).GenerateMany(new TypeId("Root"), 1);
        _ = await Assert.That(((ScalarTestValue)((ObjectTestValue)replay[0]).Properties[number.Id]).Value).IsEqualTo("N0");
    }

    [Test]
    public async Task Derived_values_follow_dependency_order_and_declared_context()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition first = root.Properties.Single(p => p.Id == new PropertyId("First"));
        PropertyDefinition last = root.Properties.Single(p => p.Id == new PropertyId("Last"));
        PropertyDefinition full = root.Properties.Single(p => p.Id == new PropertyId("Full"));
        TestDataProfile profile = TestDataProfile.Create(model, "derived")
            .For(root).Property(full).From(new[] { first.Id, last.Id }, c => $"{c.Get<string>(first.Id)}-{c.Get<string>(last.Id)}").Done().Build();

        ObjectTestValue value = (ObjectTestValue)model.TestData().WithProfile(profile).Generate(new TypeId("Root"));
        string firstValue = ((ScalarTestValue)value.Properties[first.Id]).Value?.ToString()!;
        string lastValue = ((ScalarTestValue)value.Properties[last.Id]).Value?.ToString()!;
        _ = await Assert.That(((ScalarTestValue)value.Properties[full.Id]).Value).IsEqualTo($"{firstValue}-{lastValue}");
    }

    [Test]
    public async Task Invalid_coordination_combinations_fail_at_profile_build()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition first = root.Properties.Single(p => p.Id == new PropertyId("First"));
        _ = await Assert.That(() => TestDataProfile.Create(model, "invalid").For(root).Property(first).Shared(TestDataValueScope.Batch).Unique(TestDataValueScope.Batch).Done().Build()).Throws<ArgumentException>();
    }

    private static TypeSchemaModel Model()
    {
        ScalarTypeDefinition text = new() { Id = new TypeId("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ObjectTypeDefinition root = new()
        {
            Id = new TypeId("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [],
            Properties =
            [
                Property("First", text.Id), Property("Last", text.Id), Property("Full", text.Id),
                Property("Number", text.Id), Property("Tenant", text.Id)
            ]
        };
        TypeDefinition[] types = [root, text];
        return new TypeSchemaModel { Id = new SchemaModelId("m0084"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static PropertyDefinition Property(string id, TypeId type) => new()
    { Id = new PropertyId(id), Name = id, Type = new(type), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(), Annotations = new() };
}
