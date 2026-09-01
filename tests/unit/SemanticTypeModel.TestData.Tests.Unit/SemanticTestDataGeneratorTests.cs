using SemanticTypeModel.Abstractions.Model;
namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class SemanticTestDataGeneratorTests
{
    [Test]
    public async Task Generates_deterministic_bounded_object_with_constraints()
    {
        TypeSchemaModel model = CreateModel();
        TestDataGenerationResult first = SemanticTestDataGenerator.Generate(model, new TypeId("Root"), TestDataSizeProfile.Moderate);
        TestDataGenerationResult second = SemanticTestDataGenerator.Generate(model, new TypeId("Root"), TestDataSizeProfile.Moderate);

        _ = await Assert.That(first.Succeeded).IsTrue();
        var root = (ObjectTestValue)first.Value!;
        var repeatedRoot = (ObjectTestValue)second.Value!;
        _ = await Assert.That(((ScalarTestValue)root.Properties[new PropertyId("Text")]).Value).IsEqualTo(((ScalarTestValue)repeatedRoot.Properties[new PropertyId("Text")]).Value);
        _ = await Assert.That(((ArrayTestValue)root.Properties[new PropertyId("Items")]).Items.Select(item => ((EnumTestValue)item).Value)).IsEquivalentTo(((ArrayTestValue)repeatedRoot.Properties[new PropertyId("Items")]).Items.Select(item => ((EnumTestValue)item).Value));
        _ = await Assert.That(root.Properties.Count).IsEqualTo(4);
        var text = (ScalarTestValue)root.Properties[new PropertyId("Text")];
        _ = await Assert.That(((string)text.Value!).Length).IsBetween(4, 12);
        var items = (ArrayTestValue)root.Properties[new PropertyId("Items")];
        _ = await Assert.That(items.Items.Count).IsEqualTo(3);
        _ = await Assert.That(items.Items.Select(item => ((EnumTestValue)item).Value).Distinct().Count()).IsEqualTo(3);
    }

    [Test]
    public async Task Generates_enum_and_strong_scalar_identity()
    {
        TypeSchemaModel model = CreateModel();
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, new TypeId("Root"));
        var root = (ObjectTestValue)result.Value!;
        _ = await Assert.That(root.Properties[new PropertyId("Status")]).IsTypeOf<EnumTestValue>();
        var strong = (StrongScalarTestValue)root.Properties[new PropertyId("Code")];
        _ = await Assert.That(strong.StrongTypeId).IsEqualTo(new TypeId("Code"));
        _ = await Assert.That(strong.Value).IsTypeOf<ScalarTestValue>();
    }

    [Test]
    public async Task Rejects_patterns_unknown_formats_and_unsatisfiable_ranges()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String, format: "custom");
        TypeSchemaModel model = Model(text);
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, text.Id);
        _ = await Assert.That(result.Succeeded).IsFalse();
        _ = await Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code)).Contains("TESTDATA_FORMAT_UNSUPPORTED");

        ScalarTypeDefinition patterned = Scalar("Patterned", ScalarKind.String);
        var patternRoot = new ObjectTypeDefinition { Id = new("PatternRoot"), Name = "PatternRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Empty, Keys = [], Properties = [Property("Value", patterned.Id, false, new ConstraintSet { String = new StringConstraints { Pattern = "^[a-z]+$" } })] };
        result = SemanticTestDataGenerator.Generate(Model(patternRoot, patterned), patternRoot.Id);
        _ = await Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code)).Contains("TESTDATA_PATTERN_UNSUPPORTED");

        ScalarTypeDefinition impossible = Scalar("Impossible", ScalarKind.Integer);
        var impossibleRoot = new ObjectTypeDefinition { Id = new("ImpossibleRoot"), Name = "ImpossibleRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Empty, Keys = [], Properties = [Property("Value", impossible.Id, false, new ConstraintSet { Numeric = new NumericConstraints { Minimum = 10, Maximum = 5 } })] };
        result = SemanticTestDataGenerator.Generate(Model(impossibleRoot, impossible), impossibleRoot.Id);
        _ = await Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code)).Contains("TESTDATA_UNSATISFIABLE_CONSTRAINTS");
    }

    [Test]
    public async Task Rejects_unknown_never_union_and_unresolved_reference()
    {
        ScalarTypeDefinition unknown = Scalar("Unknown", ScalarKind.Unknown);
        var never = new Leaf("Never", TypeKind.Never);
        var union = new UnionTypeDefinition { Id = new("Union"), Name = "Union", Kind = TypeKind.Union, Nullability = Nullability.NonNullable, Annotations = Empty, Options = [new TypeRef(unknown.Id)] };
        var reference = new ReferenceTypeDefinition { Id = new("Ref"), Name = "Ref", Kind = TypeKind.Reference, Nullability = Nullability.NonNullable, Annotations = Empty, Target = new TypeRef(new TypeId("Missing")) };
        TypeSchemaModel model = Model(unknown, never, union, reference);
        _ = await Assert.That(SemanticTestDataGenerator.Generate(model, unknown.Id).Diagnostics[0].Code).IsEqualTo("TESTDATA_UNSUPPORTED_SCALAR");
        _ = await Assert.That(SemanticTestDataGenerator.Generate(model, never.Id).Diagnostics[0].Code).IsEqualTo("TESTDATA_UNSUPPORTED_TYPE");
        _ = await Assert.That(SemanticTestDataGenerator.Generate(model, union.Id).Diagnostics[0].Code).IsEqualTo("TESTDATA_UNSUPPORTED_TYPE");
        _ = await Assert.That(SemanticTestDataGenerator.Generate(model, reference.Id).Diagnostics[0].Code).IsEqualTo("TESTDATA_UNRESOLVED_REFERENCE");
    }

    [Test]
    public async Task Consumes_real_generated_model_provider()
    {
        TypeSchemaModel model = TestModels.ModelA.Generated.ModelASemanticTypeModel.Create();
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, new TypeId("global::SemanticTypeModel.TestModels.ModelA.TestDataScenario"));
        _ = await Assert.That(result.HasErrors).IsFalse();
        var generated = (ObjectTestValue)result.Value!;
        _ = await Assert.That(generated.Properties.Count).IsEqualTo(3);
        _ = await Assert.That(generated.Properties.Values.OfType<ArrayTestValue>().Count()).IsGreaterThan(0);
        _ = await Assert.That(generated.Properties.Values.OfType<EnumTestValue>().Count()).IsGreaterThan(0);
        _ = await Assert.That(generated.Properties.Values.OfType<StrongScalarTestValue>().Count()).IsGreaterThan(0);
    }

    private static TypeSchemaModel CreateModel()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ScalarTypeDefinition integer = Scalar("Integer", ScalarKind.Integer);
        var status = new EnumTypeDefinition { Id = new("Status"), Name = "Status", Kind = TypeKind.Enum, Nullability = Nullability.NonNullable, Annotations = Empty, StorageKind = EnumStorageKind.String, Values = [new EnumValueDefinition { Name = "Ready", Value = "ready", Annotations = Empty }, new EnumValueDefinition { Name = "Queued", Value = "queued", Annotations = Empty }, new EnumValueDefinition { Name = "Done", Value = "done", Annotations = Empty }] };
        var code = new StrongScalarTypeDefinition { Id = new("Code"), Name = "Code", Kind = TypeKind.StrongScalar, Nullability = Nullability.NonNullable, Annotations = Empty, ValueType = new TypeRef(text.Id) };
        var items = new ArrayTypeDefinition { Id = new("Items"), Name = "Items", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = Empty, ItemType = new TypeRef(status.Id), MinItems = 3, MaxItems = 3, UniqueItems = true };
        var root = new ObjectTypeDefinition
        {
            Id = new("Root"),
            Name = "Root",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Empty,
            Keys = [],
            Properties =
        [
            Property("Text", text.Id, false, new ConstraintSet { String = new StringConstraints { MinLength = 4, MaxLength = 12 } }),
            Property("Items", items.Id, false),
            Property("Status", status.Id, false),
            Property("Code", code.Id, false)
        ]
        };
        return Model(root, text, integer, status, code, items);
    }

    private static PropertyDefinition Property(string name, TypeId type, bool nullable, ConstraintSet? constraints = null)
    {
        return new() { Id = new(name), Name = name, Type = new TypeRef(type), Cardinality = new Cardinality { IsRequired = !nullable, AllowsNull = nullable }, Constraints = constraints ?? new(), Annotations = Empty };
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind, string? format = null)
    {
        return new() { Id = new(id), Name = id, Kind = TypeKind.Scalar, ScalarKind = kind, Format = format, Nullability = Nullability.NonNullable, Annotations = Empty };
    }

    private static TypeSchemaModel Model(params TypeDefinition[] types)
    {
        return new() { Id = new("test"), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = Empty };
    }

    private static readonly AnnotationBag Empty = new();
    private sealed record Leaf : TypeDefinition
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public Leaf(string id, TypeKind kind)
        {
            Id = new TypeId(id);
            Name = id;
            Kind = kind;
            Nullability = Nullability.NonNullable;
            Annotations = Empty;
        }
    }
}
