using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SemanticTypeModel.DotNet.Diagnostics;

namespace SemanticTypeModel.DotNet.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0065DisplayIdentityAndAccessPathTests
{
    [Test]
    public async Task Valid_groups_are_encoded_as_ordered_member_annotations()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType]
            public sealed class Customer
            {
                [SemanticDisplayIdentity(Order = 1), SemanticAccessPath("ByName", Order = 1), SemanticAccessPath("ByCustomer", Order = 0)]
                public string Name { get; init; } = "";
                [SemanticDisplayIdentity, SemanticAccessPath("ByCustomer", Order = 1)]
                public string CustomerNumber { get; init; } = "";
            }
            """;

        DotNetObjectTypeDescriptor customer = Extract(source).TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single();

        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "Name").Annotations["schema.displayIdentity"]).IsEqualTo("1");
        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "CustomerNumber").Annotations["schema.displayIdentity"]).IsEqualTo("0");
        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "Name").Annotations["schema.accessPath.ByCustomer"]).IsEqualTo("0");
        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "Name").Annotations["schema.accessPath.ByName"]).IsEqualTo("1");
        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "CustomerNumber").Annotations["schema.accessPath.ByCustomer"]).IsEqualTo("1");
        _ = await Assert.That(Extract(source).Diagnostics).IsEmpty();
    }

    [Test]
    public async Task Invalid_display_identity_is_omitted_as_a_group_and_invalid_path_does_not_remove_other_paths()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType]
            public sealed class InvalidCustomer
            {
                [SemanticDisplayIdentity(Order = -1), SemanticAccessPath("Bad Path"), SemanticAccessPath("Good", Order = 0)]
                public string Name { get; init; } = "";
                [SemanticDisplayIdentity(Order = 0), SemanticAccessPath("Good", Order = 1)]
                public string Number { get; init; } = "";
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor customer = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single();

        _ = await Assert.That(extraction.Diagnostics.Select(static diagnostic => diagnostic.Code)).Contains(DotNetExtractionDiagnosticIds.DisplayIdentityDefinitionInvalid);
        _ = await Assert.That(extraction.Diagnostics.Select(static diagnostic => diagnostic.Code)).Contains(DotNetExtractionDiagnosticIds.AccessPathDefinitionInvalid);
        _ = await Assert.That(customer.Properties.All(static property => !property.Annotations.Keys.Any(static key => key == "schema.displayIdentity"))).IsTrue();
        _ = await Assert.That(customer.Properties.All(static property => !property.Annotations.Keys.Any(static key => key == "schema.accessPath.Bad Path"))).IsTrue();
        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "Name").Annotations["schema.accessPath.Good"]).IsEqualTo("0");
        _ = await Assert.That(customer.Properties.Single(static property => property.Name == "Number").Annotations["schema.accessPath.Good"]).IsEqualTo("1");
    }

    [Test]
    public async Task Duplicate_orders_and_duplicate_same_path_membership_are_diagnosed_and_omitted()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType]
            public sealed class AmbiguousCustomer
            {
                [SemanticDisplayIdentity(Order = 0), SemanticAccessPath("ByName", Order = 0), SemanticAccessPath("ByName", Order = 0)]
                public string Name { get; init; } = "";
                [SemanticDisplayIdentity(Order = 0), SemanticAccessPath("ByName", Order = 1)]
                public string Number { get; init; } = "";
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor customer = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single();

        _ = await Assert.That(extraction.Diagnostics.Select(static diagnostic => diagnostic.Code)).Contains(DotNetExtractionDiagnosticIds.DisplayIdentityDefinitionInvalid);
        _ = await Assert.That(extraction.Diagnostics.Select(static diagnostic => diagnostic.Code)).Contains(DotNetExtractionDiagnosticIds.AccessPathDefinitionInvalid);
        _ = await Assert.That(customer.Properties.All(static property => !property.Annotations.Keys.Any(static key => key == "schema.displayIdentity"))).IsTrue();
        _ = await Assert.That(customer.Properties.All(static property => !property.Annotations.Keys.Any(static key => key == "schema.accessPath.ByName"))).IsTrue();
    }

    private static DotNetExtractionResult Extract(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException();
        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        if (!string.IsNullOrWhiteSpace(typeof(SemanticTypeAttribute).Assembly.Location))
        {
            references[typeof(SemanticTypeAttribute).Assembly.Location] = MetadataReference.CreateFromFile(typeof(SemanticTypeAttribute).Assembly.Location);
        }

        var compilation = CSharpCompilation.Create(
            "M0065_" + Guid.NewGuid().ToString("N"),
            [syntaxTree],
            [.. references.Values],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        return new RoslynDotNetTypeExtractor().Extract(compilation);
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
