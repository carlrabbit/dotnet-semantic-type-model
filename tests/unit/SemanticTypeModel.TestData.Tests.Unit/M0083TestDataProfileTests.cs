using SemanticTypeModel.Abstractions.Model;
namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, CA1707, IDE0005, IDE0007, IDE0022, IDE0055, IDE0058
public class M0083TestDataProfileTests
{
    [Test]
    public async Task Profile_applies_model_bound_presence_null_weight_and_collection_rules()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition status = root.Properties.Single(p => p.Id == new PropertyId("Status"));
        PropertyDefinition comment = root.Properties.Single(p => p.Id == new PropertyId("Comment"));
        PropertyDefinition tags = root.Properties.Single(p => p.Id == new PropertyId("Tags"));

        TestDataProfile profile = TestDataProfile.Create(model, "Typical")
            .Defaults(d => d.NullProbability(1))
            .For(root)
                .Property(status).Weighted(("Active", 1)).Done()
                .Property(comment).Presence(1).Done()
                .Property(tags).FixedCount(3).Done()
            .Done()
            .Build();

        ObjectTestValue value = (ObjectTestValue)model.TestData().WithProfile(profile).WithSeed(7).Generate(new TypeId("Root"));
        _ = await Assert.That(value.Properties[status.Id]).IsTypeOf<ScalarTestValue>();
        _ = await Assert.That(((ScalarTestValue)value.Properties[status.Id]).Value).IsEqualTo("Active");
        _ = await Assert.That(value.Properties[comment.Id]).IsTypeOf<NullTestValue>();
        _ = await Assert.That(((ArrayTestValue)value.Properties[tags.Id]).Items.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Profile_is_immutable_model_bound_and_composes_explicitly()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition comment = root.Properties.Single(p => p.Id == new PropertyId("Comment"));
        TestDataProfile first = TestDataProfile.Create(model, "first").For(root).Property(comment).Presence(0).Done().Done().Build();
        TestDataProfile second = TestDataProfile.Create(model, "second").For(root).Property(comment).Presence(1).Done().Done().Build();
        TestDataProfile combined = TestDataProfile.Compose("combined", first, second);

        ObjectTestValue value = (ObjectTestValue)model.TestData().WithProfile(combined).Generate(new TypeId("Root"));
        _ = await Assert.That(value.Properties.ContainsKey(comment.Id)).IsTrue();
        _ = await Assert.That(() => model.TestData().WithProfile(TestDataProfile.Create(OtherModel(), "wrong").Build())).Throws<ArgumentException>();
    }

    [Test]
    public async Task Invalid_exact_profile_rules_fail_during_build()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition required = root.Properties.Single(p => p.Id == new PropertyId("Status"));

        _ = await Assert.That(() => TestDataProfile.Create(model, "invalid").For(root).Property(required).Presence(0).Done().Done().Build()).Throws<ArgumentException>();
    }

    private static TypeSchemaModel OtherModel()
    {
        TypeSchemaModel model = Model();
        return new TypeSchemaModel { Id = new SchemaModelId("other"), Types = model.Types, TypesById = model.TypesById, Annotations = model.Annotations };
    }

    private static TypeSchemaModel Model()
    {
        ScalarTypeDefinition text = new() { Id = new TypeId("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ArrayTypeDefinition tags = new() { Id = new TypeId("Tags"), Name = "Tags", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new(text.Id), MinItems = 0, MaxItems = 10 };
        ObjectTypeDefinition root = new()
        {
            Id = new TypeId("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [],
            Properties =
            [
                Property("Status", text.Id, required: true),
                Property("Comment", text.Id, required: false, nullable: true),
                Property("Tags", tags.Id, required: true),
            ]
        };
        TypeDefinition[] types = [root, text, tags];
        return new TypeSchemaModel { Id = new SchemaModelId("m0083"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static PropertyDefinition Property(string id, TypeId type, bool required, bool nullable = false) => new()
    {
        Id = new PropertyId(id), Name = id, Type = new(type), Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable }, Constraints = new(), Annotations = new()
    };
}
