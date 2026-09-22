#pragma warning disable IDE0011, IDE0058, IDE0072, IDE0305
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Authoring;
using SemanticTypeModel.Core.Inspection;
using SemanticTypeModel.Core.Semantics;
using SemanticTypeModel.TestData;
using SemanticTypeModel.TestData.Inspection;

namespace SemanticTypeModel.Samples.ProgrammaticModelCatalog;

internal static class Program
{
    private static readonly IReadOnlyList<Scenario> Scenarios =
    [
        new("scalar-boolean", "Boolean scalar", () => ScalarScenario(ScalarKind.Boolean, "true")),
        new("scalar-string", "String scalar", () => ScalarScenario(ScalarKind.String, "example-string")),
        new("scalar-integer", "Integer scalar", () => ScalarScenario(ScalarKind.Integer, "42")),
        new("scalar-number", "Number scalar", () => ScalarScenario(ScalarKind.Number, "42.5")),
        new("scalar-decimal", "Decimal scalar", () => ScalarScenario(ScalarKind.Decimal, "42.50")),
        new("scalar-date", "Date scalar", () => ScalarScenario(ScalarKind.Date, "2024-01-02")),
        new("scalar-time", "Time scalar", () => ScalarScenario(ScalarKind.Time, "12:34:56")),
        new("scalar-datetime", "DateTime scalar", () => ScalarScenario(ScalarKind.DateTime, "2024-01-02T12:34:56")),
        new("scalar-datetimeoffset", "DateTimeOffset scalar", () => ScalarScenario(ScalarKind.DateTimeOffset, "2024-01-02T12:34:56Z")),
        new("scalar-duration", "Duration scalar", () => ScalarScenario(ScalarKind.Duration, "01:02:03")),
        new("scalar-guid", "Guid scalar", () => ScalarScenario(ScalarKind.Guid, "00000000-0000-4000-8000-000000000001")),
        new("scalar-binary", "Binary scalar", () => ScalarScenario(ScalarKind.Binary, Convert.ToBase64String([1, 2, 3]))),
        new("scalar-json", "Json scalar", () => ScalarScenario(ScalarKind.Json, "{\"kind\":\"example\"}")),
        new("enum", "Enum value", EnumScenario),
        new("nested-object", "Nested object", NestedObjectScenario),
        new("array", "Array with ordered items", ArrayScenario),
        new("dictionary", "Dictionary with explicit entries", DictionaryScenario),
        new("reference", "Reference type", ReferenceScenario),
        new("any", "Any value", AnyScenario),
        new("unsupported-unknown", "Unknown scalar diagnostic", () => UnsupportedScenario(ScalarKind.Unknown, TypeKind.Scalar)),
        new("unsupported-never", "Never diagnostic", () => UnsupportedScenario(ScalarKind.String, TypeKind.Never)),
        new("unsupported-union", "Union diagnostic", () => UnsupportedScenario(ScalarKind.String, TypeKind.Union)),
        new("unsupported-intersection", "Intersection diagnostic", () => UnsupportedScenario(ScalarKind.String, TypeKind.Intersection)),
        new("constraints-and-cardinality", "Required, optional, nullable, bounds, multipleOf and collection constraints", ConstraintsScenario),
        new("formats-and-pattern", "Predefined formats and pattern terminology fallback", FormatScenario),
        new("unsatisfiable-diagnostic", "Unsatisfiable constraint diagnostic", UnsatisfiableScenario),
        new("semantic-vocabulary", "Roles, keys, identity, access, ownership, envelope, lifecycle and metadata", VocabularyScenario),
        new("entity-roles", "Every projection-neutral entity role", EntityRolesScenario),
        new("terminology-precedence", "Property candidate wins over Logical Type candidate", TerminologyPrecedenceScenario),
        new("terminology-fallback", "Ineligible property candidate falls through to Logical Type", TerminologyFallbackScenario),
        new("terminology-random-fallback", "No eligible terminology candidate falls through to Random", TerminologyRandomFallbackScenario),
        new("random-diversity", "Occurrence-derived diverse Random values and terminology override", RandomDiversityScenario),
        new("sampling-profile", "Named TestData sampling profile with scoped policies and deterministic composition", SamplingProfileScenario),
    ];

    private static int Main(string[] args)
    {
        string command = args.Length == 0 ? "all" : args[0];
        if (command.Equals("list", StringComparison.Ordinal))
        {
            foreach (Scenario scenario in Scenarios)
                Console.WriteLine($"{scenario.Id}: {scenario.Description}");
            return 0;
        }

        IEnumerable<Scenario> selected = command.Equals("all", StringComparison.Ordinal)
            ? Scenarios
            : Scenarios.Where(scenario => scenario.Id.Equals(command, StringComparison.Ordinal));
        if (!selected.Any())
        {
            Console.Error.WriteLine($"Unknown scenario '{command}'. Use 'list'.");
            return 2;
        }

        try
        {
            foreach (Scenario scenario in selected)
            {
                Console.WriteLine($"=== {scenario.Id} — {scenario.Description} ===");
                scenario.Run();
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Catalog assertion failed: {exception.Message}");
            return 1;
        }
    }

    private static void ScalarScenario(ScalarKind kind, string example)
    {
        ScalarTypeDefinition scalar = Scalar("Value", kind);
        ObjectTypeDefinition root = Object("Root", [Property("Value", scalar.Id, logicalType: $"Example.{kind}")]);
        TypeSchemaModel model = Build(root, scalar);
        SemanticTerminologyProfile profile = Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement(ParseExample(kind, example)));
        Emit(model, root.Id, profile);
    }

    private static void EnumScenario()
    {
        EnumTypeDefinition status = new() { Id = new("Status"), Name = "Status", Kind = TypeKind.Enum, Nullability = Nullability.NonNullable, Annotations = new(), StorageKind = EnumStorageKind.String, Values = [new EnumValueDefinition { Name = "Ready", Value = "ready", Annotations = new() }, new EnumValueDefinition { Name = "Done", Value = "done", Annotations = new() }] };
        ObjectTypeDefinition root = Object("Root", [Property("Status", status.Id)]);
        Emit(Build(root, status), root.Id, Profile(Build(root, status), root, root.Properties[0], JsonSerializer.SerializeToElement("ready")));
    }

    private static void NestedObjectScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition address = Object("Address", [Property("City", text.Id)]);
        ObjectTypeDefinition root = Object("Root", [Property("Address", address.Id)]);
        TypeSchemaModel model = Build(root, address, text);
        Emit(model, root.Id, Profile(model, address, address.Properties[0], JsonSerializer.SerializeToElement("Berlin")));
    }

    private static void ArrayScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ArrayTypeDefinition tags = new() { Id = new("Tags"), Name = "Tags", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new TypeRef(text.Id), MinItems = 2, MaxItems = 2 };
        ObjectTypeDefinition root = Object("Root", [Property("Tags", tags.Id)]);
        Emit(Build(root, tags, text), root.Id, Profile(Build(root, tags, text), root, root.Properties[0], JsonSerializer.SerializeToElement("tag")));
    }

    private static void DictionaryScenario()
    {
        ScalarTypeDefinition key = Scalar("Key", ScalarKind.Integer);
        ScalarTypeDefinition value = Scalar("Value", ScalarKind.String);
        DictionaryTypeDefinition map = new() { Id = new("Map"), Name = "Map", Kind = TypeKind.Dictionary, Nullability = Nullability.NonNullable, Annotations = new(), KeyType = new TypeRef(key.Id), ValueType = new TypeRef(value.Id) };
        ObjectTypeDefinition root = Object("Root", [Property("Map", map.Id)]);
        TypeSchemaModel model = Build(root, map, key, value);
        Emit(model, root.Id, Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement("entry")));
    }

    private static void ReferenceScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition target = Object("Target", [Property("Name", text.Id)]);
        ReferenceTypeDefinition reference = new() { Id = new("TargetReference"), Name = "TargetReference", Kind = TypeKind.Reference, Nullability = Nullability.NonNullable, Annotations = new(), Target = new TypeRef(target.Id) };
        ObjectTypeDefinition root = Object("Root", [Property("Target", reference.Id)]);
        TypeSchemaModel model = Build(root, target, reference, text);
        Emit(model, root.Id, Profile(model, target, target.Properties[0], JsonSerializer.SerializeToElement("reference")));
    }

    private static void AnyScenario()
    {
        TypeDefinition any = new SyntheticType("Any", TypeKind.Any);
        ObjectTypeDefinition root = Object("Root", [Property("Payload", any.Id)]);
        TypeSchemaModel model = Build(root, any);
        Emit(model, root.Id, Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement("ignored-for-any")));
    }

    private static void UnsupportedScenario(ScalarKind scalarKind, TypeKind kind)
    {
        TypeDefinition leaf = kind == TypeKind.Scalar ? Scalar("Unsupported", scalarKind) : new SyntheticType("Unsupported", kind);
        ObjectTypeDefinition root = Object("Root", [Property("Value", leaf.Id)]);
        TypeSchemaModel model = Build(root, leaf);
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, root.Id, TestDataSizeProfile.Simple, 81);
        Require(result.HasErrors, "Unsupported scenario unexpectedly generated a value.");
        Console.WriteLine("Model");
        Console.WriteLine(model.ToSemanticText());
        Console.WriteLine("Random");
        Console.WriteLine(string.Join("\n", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        Console.WriteLine("Example-guided");
        Console.WriteLine("Unavailable: unsupported semantic is intentionally diagnostic.");
    }

    private static void ConstraintsScenario()
    {
        ScalarTypeDefinition number = Scalar("Number", ScalarKind.Number);
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ArrayTypeDefinition values = new() { Id = new("Values"), Name = "Values", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new TypeRef(number.Id), MinItems = 1, MaxItems = 2, UniqueItems = true };
        ObjectTypeDefinition root = Object("Root", [
            Property("Required", number.Id, constraints: new ConstraintSet { Numeric = new NumericConstraints { Minimum = 2, Maximum = 10, MultipleOf = 2 } }),
            Property("Optional", text.Id, required: false),
            Property("Nullable", text.Id, nullable: true),
            Property("Values", values.Id),
        ]);
        TypeSchemaModel model = Build(root, values, number, text);
        Emit(model, root.Id, Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement(4)));
    }

    private static void FormatScenario()
    {
        ScalarTypeDefinition email = Scalar("Email", ScalarKind.String, format: "email");
        ScalarTypeDefinition code = Scalar("Code", ScalarKind.String);
        ObjectTypeDefinition root = Object("Root", [Property("Email", email.Id), Property("Code", code.Id, constraints: new ConstraintSet { String = new StringConstraints { Pattern = "^[A-Z]{3}-[0-9]{3}$" } })]);
        TypeSchemaModel model = Build(root, email, code);
        TestDataGenerationResult random = SemanticTestDataGenerator.Generate(model, root.Id, TestDataSizeProfile.Simple, 81);
        Require(random.Diagnostics.Any(d => d.Code == "TESTDATA_PATTERN_UNSUPPORTED"), "Pattern Random generation should fail closed.");
        SemanticTerminologyProfile profile = Profile(model, root, root.Properties[1], JsonSerializer.SerializeToElement("ABC-123"));
        TestDataGenerationResult examples = SemanticTestDataGenerator.Generate(model, root.Id, TestDataSizeProfile.Simple, 81, profile);
        Require(examples.Succeeded, "Pattern terminology candidate should make example-guided generation succeed.");
        Console.WriteLine("Model");
        Console.WriteLine(model.ToSemanticText());
        Console.WriteLine("Random");
        Console.WriteLine(string.Join("\n", random.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        Console.WriteLine("Example-guided");
        Console.WriteLine(examples.Value!.ToSemanticText(model));
    }

    private static void UnsatisfiableScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition root = Object("Root", [Property("Value", text.Id, constraints: new ConstraintSet { String = new StringConstraints { MinLength = 100 } })]);
        TypeSchemaModel model = Build(root, text);
        IReadOnlyList<SchemaDiagnostic> diagnostics = [];
        try
        {
            _ = model.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 10 }).WithSeed(81).Generate(root.Id);
        }
        catch (TestDataGenerationException exception)
        {
            diagnostics = exception.Diagnostics;
        }
        Require(diagnostics.Any(d => d.Code == "TESTDATA_SIZE_BUDGET_EXHAUSTED"), "An unsatisfiable generation budget must remain diagnostic.");
        Console.WriteLine("Model");
        Console.WriteLine(model.ToSemanticText());
        Console.WriteLine("Random");
        Console.WriteLine(string.Join("\n", diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        Console.WriteLine("Example-guided");
        Console.WriteLine("Unavailable: the canonical numeric interval is empty.");
    }

    private static void VocabularyScenario()
    {
        ScalarTypeDefinition id = Scalar("Id", ScalarKind.String);
        ArrayTypeDefinition ownedItems = new() { Id = new("OwnedItems"), Name = "OwnedItems", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new TypeRef(id.Id), MinItems = 1, MaxItems = 1 };
        ObjectTypeDefinition root = Object("OrderEnvelope", [
            Property("Id", id.Id, logicalType: "Order.Id", annotations: [Annotation(CoreSemanticAnnotationKeys.DisplayIdentity, "0"), Annotation(CoreSemanticAnnotationKeys.AccessPathPrefix + "ById", "0"), Annotation(CoreSemanticAnnotationKeys.Version, "true")]),
            Property("Payload", id.Id, annotations: [Annotation(CoreSemanticAnnotationKeys.EnvelopePayload, "true"), Annotation(CoreSemanticAnnotationKeys.OwnedObject, "true")]),
            Property("Metadata", id.Id, annotations: [Annotation(CoreSemanticAnnotationKeys.EnvelopeMetadata, "true"), Annotation(CoreSemanticAnnotationKeys.LifecycleState, "true"), Annotation(CoreSemanticAnnotationKeys.Revision, "true"), Annotation(CoreSemanticAnnotationKeys.CurrentVersion, "true"), Annotation(CoreSemanticAnnotationKeys.ValidFrom, "true"), Annotation(CoreSemanticAnnotationKeys.ValidTo, "true"), Annotation(CoreSemanticAnnotationKeys.ExtensionData, "true"), Annotation(CoreSemanticAnnotationKeys.RequiredWhen, "Id == 'ORDER-001'")]),
            Property("OwnedItems", ownedItems.Id, annotations: [Annotation(CoreSemanticAnnotationKeys.OwnedCollection, "true")]),
        ], role: EntityRole.Event, annotations: [Annotation(CoreSemanticAnnotationKeys.Envelope, "true"), Annotation(CoreSemanticAnnotationKeys.Versioned, "true"), Annotation(CoreSemanticAnnotationKeys.TemporalValidity, "true")]);
        root = root with { DisplayName = "Order envelope", UserDescription = "A transport wrapper", TechnicalDescription = "M0081 vocabulary fixture", Semantics = new EntitySemantics { Role = EntityRole.Event, IsAggregateRoot = true }, Mutability = SemanticMutability.Immutable, Keys = [new KeyDefinition { Name = "Composite", Kind = KeyKind.Primary, Properties = [new PropertyRef(new PropertyId("Id")), new PropertyRef(new PropertyId("Metadata"))], Annotations = new() }, new KeyDefinition { Name = "External", Kind = KeyKind.External, Properties = [new PropertyRef(new PropertyId("Id"))], Annotations = new() }], ComputedMembers = [new ComputedMemberDefinition { Name = "ComputedLabel", ResultType = new TypeRef(id.Id), Expression = new ExpressionDefinition { Language = "stm", Body = "Id" }, Annotations = new() }] };
        TypeSchemaModel model = Build(root, ownedItems, id);
        Emit(model, root.Id, Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement("ORDER-001")));
    }

    private static void EntityRolesScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        EntityRole[] roles = [EntityRole.Entity, EntityRole.ValueObject, EntityRole.Dimension, EntityRole.Fact, EntityRole.Lookup, EntityRole.Event, EntityRole.Configuration, EntityRole.Form];
        Require(roles.Length == 8, "The role catalog is incomplete.");
        ObjectTypeDefinition root = Object("RoleExample", [Property("Name", text.Id)], role: EntityRole.Entity);
        TypeSchemaModel model = Build(root, text);
        Console.WriteLine("Covered roles: " + string.Join(", ", roles));
        Emit(model, root.Id, Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement("role-example")));
    }

    private static void TerminologyPrecedenceScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition root = Object("Root", [Property("Code", text.Id, logicalType: "OrderCode")]);
        TypeSchemaModel model = Build(root, text);
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        SemanticTerminologyProfile profile = template with
        {
            Properties = [template.Properties[0] with { Values = [JsonSerializer.SerializeToElement("property-code")] }],
            LogicalTypes = [template.LogicalTypes[0] with { Values = [JsonSerializer.SerializeToElement("logical-code")] }],
        };
        Emit(model, root.Id, profile);
        Require(Inspected(model, root.Id, profile).Contains("property-code", StringComparison.Ordinal), "Property terminology must win.");
    }

    private static void TerminologyFallbackScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition root = Object("Root", [Property("Code", text.Id, logicalType: "OrderCode")]);
        TypeSchemaModel model = Build(root, text);
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        SemanticTerminologyProfile profile = template with
        {
            Properties = [template.Properties[0] with { Values = [JsonSerializer.SerializeToElement(new string('x', 100))] }],
            LogicalTypes = [template.LogicalTypes[0] with { Values = [JsonSerializer.SerializeToElement("logical-code")] }],
        };
        TestDataGenerationResult random = SemanticTestDataGenerator.Generate(model, root.Id, TestDataSizeProfile.Simple, 81);
        SemanticTestValue example = model.TestData().WithBudgets(new TestDataBudgets { MaxStringLength = 20 }).WithTerminology(profile).WithSeed(81).Generate(root.Id);
        Console.WriteLine("Model");
        Console.WriteLine(model.ToSemanticText());
        Console.WriteLine("Random");
        Console.WriteLine(random.Value!.ToSemanticText(model));
        Console.WriteLine("Example-guided");
        Console.WriteLine(example.ToSemanticText(model));
        Require(example.ToSemanticText(model).Contains("logical-code", StringComparison.Ordinal), "An ineligible property candidate must fall through to Logical Type.");
    }

    private static void TerminologyRandomFallbackScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition root = Object("Root", [Property("Code", text.Id, logicalType: "OrderCode")]);
        TypeSchemaModel model = Build(root, text);
        SemanticTerminologyProfile profile = SemanticTerminologyProfileJson.Create(model);
        TestDataGenerationResult random = SemanticTestDataGenerator.Generate(model, root.Id, TestDataSizeProfile.Simple, 81);
        TestDataGenerationResult example = SemanticTestDataGenerator.Generate(model, root.Id, TestDataSizeProfile.Simple, 81, profile);
        Require(random.Succeeded && example.Succeeded, "Empty terminology must fall through to Random generation.");
        Require(random.Value!.ToSemanticText(model) == example.Value!.ToSemanticText(model), "No eligible terminology candidate should not alter Random output.");
        Console.WriteLine("Model");
        Console.WriteLine(model.ToSemanticText());
        Console.WriteLine("Random");
        Console.WriteLine(random.Value!.ToSemanticText(model));
        Console.WriteLine("Example-guided");
        Console.WriteLine(example.Value!.ToSemanticText(model));
    }

    private static void RandomDiversityScenario()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ScalarTypeDefinition guid = Scalar("Guid", ScalarKind.Guid);
        ScalarTypeDefinition date = Scalar("Date", ScalarKind.Date);
        ScalarTypeDefinition number = Scalar("Number", ScalarKind.Number);
        ScalarTypeDefinition binary = Scalar("Binary", ScalarKind.Binary);
        ObjectTypeDefinition root = Object("RandomDiversityRoot", [
            Property("First", text.Id),
            Property("Second", text.Id),
            Property("Third", text.Id),
            Property("Id", guid.Id),
            Property("When", date.Id),
            Property("Amount", number.Id),
            Property("Payload", binary.Id),
        ]);
        TypeSchemaModel model = Build(root, text, guid, date, number, binary);
        SemanticTerminologyProfile profile = Profile(model, root, root.Properties[0], JsonSerializer.SerializeToElement("terminology-wins"));
        IReadOnlyList<SemanticTestValue> firstRun = model.TestData().WithSeed(2026).WithTerminology(profile).GenerateMany(root.Id, 3);
        IReadOnlyList<SemanticTestValue> repeatRun = model.TestData().WithSeed(2026).WithTerminology(profile).GenerateMany(root.Id, 3);
        string firstText = string.Join("\n", firstRun.Select(value => value.ToSemanticText(model)));
        string repeatText = string.Join("\n", repeatRun.Select(value => value.ToSemanticText(model)));
        Require(firstText == repeatText, "The fixed-seed Random diversity scenario must be repeatable.");
        Require(firstText.Contains("terminology-wins", StringComparison.Ordinal), "Terminology must override Random for the supplied property.");
        string[] randomStrings = firstRun.SelectMany(value => new[] { ScalarText(value, "Second"), ScalarText(value, "Third") }).ToArray();
        Require(randomStrings.Distinct(StringComparer.Ordinal).Count() == randomStrings.Length, "Ordinary Random strings must differ across properties and bulk roots.");
        string[] identifiers = firstRun.Select(value => ScalarText(value, "Id")).ToArray();
        Require(identifiers.Distinct(StringComparer.Ordinal).Count() == identifiers.Length, "Guid values must differ across bulk roots.");
        string[] dates = firstRun.Select(value => ScalarText(value, "When")).ToArray();
        Require(dates.Distinct(StringComparer.Ordinal).Count() > 1, "Another scalar family must vary across bulk roots.");
        Console.WriteLine("Fixed seed: 2026");
        Console.WriteLine(firstText);
    }

    private static void SamplingProfileScenario()
    {
        ScalarTypeDefinition code = Scalar("Code", ScalarKind.String);
        ScalarTypeDefinition amount = Scalar("Amount", ScalarKind.Integer);
        ArrayTypeDefinition tags = new() { Id = new("Tags"), Name = "Tags", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, Annotations = new(), ItemType = new TypeRef(code.Id), MinItems = 1, MaxItems = 3 };
        ObjectTypeDefinition root = Object("SamplingRoot", [
            Property("Code", code.Id, logicalType: "Scenario.Code"),
            Property("Optional", code.Id, required: false, nullable: true),
            Property("Amount", amount.Id, constraints: new ConstraintSet { Numeric = new NumericConstraints { Minimum = 10, Maximum = 20 } }),
            Property("Tags", tags.Id),
        ]);
        TypeSchemaModel model = Build(root, code, amount, tags);
        PropertyDefinition codeProperty = root.Properties.Single(property => property.Id == new PropertyId("Code"));
        PropertyDefinition optionalProperty = root.Properties.Single(property => property.Id == new PropertyId("Optional"));
        PropertyDefinition amountProperty = root.Properties.Single(property => property.Id == new PropertyId("Amount"));
        PropertyDefinition tagsProperty = root.Properties.Single(property => property.Id == new PropertyId("Tags"));

        TestDataProfileBuilder builder = TestDataProfile.Create(model, "TypicalCustomer");
        builder.Defaults(defaults => defaults.OptionalPresence(0.5).NullProbability(0.25).ValueStrategy(TestDataValueStrategy.Random));
        builder.For(root).Property(codeProperty).Weighted(("Active", 7), ("Pending", 3)).Done()
            .Property(optionalProperty).Presence(1).NullProbability(1).Done()
            .Property(amountProperty).ValueStrategy(TestDataValueStrategy.BoundaryMixed).Done()
            .Property(tagsProperty).FixedCount(2).Done().Done();
        builder.ForLogicalType("Scenario.Code").ValueStrategy(TestDataValueStrategy.Random).Done();
        TestDataProfile typical = builder.Build();

        TestDataProfile overlay = TestDataProfile.Create(model, "BoundaryOverlay")
            .Defaults(defaults => defaults.ValueStrategy(TestDataValueStrategy.BoundaryMixed))
            .Build();
        TestDataProfile combined = TestDataProfile.Compose("TypicalBoundary", typical, overlay);
        SemanticTerminologyProfile terminology = Profile(model, root, codeProperty, JsonSerializer.SerializeToElement("TerminologyFallback"));
        ObjectTestValue value = (ObjectTestValue)model.TestData().WithProfile(combined).WithTerminology(terminology).WithSeed(42).Generate(root.Id);
        string inspected = value.ToSemanticText(model);
        Require(((ScalarTestValue)value.Properties[codeProperty.Id]).Value is "Active" or "Pending", "Profile weighted values must override terminology candidates.");
        Require(value.Properties[optionalProperty.Id] is NullTestValue, "The exact nullable policy must produce NullTestValue.");
        Require(((ArrayTestValue)value.Properties[tagsProperty.Id]).Items.Count == 2, "The exact collection-size policy must be honored.");
        decimal boundary = (decimal)((ScalarTestValue)value.Properties[amountProperty.Id]).Value!;
        Require(boundary is >= 10 and <= 20, "BoundaryMixed must remain within canonical numeric bounds.");
        Require(inspected.Contains("SamplingRoot", StringComparison.Ordinal), "Semantic-value inspection must be deterministic and visible.");

        TestDataProfile omitted = TestDataProfile.Create(model, "Minimal")
            .For(root).Property(optionalProperty).Presence(0).Done().Done().Build();
        ObjectTestValue omittedValue = (ObjectTestValue)model.TestData().WithProfile(omitted).WithSeed(42).Generate(root.Id);
        Require(!omittedValue.Properties.ContainsKey(optionalProperty.Id), "Presence zero must omit an optional property.");
        Console.WriteLine("Profile: " + combined.Name + "; seed: 42");
        Console.WriteLine(inspected);
    }

    private static string ScalarText(SemanticTestValue value, string property)
    {
        ObjectTestValue root = (ObjectTestValue)value;
        ScalarTestValue scalar = (ScalarTestValue)root.Properties[new PropertyId(property)];
        return Convert.ToString(scalar.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static void Emit(TypeSchemaModel model, TypeId root, SemanticTerminologyProfile profile)
    {
        TestDataGenerationResult random = SemanticTestDataGenerator.Generate(model, root, TestDataSizeProfile.Simple, 81);
        TestDataGenerationResult examples = SemanticTestDataGenerator.Generate(model, root, TestDataSizeProfile.Simple, 81, profile);
        Require(random.Succeeded, "Random semantic generation failed: " + string.Join("; ", random.Diagnostics.Select(d => d.Code + " " + d.Message)));
        Require(examples.Succeeded, "Example-guided semantic generation failed: " + string.Join("; ", examples.Diagnostics.Select(d => d.Code + " " + d.Message)));
        Console.WriteLine("Model");
        Console.WriteLine(model.ToSemanticText());
        Console.WriteLine("Random");
        Console.WriteLine(random.Value!.ToSemanticText(model));
        Console.WriteLine("Example-guided");
        Console.WriteLine(examples.Value!.ToSemanticText(model));
    }

    private static string Inspected(TypeSchemaModel model, TypeId root, SemanticTerminologyProfile profile)
    {
        TestDataGenerationResult result = SemanticTestDataGenerator.Generate(model, root, TestDataSizeProfile.Simple, 81, profile);
        Require(result.Succeeded, "Profile-guided generation failed.");
        return result.Value!.ToSemanticText(model);
    }

    private static SemanticTerminologyProfile Profile(TypeSchemaModel model, ObjectTypeDefinition owner, PropertyDefinition property, JsonElement candidate)
    {
        SemanticTerminologyProfile template = SemanticTerminologyProfileJson.Create(model);
        return template with { Properties = template.Properties.Select(entry => entry.OwnerTypeId == owner.Id.Value && entry.PropertyId == property.Id.Value ? entry with { Values = [candidate] } : entry).ToArray() };
    }

    private static object ParseExample(ScalarKind kind, string value) => kind switch
    {
        ScalarKind.Integer => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.Number or ScalarKind.Decimal => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.Boolean => bool.Parse(value),
        ScalarKind.Json => JsonDocument.Parse(value).RootElement.Clone(),
        ScalarKind.Date => DateOnly.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.Time => TimeOnly.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.DateTime => DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.DateTimeOffset => DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.Duration => TimeSpan.Parse(value, System.Globalization.CultureInfo.InvariantCulture),
        ScalarKind.Guid => Guid.Parse(value),
        ScalarKind.Binary => value,
        _ => value,
    };

    private static TypeSchemaModel Build(params TypeDefinition[] declarations)
    {
        var builder = new TypeSchemaModelAuthoringBuilder(new SchemaModelId("m0081-catalog"));
        foreach (TypeDefinition declaration in declarations)
            builder.AddType(declaration);
        TypeSchemaModelAuthoringResult result = builder.Build();
        Require(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.Model!;
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind, string? format = null) => new() { Id = new(id), Name = id, Kind = TypeKind.Scalar, ScalarKind = kind, Format = format, Nullability = Nullability.NonNullable, Annotations = new() };
    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties, EntityRole role = EntityRole.Unspecified, IReadOnlyList<Annotation>? annotations = null) => new() { Id = new(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag { Items = annotations ?? [] }, Keys = [], Properties = properties, Semantics = new EntitySemantics { Role = role } };
    private static PropertyDefinition Property(string id, TypeId type, bool required = true, bool nullable = false, string? logicalType = null, ConstraintSet? constraints = null, IReadOnlyList<Annotation>? annotations = null)
    {
        var items = new List<Annotation>();
        if (logicalType is not null)
            items.Add(Annotation(CoreSemanticAnnotationKeys.LogicalType, logicalType));
        items.AddRange(annotations ?? []);
        return new PropertyDefinition { Id = new(id), Name = id, Type = new TypeRef(type), Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable }, Constraints = constraints ?? new(), Annotations = new AnnotationBag { Items = items } };
    }
    private static Annotation Annotation(string key, object value) => new() { Key = new AnnotationKey(key), Value = value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared };
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed record Scenario(string Id, string Description, Action Run);
    private sealed record SyntheticType : TypeDefinition
    {
        [SetsRequiredMembers]
        public SyntheticType(string id, TypeKind kind) { Id = new TypeId(id); Name = id; Kind = kind; Nullability = Nullability.NonNullable; Annotations = new(); }
    }
}
#pragma warning restore IDE0011, IDE0058, IDE0072, IDE0305
