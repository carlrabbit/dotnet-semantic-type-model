using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SemanticTypeModel.DotNet.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0068StrongScalarTests
{
    [Test]
    public async Task Explicit_readonly_wrapper_is_extracted_as_strong_scalar()
    {
        DotNetExtractionResult extraction = Extract("""
            using SemanticTypeModel.DotNet;
            [SemanticType, SemanticStrongScalar]
            public readonly record struct SpecificationVersionId(System.Guid Value);
            """);


        DotNetStrongScalarTypeDescriptor strong = extraction.TypesById.Values.OfType<DotNetStrongScalarTypeDescriptor>().Single();
        _ = await Assert.That(strong.ValueTypeId).IsEqualTo("global::System.Guid");
        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task Invalid_wrapper_is_rejected_without_inference()
    {
        DotNetExtractionResult extraction = Extract("""
            using SemanticTypeModel.DotNet;
            [SemanticType]
            public struct CustomerId { public System.Guid Value { get; set; } public int Extra { get; } }
            """);

        _ = await Assert.That(extraction.TypesById.Values.OfType<DotNetStrongScalarTypeDescriptor>()).IsEmpty();
        _ = await Assert.That(extraction.Diagnostics.Select(static diagnostic => diagnostic.Code)).DoesNotContain("STM5051");
    }

    private static DotNetExtractionResult Extract(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException();
        var references = trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(static path => MetadataReference.CreateFromFile(path)).ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(SemanticTypeAttribute).Assembly.Location));
        var compilation = CSharpCompilation.Create("M0068_" + Guid.NewGuid().ToString("N"), [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return new RoslynDotNetTypeExtractor().Extract(compilation);
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
