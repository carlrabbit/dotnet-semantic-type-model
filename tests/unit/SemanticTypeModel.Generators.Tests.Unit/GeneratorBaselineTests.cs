using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.Generators;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.Generators.Tests.Unit;

[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class GeneratorBaselineTests
{
    [Test]
    public async Task Fixture_1_simple_annotated_object_should_preserve_requiredness_nullability_and_key_metadata()
    {
        const string source = """
            using System;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            [SemanticName("Customer")]
            [SemanticDescription("A customer account.")]
            public sealed class Customer
            {
                [SemanticKey]
                public Guid Id { get; init; }

                public required string Name { get; init; }

                public string? Nickname { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");

        PropertyShape name = customer.Properties.Single(static property => property.Name == "Name");
        PropertyShape nickname = customer.Properties.Single(static property => property.Name == "Nickname");
        PropertyShape id = customer.Properties.Single(static property => property.Name == "Id");

        _ = await Assert.That(name.IsRequired).IsTrue();
        _ = await Assert.That(name.IsNullable).IsFalse();
        _ = await Assert.That(nickname.IsRequired).IsFalse();
        _ = await Assert.That(nickname.IsNullable).IsTrue();
        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "schema.key" && annotation.Value == "true")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.title" && annotation.Value == "Customer")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.description" && annotation.Value == "A customer account.")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_2_scalars_should_map_to_expected_scalar_shapes_and_annotations()
    {
        const string source = """
            using System;
            using System.Text.Json;
            using System.Text.Json.Nodes;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class ScalarBag
            {
                public bool Flag { get; init; }
                public string Text { get; init; } = string.Empty;
                public int Count { get; init; }
                public double Ratio { get; init; }
                public decimal Amount { get; init; }
                public DateOnly Date { get; init; }
                public TimeOnly Time { get; init; }
                public DateTime Timestamp { get; init; }
                public DateTimeOffset OffsetTimestamp { get; init; }
                public TimeSpan Duration { get; init; }
                public Guid Identifier { get; init; }
                public byte[] Payload { get; init; } = [];
                public JsonDocument Document { get; init; } = JsonDocument.Parse("{}");
                public JsonElement Element { get; init; }
                public JsonNode? Node { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape root = (ObjectShape)model.GetShape("global::ScalarBag");
        Dictionary<string, PropertyShape> properties = root.Properties.ToDictionary(static property => property.Name, static property => property, StringComparer.Ordinal);

        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Flag"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.Boolean);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Text"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.String);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Count"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.Integer);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Ratio"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.Number);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Amount"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "dotnet.scalarKind" && annotation.Value == "Decimal")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Date"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "date")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Time"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "time")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Timestamp"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "date-time")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Duration"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "duration")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Identifier"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "uuid")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_3_collections_and_dictionaries_should_map_and_diagnose_unsupported_keys()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class CollectionBag
            {
                public string[] Names { get; init; } = [];
                public List<int> Ints { get; init; } = [];
                public IReadOnlyList<Guid> Ids { get; init; } = [];
                public HashSet<double> Ratios { get; init; } = [];
                public Dictionary<string, int> Lookup { get; init; } = [];
                public Dictionary<DateTime, int> Unsupported { get; init; } = [];
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape root = (ObjectShape)model.GetShape("global::CollectionBag");
        Dictionary<string, PropertyShape> properties = root.Properties.ToDictionary(static property => property.Name, static property => property, StringComparer.Ordinal);

        _ = await Assert.That(model.GetShape(properties["Names"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Ints"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Ids"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Ratios"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Lookup"].Type!.Identifier!)).IsTypeOf<DictionaryShape>();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5007")).IsTrue();
    }

    [Test]
    public async Task Fixture_4_enum_should_include_values_and_numeric_metadata()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            public enum OrderStatus
            {
                New = 1,
                Packed = 2,
                Shipped = 5,
            }

            [SemanticType]
            public sealed class Order
            {
                public OrderStatus Status { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        EnumShape orderStatus = (EnumShape)model.GetShape("global::OrderStatus");

        _ = await Assert.That(orderStatus.Values.SequenceEqual(["New", "Packed", "Shipped"])).IsTrue();
        _ = await Assert.That(orderStatus.Annotations.Any(static annotation => annotation.Key == "dotnet.enumNumericValues" && annotation.Value == "[1,2,5]")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_5_nested_object_graph_should_include_reachable_types()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                public Address Address { get; init; } = new();
            }

            public sealed class Address
            {
                public string Street { get; init; } = string.Empty;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        _ = await Assert.That(model.TryGetShape("global::Address")).IsNotNull();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_6_generics_should_generate_distinct_closed_identities_and_diagnose_open_generics()
    {
        const string source = """
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
            public sealed class Customer;
            public sealed class Order;

            [SemanticType]
            public sealed class Root
            {
                public PagedResult<Customer> Customers { get; init; } = new([], 0);
                public PagedResult<Order> Orders { get; init; } = new([], 0);
            }

            [SemanticType]
            public sealed class OpenGeneric<T>
            {
                public T Value { get; init; } = default!;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);

        _ = await Assert.That(model.TryGetShape("global::PagedResult<global::Customer>")).IsNotNull();
        _ = await Assert.That(model.TryGetShape("global::PagedResult<global::Order>")).IsNotNull();
        _ = await Assert.That(model.TryGetShape("global::PagedResult<global::Customer>")).IsNotEqualTo(model.TryGetShape("global::PagedResult<global::Order>"));
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5004")).IsTrue();
    }

    [Test]
    public async Task Fixture_7_inheritance_and_interface_metadata_should_be_preserved_as_annotations()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            public interface IMarker { }
            public class BaseEntity { public int Id { get; init; } }

            [SemanticType]
            public sealed class Customer : BaseEntity, IMarker
            {
                public string Name { get; init; } = string.Empty;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");

        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "dotnet.baseType" && annotation.Value == "global::BaseEntity")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "dotnet.interfaces")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_8_generated_model_should_export_to_json_schema()
    {
        const string source = """
            using System;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                [SemanticKey]
                public Guid Id { get; init; }

                public required string Name { get; init; }

                [SemanticIgnore]
                public string InternalCode { get; init; } = string.Empty;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        JsonSchemaExportResult export = JsonSchemaExporter.Export(model);
        string json = export.Document.RootElement.GetRawText();

        _ = await Assert.That(json.Contains("\"properties\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"Name\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"required\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"Id\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("InternalCode", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    private static (TypeSchemaModel Model, IReadOnlyList<Diagnostic> Diagnostics) GenerateModel(string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        IIncrementalGenerator generator = new SemanticTypeModelSourceGenerator();
        CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> _);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        IReadOnlyList<Diagnostic> diagnostics = runResult.Results.SelectMany(static result => result.Diagnostics).ToArray();

        using var stream = new MemoryStream();
        EmitResult emitResult = outputCompilation.Emit(stream);
        if (!emitResult.Success)
        {
            string messages = string.Join(Environment.NewLine, emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException($"Compilation failed:{Environment.NewLine}{messages}");
        }

        stream.Position = 0;
        System.Reflection.Assembly generatedAssembly = System.Reflection.Assembly.Load(stream.ToArray());
        Type? providerType = generatedAssembly.GetType("SemanticTypeModel.Generated.AppSemanticTypeModel", throwOnError: false, ignoreCase: false);
        if (providerType is null)
        {
            throw new InvalidOperationException("Generated provider type was not found.");
        }

        MethodInfo? createMethod = providerType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        if (createMethod is null)
        {
            throw new InvalidOperationException("Generated provider Create method was not found.");
        }

        var model = (TypeSchemaModel?)createMethod.Invoke(null, null);
        if (model is null)
        {
            throw new InvalidOperationException("Generated provider returned null.");
        }

        return (model, diagnostics);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        MetadataReference[] references = GetMetadataReferences();

        return CSharpCompilation.Create(
            assemblyName: $"SemanticTypeModel.GeneratorTest_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        AddReference(references, typeof(object).Assembly);
        AddReference(references, typeof(Enumerable).Assembly);
        AddReference(references, typeof(SemanticTypeAttribute).Assembly);
        AddReference(references, typeof(SemanticTypeModelSourceGenerator).Assembly);
        AddReference(references, typeof(TypeSchemaModel).Assembly);
        AddReference(references, typeof(JsonSchemaExporter).Assembly);
        AddReference(references, typeof(System.Text.Json.JsonDocument).Assembly);

        return [.. references.Values];
    }

    private static void AddReference(Dictionary<string, PortableExecutableReference> references, Assembly assembly)
    {
        if (string.IsNullOrWhiteSpace(assembly.Location))
        {
            return;
        }

        references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
    }
}
