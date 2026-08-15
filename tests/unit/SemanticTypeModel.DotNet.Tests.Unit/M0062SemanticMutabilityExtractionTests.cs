using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.DotNet.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0062SemanticMutabilityExtractionTests
{
    [Test]
    public async Task Extraction_preserves_type_property_and_field_declarations_without_access_inference()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType, SemanticImmutable]
            public sealed class Specification
            {
                public string Unspecified { get; init; } = "";
                [SemanticMutable] public string Cache { get; set; } = "";
                [SemanticMutable, SemanticName("fieldCache")] public string FieldCache = "";
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor type = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(static candidate => candidate.Name == "Specification");

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(type.Mutability).IsEqualTo(SemanticMutability.Immutable);
        _ = await Assert.That(type.Properties.Single(static property => property.Name == "Unspecified").Mutability).IsNull();
        _ = await Assert.That(type.Properties.Single(static property => property.Name == "Cache").Mutability).IsEqualTo(SemanticMutability.Mutable);
        _ = await Assert.That(type.Properties.Single(static property => property.Name == "fieldCache").Mutability).IsEqualTo(SemanticMutability.Mutable);
    }

    [Test]
    public async Task Conflicting_mutability_declarations_are_diagnosed()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType, SemanticMutable, SemanticImmutable]
            public sealed class InvalidSpecification;
            """;

        DotNetExtractionResult extraction = Extract(source);

        _ = await Assert.That(extraction.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM5048")).IsTrue();
        _ = await Assert.That(extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single().Mutability).IsNull();
    }

    private static DotNetExtractionResult Extract(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }
        AddReference(references, typeof(SemanticTypeAttribute).Assembly);
        var compilation = CSharpCompilation.Create($"M0062_{Guid.NewGuid():N}", [syntaxTree], [.. references.Values], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return new RoslynDotNetTypeExtractor().Extract(compilation);
    }

    private static void AddReference(Dictionary<string, PortableExecutableReference> references, Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
        }
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
