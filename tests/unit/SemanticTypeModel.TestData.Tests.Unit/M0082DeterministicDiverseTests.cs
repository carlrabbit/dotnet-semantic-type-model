using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData.Tests.Unit;

#pragma warning disable CS1591, CA1707, IDE0007, IDE0022
public sealed class M0082DeterministicDiverseTests
{
    [Test]
    public async Task GenerateMany_uses_root_ordinal_for_distinct_high_cardinality_values()
    {
        TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.String);
        IReadOnlyList<SemanticTestValue> values = model.TestData().WithSeed(42).GenerateMany(new TypeId("Root"), 100);
        string[] strings = [.. values.Cast<ObjectTestValue>().Select(value => (string)((ScalarTestValue)value.Properties[new PropertyId("Value")]).Value!)];
        _ = await Assert.That(strings.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(100);

        IReadOnlyList<SemanticTestValue> guidValues = RootWithScalar("Root", "Value", ScalarKind.Guid).TestData().WithSeed(42).GenerateMany(new TypeId("Root"), 100);
        string[] guids = [.. guidValues.Cast<ObjectTestValue>().Select(value => Fingerprint((ScalarTestValue)value.Properties[new PropertyId("Value")]))];
        _ = await Assert.That(guids.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(100);
    }

    [Test]
    public async Task Ordinary_sibling_properties_are_distinct_and_seed_changes_a_high_cardinality_value()
    {
        TypeSchemaModel model = RootWithScalar("Root", "First", ScalarKind.String, null, Property("Second", new TypeId("Scalar")));
        var first = (ObjectTestValue)model.TestData().WithSeed(7).Generate(new TypeId("Root"));
        var changedSeed = (ObjectTestValue)model.TestData().WithSeed(8).Generate(new TypeId("Root"));
        string firstValue = (string)((ScalarTestValue)first.Properties[new PropertyId("First")]).Value!;
        string secondValue = (string)((ScalarTestValue)first.Properties[new PropertyId("Second")]).Value!;
        string changedValue = (string)((ScalarTestValue)changedSeed.Properties[new PropertyId("First")]).Value!;
        _ = await Assert.That(firstValue).IsNotEqualTo(secondValue);
        _ = await Assert.That(firstValue).IsNotEqualTo(changedValue);
    }

    [Test]
    public async Task Fixed_coordinate_has_a_stable_platform_neutral_output_vector()
    {
        TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.String);
        string actual = Text(model.TestData().WithSeed(42).Generate(new TypeId("Root")));
        _ = await Assert.That(actual).IsEqualTo("twgaqrfl");
    }

    [Test]
    public async Task Unrelated_sibling_changes_do_not_perturb_existing_values_or_callback_seed()
    {
        TypeSchemaModel first = RootWithScalar("Root", "Value", ScalarKind.String);
        TypeSchemaModel second = RootWithScalar("Root", "Value", ScalarKind.String, null, Property("Other", new TypeId("OtherScalar")));
        var firstSeed = 0;
        var secondSeed = 0;
        SemanticTestValue firstValue = first.TestData().WithSeed(7).WithPropertyGenerator(new TypeId("Root"), new PropertyId("Value"), context => { firstSeed = context.Seed; return null; }).Generate(new TypeId("Root"));
        SemanticTestValue secondValue = second.TestData().WithSeed(7).WithPropertyGenerator(new TypeId("Root"), new PropertyId("Value"), context => { secondSeed = context.Seed; return null; }).Generate(new TypeId("Root"));
        var firstText = (string)((ScalarTestValue)((ObjectTestValue)firstValue).Properties[new PropertyId("Value")]).Value!;
        var secondText = (string)((ScalarTestValue)((ObjectTestValue)secondValue).Properties[new PropertyId("Value")]).Value!;
        _ = await Assert.That(secondText).IsEqualTo(firstText);
        _ = await Assert.That(secondSeed).IsEqualTo(firstSeed);

        TypeSchemaModel reordered = RootWithScalar("Root", "Value", ScalarKind.String, null, Property("Other", new TypeId("OtherScalar")));
        ObjectTypeDefinition reorderedRoot = (ObjectTypeDefinition)reordered.TypesById[new TypeId("Root")];
        reorderedRoot = new ObjectTypeDefinition { Id = reorderedRoot.Id, Name = reorderedRoot.Name, Kind = reorderedRoot.Kind, Nullability = reorderedRoot.Nullability, Annotations = reorderedRoot.Annotations, Properties = [reorderedRoot.Properties[1], reorderedRoot.Properties[0]], Keys = reorderedRoot.Keys };
        reordered = new TypeSchemaModel { Id = reordered.Id, Types = [.. reordered.Types.Select(type => type.Id == reorderedRoot.Id ? reorderedRoot : type)], TypesById = new Dictionary<TypeId, TypeDefinition>(reordered.TypesById) { [reorderedRoot.Id] = reorderedRoot }, Annotations = reordered.Annotations };
        SemanticTestValue reorderedValue = reordered.TestData().WithSeed(7).Generate(new TypeId("Root"));
        string reorderedText = (string)((ScalarTestValue)((ObjectTestValue)reorderedValue).Properties[new PropertyId("Value")]).Value!;
        _ = await Assert.That(reorderedText).IsEqualTo(firstText);
    }

    [Test]
    public async Task Profiles_use_category_specific_targets()
    {
        TypeSchemaModel scalar = RootWithScalar("Root", "Value", ScalarKind.String);
        foreach (ValueTuple<TestDataSizeProfile, int> profileAndLength in new (TestDataSizeProfile, int)[] { (TestDataSizeProfile.Simple, 8), (TestDataSizeProfile.Moderate, 32), (TestDataSizeProfile.Extreme, 1024) })
        {
            TestDataSizeProfile profile = profileAndLength.Item1;
            int length = profileAndLength.Item2;
            var value = (ObjectTestValue)SemanticTestDataGenerator.Generate(scalar, new TypeId("Root"), profile, 1).Value!;
            _ = await Assert.That(((string)((ScalarTestValue)value.Properties[new PropertyId("Value")]).Value!).Length).IsEqualTo(length);
        }

        foreach ((TestDataSizeProfile profile, int expected) in new (TestDataSizeProfile, int)[] { (TestDataSizeProfile.Simple, 8), (TestDataSizeProfile.Moderate, 32), (TestDataSizeProfile.Extreme, 1024) })
        {
            TypeSchemaModel binaryModel = RootWithScalar("Root", "Value", ScalarKind.Binary);
            var generated = (ObjectTestValue)SemanticTestDataGenerator.Generate(binaryModel, new TypeId("Root"), profile, 2).Value!;
            _ = await Assert.That(((byte[])((ScalarTestValue)generated.Properties[new PropertyId("Value")]).Value!).Length).IsEqualTo(expected);
        }

        foreach ((TestDataSizeProfile profile, int expected) in new (TestDataSizeProfile, int)[] { (TestDataSizeProfile.Simple, 1), (TestDataSizeProfile.Moderate, 8), (TestDataSizeProfile.Extreme, 100) })
        {
            TypeSchemaModel arrayModel = RootWithCollection("ArrayRoot", false);
            TestDataGenerationResult arrayResult = SemanticTestDataGenerator.Generate(arrayModel, new TypeId("ArrayRoot"), profile, 2);
            _ = await Assert.That(arrayResult.Succeeded).IsTrue();
            var arrayRoot = (ObjectTestValue)arrayResult.Value!;
            _ = await Assert.That(((ArrayTestValue)arrayRoot.Properties[new PropertyId("Value")]).Items.Count).IsEqualTo(expected);
            TypeSchemaModel dictionaryModel = RootWithCollection("DictionaryRoot", true);
            var dictionaryRoot = (ObjectTestValue)SemanticTestDataGenerator.Generate(dictionaryModel, new TypeId("DictionaryRoot"), profile, 2).Value!;
            _ = await Assert.That(((DictionaryTestValue)dictionaryRoot.Properties[new PropertyId("Value")]).Entries.Count).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task Small_legal_domains_and_boolean_repetition_are_not_hidden_uniqueness_failures()
    {
        TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.Integer, constraints: new ConstraintSet { Numeric = new NumericConstraints { Minimum = 7, Maximum = 7 } });
        IReadOnlyList<SemanticTestValue> values = model.TestData().WithSeed(9).GenerateMany(new TypeId("Root"), 10);
        _ = await Assert.That(values.Count).IsEqualTo(10);
        _ = await Assert.That(values.Cast<ObjectTestValue>().Select(value => ((ScalarTestValue)value.Properties[new PropertyId("Value")]).Value).Distinct().Count()).IsEqualTo(1);
        TypeSchemaModel boolean = RootWithScalar("Root", "Value", ScalarKind.Boolean);
        _ = await Assert.That(boolean.TestData().WithSeed(9).GenerateMany(new TypeId("Root"), 100).Count).IsEqualTo(100);
        TypeSchemaModel finiteEnum = FiniteEnumModel();
        IReadOnlyList<SemanticTestValue> enumValues = finiteEnum.TestData().WithSeed(9).GenerateMany(new TypeId("EnumRoot"), 100);
        string[] selected = [.. enumValues.Cast<EnumTestValue>().Select(value => Convert.ToString(value.Value, System.Globalization.CultureInfo.InvariantCulture)!)];
        _ = await Assert.That(selected.Distinct(StringComparer.Ordinal).Count()).IsEqualTo(2);
        _ = await Assert.That(selected.Distinct(StringComparer.Ordinal).Count()).IsLessThan(selected.Length);
    }

    [Test]
    public async Task Numeric_exclusive_bounds_and_multiple_of_remain_satisfied()
    {
        TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.Number, constraints: new ConstraintSet { Numeric = new NumericConstraints { Minimum = 0.5m, ExclusiveMinimum = true, Maximum = 2m, ExclusiveMaximum = true, MultipleOf = 0.25m } });
        IReadOnlyList<SemanticTestValue> values = model.TestData().WithSeed(11).GenerateMany(new TypeId("Root"), 20);
        foreach (ObjectTestValue item in values.Cast<ObjectTestValue>())
        {
            decimal value = (decimal)((ScalarTestValue)item.Properties[new PropertyId("Value")]).Value!;
            _ = await Assert.That(value > 0.5m && value < 2m && value % 0.25m == 0m).IsTrue();
        }
    }

    [Test]
    public async Task Explicit_unique_items_and_dictionary_keys_still_fail_on_domain_exhaustion()
    {
        TypeSchemaModel arrayModel = UniqueBooleanArrayModel();
        TestDataGenerationResult arrayResult = SemanticTestDataGenerator.Generate(arrayModel, new TypeId("Root"), TestDataSizeProfile.Simple, 3);
        _ = await Assert.That(arrayResult.Diagnostics.Any(diagnostic => diagnostic.Code == "TESTDATA_UNIQUENESS_EXHAUSTED")).IsTrue();

        TypeSchemaModel dictionaryModel = UniqueBooleanDictionaryModel();
        TestDataGenerationResult dictionaryResult = SemanticTestDataGenerator.Generate(dictionaryModel, new TypeId("Root"), TestDataSizeProfile.Extreme, 3);
        _ = await Assert.That(dictionaryResult.Diagnostics.Any(diagnostic => diagnostic.Code == "TESTDATA_UNIQUENESS_EXHAUSTED")).IsTrue();
    }

    [Test]
    public async Task Equal_rank_terminology_candidates_are_deterministic_for_same_occurrence()
    {
        TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.String);
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        SemanticTerminologyProfile profile = template with { Properties = [template.Properties[0] with { Values = [System.Text.Json.JsonSerializer.SerializeToElement("first"), System.Text.Json.JsonSerializer.SerializeToElement("other")] }] };
        var first = (ObjectTestValue)model.TestData().WithSeed(14).WithTerminology(profile).Generate(new TypeId("Root"));
        var repeat = (ObjectTestValue)model.TestData().WithSeed(14).WithTerminology(profile).Generate(new TypeId("Root"));
        _ = await Assert.That(((ScalarTestValue)first.Properties[new PropertyId("Value")]).Value).IsEqualTo(((ScalarTestValue)repeat.Properties[new PropertyId("Value")]).Value);
    }

    [Test]
    public async Task Programmatic_property_generator_precedes_terminology_candidate()
    {
        TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.String);
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        SemanticTerminologyProfile profile = template with { Properties = [template.Properties[0] with { Values = [System.Text.Json.JsonSerializer.SerializeToElement("terminology")] }] };
        var generated = (ObjectTestValue)model.TestData().WithTerminology(profile).WithPropertyGenerator(new TypeId("Root"), new PropertyId("Value"), _ => "programmatic").WithSeed(2).Generate(new TypeId("Root"));
        _ = await Assert.That(((ScalarTestValue)generated.Properties[new PropertyId("Value")]).Value).IsEqualTo("programmatic");
    }

    [Test]
    public async Task Logical_type_generator_precedes_terminology_candidate()
    {
        TypeSchemaModel model = LogicalModel();
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        SemanticTerminologyProfile profile = template with { LogicalTypes = [template.LogicalTypes[0] with { Values = [System.Text.Json.JsonSerializer.SerializeToElement("terminology")] }] };
        var generated = (ObjectTestValue)model.TestData().WithTerminology(profile).WithLogicalTypeGenerator("ScenarioCode", _ => "logical-generator").WithSeed(2).Generate(new TypeId("Root"));
        _ = await Assert.That(((ScalarTestValue)generated.Properties[new PropertyId("Value")]).Value).IsEqualTo("logical-generator");
    }

    [Test]
    public async Task Supported_scalar_families_vary_across_bulk_coordinates()
    {
        foreach (ScalarKind kind in new[] { ScalarKind.Guid, ScalarKind.Binary, ScalarKind.Date, ScalarKind.Time, ScalarKind.DateTime, ScalarKind.DateTimeOffset, ScalarKind.Duration, ScalarKind.Json, ScalarKind.Integer, ScalarKind.Number, ScalarKind.Decimal })
        {
            TypeSchemaModel model = RootWithScalar("Root", "Value", kind);
            IReadOnlyList<SemanticTestValue> values = model.TestData().WithSeed(19).GenerateMany(new TypeId("Root"), 12);
            string[] fingerprints = [.. values.Cast<ObjectTestValue>().Select(value => Fingerprint((ScalarTestValue)value.Properties[new PropertyId("Value")]))];
            _ = await Assert.That(fingerprints.Distinct(StringComparer.Ordinal).Count()).IsGreaterThan(1);
        }
    }

    [Test]
    public async Task Every_predefined_format_is_deterministic_and_varies()
    {
        string[] formats = ["email", "uri", "uri-reference", "hostname", "ipv4", "ipv6", "date", "time", "date-time", "duration", "uuid"];
        foreach (var format in formats)
        {
            TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.String, format);
            var first = Text(model.TestData().WithSeed(31).Generate(new TypeId("Root")));
            var repeat = Text(model.TestData().WithSeed(31).Generate(new TypeId("Root")));
            var other = Text(model.TestData().WithSeed(32).Generate(new TypeId("Root")));
            _ = await Assert.That(repeat).IsEqualTo(first);
            _ = await Assert.That(other).IsNotEqualTo(first);
        }

        foreach (var format in formats)
        {
            TypeSchemaModel model = RootWithScalar("Root", "Value", ScalarKind.String, format, constraints: new ConstraintSet { String = new StringConstraints { MinLength = 1, MaxLength = 128 } });
            IReadOnlyList<SemanticTestValue> generated = model.TestData().WithSeed(73).GenerateMany(new TypeId("Root"), 8);
            string[] values = [.. generated.Select(Text)];
            foreach (string value in values)
            {
                _ = await Assert.That(value.Length).IsBetween(1, 128);
            }
            _ = await Assert.That(values.Distinct(StringComparer.Ordinal).Count()).IsGreaterThan(1);
            if (format is "email" or "hostname")
            {
                _ = await Assert.That(values.All(value => value.EndsWith("example.test", StringComparison.Ordinal))).IsTrue();
            }
            if (format == "uri")
            {
                _ = await Assert.That(values.All(value => value.StartsWith("https://example.test/", StringComparison.Ordinal))).IsTrue();
            }
            if (format == "ipv4")
            {
                _ = await Assert.That(values.All(value => value.StartsWith("192.0.2.", StringComparison.Ordinal))).IsTrue();
            }
            if (format == "ipv6")
            {
                _ = await Assert.That(values.All(value => value.StartsWith("2001:db8::", StringComparison.Ordinal))).IsTrue();
            }
        }
    }

    private static string Text(SemanticTestValue value)
    {
        return (string)((ScalarTestValue)((ObjectTestValue)value).Properties[new PropertyId("Value")]).Value!;
    }

    private static string Fingerprint(ScalarTestValue value)
    {
        return value.Value switch { byte[] bytes => Convert.ToBase64String(bytes), _ => Convert.ToString(value.Value, System.Globalization.CultureInfo.InvariantCulture)! };
    }

    private static TypeSchemaModel RootWithScalar(string rootId, string name, ScalarKind kind, string? format = null, PropertyDefinition? extra = null, ConstraintSet? constraints = null)
    {
        ScalarTypeDefinition scalar = new() { Id = new("Scalar"), Name = "Scalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = kind, Format = format };
        var properties = new List<PropertyDefinition> { Property(name, scalar.Id, constraints) };
        var types = new List<TypeDefinition> { scalar };
        if (extra is not null)
        {
            ScalarTypeDefinition otherScalar = new() { Id = new("OtherScalar"), Name = "OtherScalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
            properties.Add(extra);
            types.Add(otherScalar);
        }
        ObjectTypeDefinition root = new() { Id = new(rootId), Name = rootId, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = properties, Keys = [] };
        types.Add(root);
        return new TypeSchemaModel { Id = new("test"), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new() };
    }

    private static TypeSchemaModel RootWithCollection(string rootId, bool dictionary)
    {
        ScalarTypeDefinition text = new() { Id = new("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        TypeDefinition container = dictionary
            ? new DictionaryTypeDefinition { Id = new("Container"), Name = "Container", Kind = TypeKind.Dictionary, Nullability = Nullability.NonNullable, Annotations = new(), KeyType = new TypeRef(text.Id), ValueType = new TypeRef(text.Id) }
            : new ArrayTypeDefinition { Id = new("Container"), Name = "Container", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new TypeRef(text.Id) };
        ObjectTypeDefinition root = new() { Id = new(rootId), Name = rootId, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [Property("Value", container.Id)], Keys = [] };
        return new TypeSchemaModel { Id = new("test"), Types = [root, container, text], TypesById = new Dictionary<TypeId, TypeDefinition> { [root.Id] = root, [container.Id] = container, [text.Id] = text }, Annotations = new() };
    }

    private static TypeSchemaModel LogicalModel()
    {
        ScalarTypeDefinition scalar = new() { Id = new("Scalar"), Name = "Scalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        PropertyDefinition property = Property("Value", scalar.Id);
        property = new PropertyDefinition { Id = property.Id, Name = property.Name, Type = property.Type, Cardinality = property.Cardinality, Constraints = property.Constraints, Annotations = new AnnotationBag { Items = [new Annotation { Key = new AnnotationKey("schema.logicalType"), Value = "ScenarioCode", Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }] } };
        ObjectTypeDefinition root = new() { Id = new("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        TypeDefinition[] types = [scalar, root];
        return new TypeSchemaModel { Id = new("test"), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new() };
    }

    private static TypeSchemaModel FiniteEnumModel()
    {
        EnumTypeDefinition enumeration = new() { Id = new("EnumRoot"), Name = "EnumRoot", Kind = TypeKind.Enum, Nullability = Nullability.NonNullable, Annotations = new(), StorageKind = EnumStorageKind.String, Values = [new EnumValueDefinition { Name = "First", Value = "first", Annotations = new() }, new EnumValueDefinition { Name = "Second", Value = "second", Annotations = new() }] };
        TypeDefinition[] types = [enumeration];
        return new TypeSchemaModel { Id = new("test"), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new() };
    }

    private static TypeSchemaModel UniqueBooleanArrayModel()
    {
        ScalarTypeDefinition boolean = new() { Id = new("Boolean"), Name = "Boolean", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.Boolean };
        ArrayTypeDefinition array = new() { Id = new("Items"), Name = "Items", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new TypeRef(boolean.Id), MinItems = 3, MaxItems = 3, UniqueItems = true };
        ObjectTypeDefinition root = new() { Id = new("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [Property("Value", array.Id)], Keys = [] };
        return new TypeSchemaModel { Id = new("test"), Types = [root, array, boolean], TypesById = new Dictionary<TypeId, TypeDefinition> { [root.Id] = root, [array.Id] = array, [boolean.Id] = boolean }, Annotations = new() };
    }

    private static TypeSchemaModel UniqueBooleanDictionaryModel()
    {
        ScalarTypeDefinition boolean = new() { Id = new("Boolean"), Name = "Boolean", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.Boolean };
        DictionaryTypeDefinition dictionary = new() { Id = new("Map"), Name = "Map", Kind = TypeKind.Dictionary, Nullability = Nullability.NonNullable, Annotations = new(), KeyType = new TypeRef(boolean.Id), ValueType = new TypeRef(boolean.Id) };
        PropertyDefinition property = Property("Value", dictionary.Id, new ConstraintSet { Array = new ArrayConstraints { MinItems = 3, MaxItems = 3 } });
        ObjectTypeDefinition root = new() { Id = new("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        return new TypeSchemaModel { Id = new("test"), Types = [root, dictionary, boolean], TypesById = new Dictionary<TypeId, TypeDefinition> { [root.Id] = root, [dictionary.Id] = dictionary, [boolean.Id] = boolean }, Annotations = new() };
    }

    private static PropertyDefinition Property(string name, TypeId type, ConstraintSet? constraints = null)
    {
        return new() { Id = new(name), Name = name, Type = new(type), Cardinality = new Cardinality { IsRequired = true }, Constraints = constraints ?? new(), Annotations = new() };
    }
}
#pragma warning restore CS1591, CA1707, IDE0007, IDE0022
