using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Generators;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.Samples.CodeFirstAuthoring;

internal static class Program
{
    private static readonly JsonSerializerOptions WriteIndentedOptions = new() { WriteIndented = true };

    private static void Main()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType(SemanticTypeRole.Entity)]
            [SemanticDisplayName("Customer account")]
            [SemanticAnnotation("efCore.tableName", "customer_accounts")]
            public sealed class Customer
            {
                [SemanticKey(IsGenerated = true)]
                [SemanticFormat(SemanticScalarFormat.Uuid)]
                public required string Id { get; init; }

                [SemanticDisplayName("Email address")]
                [SemanticFormat(SemanticScalarFormat.Email)]
                [SemanticStringConstraints(MinLength = 5, MaxLength = 200)]
                [SemanticAnnotation("ui.placeholder", "name@example.com")]
                public required string Email { get; init; }

                public string? Nickname { get; init; }

                public CustomerStatus Status { get; init; }

                public Address BillingAddress { get; init; } = new();

                [SemanticRelationship(nameof(CustomerOrder), ForeignKey = nameof(CustomerOrder.CustomerId), Cardinality = RelationshipCardinality.OneToMany)]
                public System.Collections.Generic.List<CustomerOrder> Orders { get; init; } = [];
            }

            [SemanticRole(SemanticTypeRole.ValueObject)]
            public sealed class Address
            {
                [SemanticStringConstraints(MinLength = 2, MaxLength = 100)]
                public string Street { get; init; } = string.Empty;

                [SemanticStringConstraints(MinLength = 2, MaxLength = 80)]
                public string City { get; init; } = string.Empty;
            }

            [SemanticRole(SemanticTypeRole.Entity)]
            public sealed class CustomerOrder
            {
                [SemanticKey]
                [SemanticFormat(SemanticScalarFormat.Uuid)]
                public required string Id { get; init; }

                [SemanticFormat(SemanticScalarFormat.Uuid)]
                public required string CustomerId { get; init; }

                [SemanticStringConstraints(MinLength = 1, MaxLength = 64)]
                public required string OrderNumber { get; init; }
            }

            public enum CustomerStatus
            {
                [SemanticEnumValue(DisplayName = "Active customer", Description = "Customer can place new orders.")]
                Active = 1,

                [SemanticEnumValue(DisplayName = "Suspended customer", Description = "Customer is blocked from placing new orders.")]
                Suspended = 2,
            }
            """;

        TypeSchemaModel model = GenerateModel(source);
        var adapted = LegacyTypeSchemaModelAdapter.Adapt(model);
        var hardened = adapted.Model
            ?? throw new InvalidOperationException("The generated semantic model could not be adapted to the hardened model.");

        JsonSchemaExportResult schema = JsonSchemaExporter.Export(model);
        JsonSchemaExportResult jsonEditorSchema = JsonSchemaExporter.Export(
            model,
            new JsonSchemaExportOptions
            {
                UiExport = new JsonSchemaUiExportOptions
                {
                    UiMode = JsonSchemaUiMode.JsonEditorCompatible,
                    IncludeGenericUiAnnotations = true,
                    IncludeJsonEditorCompatibilityAnnotations = true,
                },
            });

        var outputDirectory = Path.Combine("artifacts", "samples", "code-first-authoring");
        _ = Directory.CreateDirectory(outputDirectory);

        WriteJson(Path.Combine(outputDirectory, "customer.schema.json"), schema.Document.RootElement);
        WriteJson(Path.Combine(outputDirectory, "customer.ui-schema.json"), jsonEditorSchema.Document.RootElement);

        var modelBuilder = new ModelBuilder(new ConventionSet());
        EfCoreModelBuilderProjectionResult efCoreResult = modelBuilder.ApplySemanticTypeModel(
            hardened,
            options =>
            {
                options.DefaultSchema = "sample";
                options.ProjectUnannotatedObjectsAsEntities = true;
            });

        Console.WriteLine($"root: {model.RootIdentifier}");
        Console.WriteLine($"named shapes: {model.Shapes.Count}");
        Console.WriteLine($"adapter diagnostics: {adapted.Diagnostics.Count}");
        Console.WriteLine($"efcore diagnostics: {efCoreResult.Diagnostics.Count}");
        Console.WriteLine($"artifacts: {outputDirectory}");
    }

    private static TypeSchemaModel GenerateModel(string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        IIncrementalGenerator generator = new SemanticTypeModelSourceGenerator();
        CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

        using var stream = new MemoryStream();
        EmitResult emitResult = outputCompilation.Emit(stream);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString())));
        }

        stream.Position = 0;
        Assembly assembly = Assembly.Load(stream.ToArray());
        Type providerType = assembly.GetType("SemanticTypeModel.Generated.AppSemanticTypeModel", throwOnError: true)!;
        MethodInfo create = providerType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Generated Create method was not found.");

        var model = (TypeSchemaModel?)create.Invoke(null, null)
            ?? throw new InvalidOperationException("Generated provider returned null.");
        return model;
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        AddReference(references, typeof(object).Assembly);
        AddReference(references, typeof(Enumerable).Assembly);
        AddReference(references, typeof(SemanticTypeModel.DotNet.SemanticTypeAttribute).Assembly);
        AddReference(references, typeof(SemanticTypeModelSourceGenerator).Assembly);
        AddReference(references, typeof(TypeSchemaModel).Assembly);
        AddReference(references, typeof(JsonSchemaExporter).Assembly);

        return CSharpCompilation.Create(
            assemblyName: $"SemanticTypeModel.Sample.Generated_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: [.. references.Values],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static void AddReference(Dictionary<string, PortableExecutableReference> references, Assembly assembly)
    {
        if (string.IsNullOrWhiteSpace(assembly.Location))
        {
            return;
        }

        references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
    }

    private static void WriteJson(string path, JsonElement rootElement)
    {
        string json = JsonSerializer.Serialize(rootElement, WriteIndentedOptions);
        File.WriteAllText(path, $"{json}{Environment.NewLine}");
    }
}
