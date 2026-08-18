using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.DotNet.Tests.Unit;

#pragma warning disable CS1591
public sealed class M0058TypedLiteralExtractionTests
{
    [Test]
    public async Task Configuration_role_remains_projection_neutral_without_options_metadata()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType(SemanticTypeRole.Configuration)] public sealed class Settings { public string? Name { get; init; } }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor settings = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(static type => type.Name == "Settings");

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(settings.Annotations["schema.role"]).IsEqualTo("Configuration");
        _ = await Assert.That(settings.Annotations.Keys.Any(static key => key.StartsWith("configuration.", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task Import_specification_extracts_four_typed_enum_conditions_without_object_pollution()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            public enum ImportType { CsvFile, XmlFile, WebService1, WebService2 }
            [SemanticType(SemanticTypeRole.ValueObject)] public sealed record CsvSourceSpecification;
            [SemanticType(SemanticTypeRole.ValueObject)] public sealed record XmlSourceSpecification;
            [SemanticType(SemanticTypeRole.ValueObject)] public sealed record WebServiceSource;
            [SemanticType(SemanticTypeRole.ValueObject)] public sealed record PostProcessing;
            [SemanticType(SemanticTypeRole.Entity)] public sealed class ImportSpecification
            {
                public ImportType ImportType { get; init; }
                public ImportType? OptionalImportType { get; init; }
                [SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.CsvFile))] public CsvSourceSpecification? CsvSource { get; init; }
                [SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.XmlFile))] public XmlSourceSpecification? XmlSource { get; init; }
                [SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.WebService1))] public WebServiceSource? WebService1Source { get; init; }
                [SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.WebService2))] public WebServiceSource? WebService2Source { get; init; }
                public PostProcessing? PostProcessing { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor import = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(static type => type.Name == "ImportSpecification");
        ConditionalConstraint[] constraints = [.. import.Properties.SelectMany(static property => property.ConditionalConstraints)];

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(constraints.Length).IsEqualTo(4);
        _ = await Assert.That(constraints.All(static constraint => constraint.Literal.Kind == SemanticLiteralKind.EnumMember && constraint.Literal.EnumTypeId == constraint.SourceTypeId)).IsTrue();
        _ = await Assert.That(extraction.TypesById[constraints[0].SourceTypeId.Value]).IsTypeOf<DotNetEnumTypeDescriptor>();
        _ = await Assert.That(import.Properties.Single(static property => property.Name == "OptionalImportType").IsNullable).IsTrue();
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
        var compilation = CSharpCompilation.Create($"M0058_{Guid.NewGuid():N}", [syntaxTree], [.. references.Values], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
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
#pragma warning restore CS1591
