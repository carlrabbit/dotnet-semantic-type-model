using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.Generators.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0074ScalarRepresentationTests
{
    [Test]
    public async Task Extractor_should_cover_the_supported_scalar_representation_submatrix()
    {
        const string source = """
            using System;
            using System.Text.Json;
            using System.Text.Json.Nodes;
            using SemanticTypeModel.DotNet;

            [SemanticType(SemanticTypeRole.Entity)]
            public sealed class ScalarRepresentations
            {
                [SemanticKey] public Guid Id { get; init; }
                public char Character { get; init; }
                public ReadOnlyMemory<byte> Memory { get; init; }
                public JsonDocument Document { get; init; } = JsonDocument.Parse("null");
                public JsonElement Element { get; init; }
                public JsonNode? Node { get; init; }
                public Uri RelativeUri { get; init; } = new("relative", UriKind.Relative);
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor record = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(type => type.Name == "ScalarRepresentations");
        DotNetScalarTypeDescriptor scalar(string name)
        {
            return extraction.TypesById.Values.OfType<DotNetScalarTypeDescriptor>().Single(type => type.Name == name);
        }

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(record.Properties.Single(property => property.Name == "Character").TypeId).IsEqualTo("char");
        _ = await Assert.That(scalar("Char").ScalarKind).IsEqualTo(DotNetScalarKind.String);
        _ = await Assert.That(scalar("ReadOnlyMemory").ScalarKind).IsEqualTo(DotNetScalarKind.Binary);
        _ = await Assert.That(scalar("JsonDocument").ScalarKind).IsEqualTo(DotNetScalarKind.Json);
        _ = await Assert.That(scalar("JsonElement").ScalarKind).IsEqualTo(DotNetScalarKind.Json);
        _ = await Assert.That(scalar("JsonNode").ScalarKind).IsEqualTo(DotNetScalarKind.Json);
        _ = await Assert.That(scalar("Uri").Format).IsEqualTo("uri-reference");
        _ = await Assert.That(record.Properties.Single(property => property.Name == "RelativeUri").Annotations["schema.format"]).IsEqualTo("uri-reference");
    }

    private static DotNetExtractionResult Extract(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException();
        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        AddAssemblyReference(references, typeof(object).Assembly);
        AddAssemblyReference(references, typeof(SemanticTypeAttribute).Assembly);
        AddAssemblyReference(references, typeof(JsonElement).Assembly);
        CSharpCompilation compilation = CSharpCompilation.Create(
            $"SemanticTypeModel.M0074_{Guid.NewGuid():N}", [syntaxTree], [.. references.Values],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return new RoslynDotNetTypeExtractor().Extract(compilation);
    }

    private static void AddAssemblyReference(Dictionary<string, PortableExecutableReference> references, Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
        }
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
