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

    [Test]
    public async Task Composition_overlays_coordination_fields_and_revalidates_the_final_graph()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition first = root.Properties.Single(p => p.Id == new PropertyId("First"));
        PropertyDefinition full = root.Properties.Single(p => p.Id == new PropertyId("Full"));

        TestDataProfile derived = TestDataProfile.Create(model, "derived")
            .For(root).Property(full).From(new[] { first.Id }, c => c.Get<string>(first.Id)).Done().Build();
        TestDataProfile unique = TestDataProfile.Create(model, "unique")
            .For(root).Property(full).Unique(TestDataValueScope.Batch).Done().Build();
        TestDataProfile combined = TestDataProfile.Compose("combined", derived, unique);
        _ = await Assert.That(((ScalarTestValue)((ObjectTestValue)model.TestData().WithProfile(combined).Generate(new TypeId("Root"))).Properties[full.Id]).Value).IsNotNull();

        TestDataProfile shared = TestDataProfile.Create(model, "shared")
            .For(root).Property(full).Shared(TestDataValueScope.Batch).Done().Build();
        _ = await Assert.That(() => TestDataProfile.Compose("contradictory", derived, shared)).Throws<ArgumentException>();

        TestDataProfile cycleA = TestDataProfile.Create(model, "cycle-a")
            .For(root).Property(full).From(new[] { first.Id }, c => c.Get<string>(first.Id)).Done().Build();
        TestDataProfile cycleB = TestDataProfile.Create(model, "cycle-b")
            .For(root).Property(first).From(new[] { full.Id }, c => c.Get<string>(full.Id)).Done().Build();
        _ = await Assert.That(() => TestDataProfile.Compose("cycle", cycleA, cycleB)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Enum_sources_use_profile_coordinated_and_programmatic_precedence()
    {
        TypeSchemaModel model = EnumModel();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("EnumRoot"));
        PropertyDefinition status = root.Properties.Single(p => p.Id == new PropertyId("Status"));
        TestDataProfile weighted = TestDataProfile.Create(model, "weighted")
            .For(root).Property(status).Weighted(("ready", 1)).Done().Build();
        EnumTestValue weightedValue = (EnumTestValue)((ObjectTestValue)model.TestData().WithProfile(weighted).Generate(root.Id)).Properties[status.Id];
        _ = await Assert.That(weightedValue.Value).IsEqualTo("ready");

        TestDataProfile derived = TestDataProfile.Create(model, "derived")
            .For(root).Property(status).From([], _ => "queued").Done().Build();
        EnumTestValue derivedValue = (EnumTestValue)((ObjectTestValue)model.TestData().WithProfile(derived).Generate(root.Id)).Properties[status.Id];
        _ = await Assert.That(derivedValue.Value).IsEqualTo("queued");

        TestDataProfile sequence = TestDataProfile.Create(model, "sequence")
            .For(root).Property(status).Sequence(TestDataValueScope.Batch, i => i == 0 ? "ready" : "done").Done().Build();
        EnumTestValue sequenceValue = (EnumTestValue)((ObjectTestValue)model.TestData().WithProfile(sequence).Generate(root.Id)).Properties[status.Id];
        _ = await Assert.That(sequenceValue.Value).IsEqualTo("ready");

        EnumTestValue programmatic = (EnumTestValue)((ObjectTestValue)model.TestData().WithProfile(sequence).WithPropertyGenerator(root.Id, status.Id, _ => "done").Generate(root.Id)).Properties[status.Id];
        _ = await Assert.That(programmatic.Value).IsEqualTo("done");
        _ = await Assert.That(() => TestDataProfile.Create(model, "invalid-enum").For(root).Property(status).Weighted(("missing", 1)).Done().Build()).Throws<ArgumentException>();
    }

    [Test]
    public async Task Inherited_dependencies_are_validated_and_planned_with_effective_properties()
    {
        TypeSchemaModel model = InheritedModel();
        ObjectTypeDefinition derived = (ObjectTypeDefinition)model.GetType(new TypeId("Derived"));
        PropertyDefinition first = ((ObjectTypeDefinition)model.GetType(new TypeId("Base"))).Properties.Single();
        PropertyDefinition full = derived.Properties.Single();
        TestDataProfile profile = TestDataProfile.Create(model, "inherited")
            .For(derived).Property(full).From(new[] { first.Id }, c => $"derived:{c.Get<string>(first.Id)}").Done().Build();
        ObjectTestValue value = (ObjectTestValue)model.TestData().WithProfile(profile).Generate(derived.Id);
        _ = await Assert.That(((ScalarTestValue)value.Properties[full.Id]).Value?.ToString()).StartsWith("derived:");

        PropertyDefinition onlyDerived = derived.Properties.Single();
        _ = await Assert.That(() => TestDataProfile.Create(model, "invalid-inherited")
            .For((ObjectTypeDefinition)model.GetType(new TypeId("Base"))).Property(first)
            .From(new[] { onlyDerived.Id }, _ => "invalid").Done().Build()).Throws<ArgumentException>();
    }

    [Test]
    public async Task Coordination_diagnostics_are_specific_and_do_not_fall_through()
    {
        TypeSchemaModel model = Model();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("Root"));
        PropertyDefinition first = root.Properties.Single(p => p.Id == new PropertyId("First"));
        PropertyDefinition full = root.Properties.Single(p => p.Id == new PropertyId("Full"));

        TestDataProfile callback = TestDataProfile.Create(model, "callback")
            .For(root).Property(full).From(new[] { first.Id }, _ => throw new InvalidOperationException("boom")).Done().Build();
        TestDataGenerationException callbackError = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithProfile(callback).Generate(root.Id));
        _ = await Assert.That(callbackError.Diagnostics.Any(d => d.Code == "TESTDATA_COORDINATION_CALLBACK_FAILED")).IsTrue();

        TestDataProfile invalid = TestDataProfile.Create(model, "invalid")
            .For(root).Property(full).From(new[] { first.Id }, _ => null).Done().Build();
        TestDataGenerationException invalidError = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithProfile(invalid).Generate(root.Id));
        _ = await Assert.That(invalidError.Diagnostics.Any(d => d.Code == "TESTDATA_COORDINATION_CANDIDATE_INVALID")).IsTrue();

        TestDataProfile undeclared = TestDataProfile.Create(model, "undeclared")
            .For(root).Property(full).From(new[] { first.Id }, c => c.Get<string>(new PropertyId("Last"))).Done().Build();
        TestDataGenerationException undeclaredError = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithProfile(undeclared).Generate(root.Id));
        _ = await Assert.That(undeclaredError.Diagnostics.Any(d => d.Code == "TESTDATA_COORDINATION_DEPENDENCY_UNDECLARED")).IsTrue();

        TestDataProfile incompatible = TestDataProfile.Create(model, "incompatible")
            .For(root).Property(full).From([first.Id], c => c.TryGet<int>(first.Id, out int value) ? value : "fallback").Done().Build();
        TestDataGenerationException incompatibleError = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithProfile(incompatible).Generate(root.Id));
        _ = await Assert.That(incompatibleError.Diagnostics.Any(d => d.Code == "TESTDATA_COORDINATION_CALLBACK_FAILED")).IsTrue();

        TypeSchemaModel optionalModel = OptionalDependencyModel();
        ObjectTypeDefinition optionalRoot = (ObjectTypeDefinition)optionalModel.GetType(new TypeId("OptionalDependencyRoot"));
        PropertyDefinition dependency = optionalRoot.Properties.Single(p => p.Id == new PropertyId("Dependency"));
        PropertyDefinition target = optionalRoot.Properties.Single(p => p.Id == new PropertyId("Target"));
        TestDataProfile unavailable = TestDataProfile.Create(optionalModel, "unavailable")
            .For(optionalRoot).Property(dependency).NullProbability(1).Done()
            .Property(target).From(new[] { dependency.Id }, c => c.Get<string>(dependency.Id)).Done().Build();
        TestDataGenerationException unavailableError = Assert.Throws<TestDataGenerationException>(() => optionalModel.TestData().WithProfile(unavailable).Generate(optionalRoot.Id));
        _ = await Assert.That(unavailableError.Diagnostics.Any(d => d.Code == "TESTDATA_COORDINATION_DEPENDENCY_UNAVAILABLE")).IsTrue();
    }

    [Test]
    public async Task Sequence_omission_and_null_do_not_consume_indexes_and_invocations_are_isolated()
    {
        TypeSchemaModel model = OptionalSequenceModel();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("OptionalRoot"));
        PropertyDefinition sequence = root.Properties.Single(p => p.Id == new PropertyId("Sequence"));
        TestDataProfile profile = TestDataProfile.Create(model, "sequence")
            .For(root).Property(sequence).Presence(0).Sequence(TestDataValueScope.Batch, i => $"S{i}").Done().Build();
        ObjectTestValue omitted = (ObjectTestValue)model.TestData().WithProfile(profile).GenerateMany(root.Id, 1)[0];
        _ = await Assert.That(omitted.Properties.ContainsKey(sequence.Id)).IsFalse();

        TestDataProfile present = TestDataProfile.Create(model, "present")
            .For(root).Property(sequence).Sequence(TestDataValueScope.Batch, i => $"S{i}").Done().Build();
        IReadOnlyList<SemanticTestValue> first = model.TestData().WithProfile(present).GenerateMany(root.Id, 2);
        IReadOnlyList<SemanticTestValue> replay = model.TestData().WithProfile(present).GenerateMany(root.Id, 1);
        _ = await Assert.That(((ScalarTestValue)((ObjectTestValue)first[0]).Properties[sequence.Id]).Value).IsEqualTo("S0");
        _ = await Assert.That(((ScalarTestValue)((ObjectTestValue)first[1]).Properties[sequence.Id]).Value).IsEqualTo("S1");
        _ = await Assert.That(((ScalarTestValue)((ObjectTestValue)replay[0]).Properties[sequence.Id]).Value).IsEqualTo("S0");
    }

    [Test]
    public async Task Binary_uniqueness_uses_content_and_finite_domains_exhaust_diagnostically()
    {
        TypeSchemaModel model = BinaryModel();
        ObjectTypeDefinition root = (ObjectTypeDefinition)model.GetType(new TypeId("BinaryRoot"));
        PropertyDefinition value = root.Properties.Single();
        TestDataProfile alternatives = TestDataProfile.Create(model, "alternatives")
            .For(root).Property(value).Weighted((Convert.ToBase64String([1]), 1), (Convert.ToBase64String([2]), 1)).Unique(TestDataValueScope.Batch).Done().Build();
        IReadOnlyList<SemanticTestValue> generated = model.TestData().WithProfile(alternatives).GenerateMany(root.Id, 2);
        byte[] first = (byte[])((ScalarTestValue)((ObjectTestValue)generated[0]).Properties[value.Id]).Value!;
        byte[] second = (byte[])((ScalarTestValue)((ObjectTestValue)generated[1]).Properties[value.Id]).Value!;
        _ = await Assert.That(Convert.ToBase64String(first)).IsNotEqualTo(Convert.ToBase64String(second));

        TestDataProfile exhausted = TestDataProfile.Create(model, "exhausted")
            .For(root).Property(value).Weighted((Convert.ToBase64String([1]), 1)).Unique(TestDataValueScope.Batch).Done().Build();
        TestDataGenerationException exception = Assert.Throws<TestDataGenerationException>(() => model.TestData().WithProfile(exhausted).GenerateMany(root.Id, 2));
        _ = await Assert.That(exception.Diagnostics.Any(d => d.Code == "TESTDATA_COORDINATION_UNIQUENESS_EXHAUSTED")).IsTrue();
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

    private static TypeSchemaModel EnumModel()
    {
        EnumTypeDefinition status = new() { Id = new TypeId("Status"), Name = "Status", Kind = TypeKind.Enum, Nullability = Nullability.NonNullable, Annotations = new(), StorageKind = EnumStorageKind.String, Values = [new EnumValueDefinition { Name = "Ready", Value = "ready", Annotations = new() }, new EnumValueDefinition { Name = "Queued", Value = "queued", Annotations = new() }, new EnumValueDefinition { Name = "Done", Value = "done", Annotations = new() }] };
        ObjectTypeDefinition root = new() { Id = new TypeId("EnumRoot"), Name = "EnumRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Properties = [Property("Status", status.Id)] };
        TypeDefinition[] types = [root, status];
        return new TypeSchemaModel { Id = new SchemaModelId("enum-m0084"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static TypeSchemaModel InheritedModel()
    {
        ScalarTypeDefinition text = new() { Id = new TypeId("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ObjectTypeDefinition baseObject = new() { Id = new TypeId("Base"), Name = "Base", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Properties = [Property("First", text.Id)] };
        ObjectTypeDefinition derived = new() { Id = new TypeId("Derived"), Name = "Derived", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Composition = new() { AllOf = [new TypeRef(baseObject.Id)] }, Properties = [Property("Full", text.Id)] };
        TypeDefinition[] types = [baseObject, derived, text];
        return new TypeSchemaModel { Id = new SchemaModelId("inheritance-m0084"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static TypeSchemaModel OptionalSequenceModel()
    {
        ScalarTypeDefinition text = new() { Id = new TypeId("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ObjectTypeDefinition root = new() { Id = new TypeId("OptionalRoot"), Name = "OptionalRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Properties = [new PropertyDefinition { Id = new PropertyId("Sequence"), Name = "Sequence", Type = new(text.Id), Cardinality = new Cardinality { IsRequired = false, AllowsNull = true }, Constraints = new(), Annotations = new() }] };
        TypeDefinition[] types = [root, text];
        return new TypeSchemaModel { Id = new SchemaModelId("optional-m0084"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static TypeSchemaModel BinaryModel()
    {
        ScalarTypeDefinition binary = new() { Id = new TypeId("Binary"), Name = "Binary", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.Binary };
        ObjectTypeDefinition root = new() { Id = new TypeId("BinaryRoot"), Name = "BinaryRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Properties = [Property("Value", binary.Id)] };
        TypeDefinition[] types = [root, binary];
        return new TypeSchemaModel { Id = new SchemaModelId("binary-m0084"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static TypeSchemaModel OptionalDependencyModel()
    {
        ScalarTypeDefinition text = new() { Id = new TypeId("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ObjectTypeDefinition root = new() { Id = new TypeId("OptionalDependencyRoot"), Name = "OptionalDependencyRoot", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Properties =
        [
            new PropertyDefinition { Id = new PropertyId("Dependency"), Name = "Dependency", Type = new(text.Id), Cardinality = new Cardinality { IsRequired = false, AllowsNull = true }, Constraints = new(), Annotations = new() },
            Property("Target", text.Id)
        ] };
        TypeDefinition[] types = [root, text];
        return new TypeSchemaModel { Id = new SchemaModelId("optional-dependency-m0084"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static PropertyDefinition Property(string id, TypeId type) => new()
    { Id = new PropertyId(id), Name = id, Type = new(type), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(), Annotations = new() };
}
