using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Generators;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.Samples.DotNetGenerator;

internal static class Program
{
    private static void Main()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                public required string Id { get; init; }

                public required string Name { get; init; }
            }
            """;

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
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(model);

        Console.WriteLine($"generated root: {model.RootIdentifier}");
        Console.WriteLine(exported.Document.RootElement.GetRawText());
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
        // Single-file or in-memory load contexts may not provide a physical assembly location.
        if (string.IsNullOrWhiteSpace(assembly.Location))
        {
            return;
        }

        references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
    }
}
