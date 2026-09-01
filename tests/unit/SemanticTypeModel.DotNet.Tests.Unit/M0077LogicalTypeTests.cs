using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SemanticTypeModel.DotNet.Diagnostics;


namespace SemanticTypeModel.DotNet.Tests.Unit;

public sealed class M0077LogicalTypeTests
{
    [Test]
    public async Task Logical_type_is_property_annotation_on_ordinary_scalar()
    {
        DotNetExtractionResult result = Extract("""
            using System;
            using SemanticTypeModel.DotNet;
            [SemanticType] public sealed class Customer { [SemanticLogicalType("CustomerId")] public Guid Id { get; init; } }
            """);
        DotNetObjectTypeDescriptor customer = result.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(t => t.Name == "Customer");
        _ = await Assert.That(result.Diagnostics).IsEmpty();
        _ = await Assert.That(customer.Properties.Single().Annotations["schema.logicalType"]).IsEqualTo("CustomerId");
    }

    [Test]
    public async Task Invalid_logical_type_authoring_reports_STM5052()
    {
        DotNetExtractionResult result = Extract("""
            using SemanticTypeModel.DotNet;
            [SemanticType] public sealed class Customer { [SemanticLogicalType("bad name")] public string Id { get; init; } }
            """);
        _ = await Assert.That(result.Diagnostics.Any(d => d.Code == DotNetExtractionDiagnosticIds.LogicalTypeDefinitionInvalid)).IsTrue();
    }

    private static DotNetExtractionResult Extract(string source)
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException();
        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }
        references[typeof(SemanticTypeAttribute).Assembly.Location] = MetadataReference.CreateFromFile(typeof(SemanticTypeAttribute).Assembly.Location);
        return new RoslynDotNetTypeExtractor().Extract(CSharpCompilation.Create("M0077", [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))], [.. references.Values], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)));
    }
}
