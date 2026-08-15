using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.DotNet.Diagnostics;

namespace SemanticTypeModel.EFCore.Generators.Tests.Unit;

public sealed class M0060GeneratedConfigurationTests
{
    // PartialHook compilation and MultiModel ownership/name diagnostics are intentionally kept in this focused fixture.
    [Test]
    public async Task M0060_GeneratedConfiguration_mapping_hooks_inheritance_and_text_are_deterministic_and_compile()
    {
        const string source = """
            [assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Domain.Marker))]
            namespace Domain;
            public sealed class Marker { }
            public class LegacyBase { public string LegacyCode { get; set; } = ""; }
            public class Specification : LegacyBase
            {
                public StrongId Id { get; set; }
                public Mode Mode { get; set; }
                public Details Details { get; set; } = new();
                public System.Uri Url { get; set; } = new("relative", System.UriKind.Relative);
                public System.Uri? OptionalUrl { get; set; }
            }
            public sealed class ImportSpecification : Specification { public byte[] Payload { get; set; } = []; public System.ReadOnlyMemory<byte> Memory { get; set; } }
            public readonly record struct StrongId(System.Guid Value);
            public sealed class Details { public string Name { get; set; } = ""; }
            public enum Mode { One, Two }
            internal partial class SpecificationConfiguration
            {
                static partial void ConfigureBeforeGenerated(global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Specification> builder) { }
                static partial void ConfigureAfterGenerated(global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Specification> builder) { }
            }
            """;
        object manifest = Manifest("FinanceSemanticTypeModel",
        [
            Type("System.String", "String", "Scalar"), Type("System.Guid", "Guid", "Scalar"), Type("System.Uri", "Uri", "Scalar"),
            Type("Domain.StrongId", "StrongId", "Object"), Type("Domain.Mode", "Mode", "Enum"),
            Type("Domain.Details", "Details", "Object", "ValueObject"), Type("System.Byte[]", "Bytes", "Array"),
            Type("System.ReadOnlyMemory`1[System.Byte]", "Memory", "Object"),
            Type("Domain.Specification", "Specification", "Object", "Entity", properties:
            [
                Property("Id", "Domain.StrongId", key: true), Property("Mode", "Domain.Mode"),
                Property("Details", "Domain.Details", ownership: "Object"), Property("LegacyCode", "System.String", declaring: "Domain.LegacyBase"),
                Property("Url", "System.Uri"), Property("OptionalUrl", "System.Uri", nullable: true),
            ]),
            Type("Domain.ImportSpecification", "ImportSpecification", "Object", "Entity", "Domain.Specification",
            [
                Property("Id", "Domain.StrongId", key: true, declaring: "Domain.Specification"),
                Property("Payload", "System.Byte[]"), Property("Memory", "System.ReadOnlyMemory`1[System.Byte]"),
            ]),
        ]);

        GeneratorDriverRunResult first = Run(source, manifest);
        GeneratorDriverRunResult second = Run(source, manifest);
        string[] firstText = Text(first);
        string[] secondText = Text(second);

        _ = await Assert.That(first.Diagnostics).IsEmpty();
        _ = await Assert.That(firstText).IsEquivalentTo(secondText);
        _ = await Assert.That(firstText.Count(text => text.Contains("IEntityTypeConfiguration<", StringComparison.Ordinal))).IsEqualTo(2);
        _ = await Assert.That(firstText.Any(text => text.Contains("DetailsConfiguration", StringComparison.Ordinal))).IsFalse();
        _ = await Assert.That(firstText.Any(text => text.Contains("ModeConfiguration", StringComparison.Ordinal))).IsFalse();
        string combined = string.Join("\n", firstText);
        _ = await Assert.That(combined).Contains("HasConversion<string>()");
        _ = await Assert.That(combined).Contains("SemanticEfValueConverters.Json<");
        _ = await Assert.That(combined).Contains("SemanticEfValueConverters.Uri()");
        _ = await Assert.That(combined).Contains("SemanticEfValueConverters.NullableUri()");
        _ = await Assert.That(combined).Contains("new global::Domain.StrongId(value)");
        _ = await Assert.That(combined).Contains("value.ToArray()");
        _ = await Assert.That(combined).Contains("IsRequired(true)");
        _ = await Assert.That(combined).Contains("IsRequired(false)");
        _ = await Assert.That(combined.CountOccurrences("entity.LegacyCode")).IsEqualTo(1);
        _ = await Assert.That(combined).Contains("UseTptMappingStrategy()");
        _ = await Assert.That(combined).Contains("ConfigureBeforeGenerated(builder)");
        _ = await Assert.That(combined).Contains("ConfigureAfterGenerated(builder)");
        string registration = firstText.Single(text => text.Contains("ApplyFinanceSemanticModel", StringComparison.Ordinal));
        _ = await Assert.That(registration.IndexOf("SpecificationConfiguration", StringComparison.Ordinal)).IsLessThan(registration.IndexOf("ImportSpecificationConfiguration", StringComparison.Ordinal));
    }

    [Test]
    public async Task EFCoreGenerator_reports_manifest_presence_format_and_version_diagnostics()
    {
        const string selection = "[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Marker))] public class Marker {}";
        GeneratorDriverRunResult unsupported = Run(selection, Manifest("Bad", [], 99));
        GeneratorDriverRunResult missing = RunWithoutManifest(selection);
        GeneratorDriverRunResult invalid = RunCore("[assembly: System.Reflection.AssemblyMetadata(\"SemanticTypeModel.Manifest\", \"not-base64\")]\n" + selection);
        GeneratorDriverRunResult ambiguous = RunCore(Metadata(Manifest("One", [])) + Metadata(Manifest("Two", [])) + selection);
        GeneratorDriverRunResult mismatch = Run(selection, Manifest("Mismatch", [], semanticTypeModelVersion: "999.0.0"));
        _ = await Assert.That(unsupported.Has(DotNetExtractionDiagnosticIds.EfManifestVersionUnsupported)).IsTrue();
        _ = await Assert.That(missing.Has(DotNetExtractionDiagnosticIds.EfSelectedManifestMissing)).IsTrue();
        _ = await Assert.That(invalid.Has(DotNetExtractionDiagnosticIds.EfSelectedManifestInvalid)).IsTrue();
        _ = await Assert.That(ambiguous.Has(DotNetExtractionDiagnosticIds.EfSelectedManifestAmbiguous)).IsTrue();
        _ = await Assert.That(mismatch.Has(DotNetExtractionDiagnosticIds.EfManifestSuiteVersionMismatch)).IsTrue();
    }

    [Test]
    public async Task EFCoreGenerator_reports_ownership_name_resolution_and_projection_diagnostics()
    {
        const string duplicateSelection = "[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Marker))] [assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Marker))] public class Marker {} public class Entity { public int Id {get;set;} }";
        object duplicate = Manifest("Duplicate", [Type("Entity", "Entity", "Object", "Entity", properties: [Property("Id", "System.Int32", key: true)]), Type("System.Int32", "Int32", "Scalar")]);
        GeneratorDriverRunResult ownership = Run(duplicateSelection, duplicate);

        const string collisionSource = "[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Marker))] public class Marker {} namespace A { public class Widget { public int Id {get;set;} } } namespace B { public class Widget { public int Id {get;set;} } }";
        object collisionManifest = Manifest("Names", [Type("System.Int32", "Int32", "Scalar"), Entity("A.Widget"), Entity("B.Widget")]);
        GeneratorDriverRunResult configurationCollision = Run(collisionSource, collisionManifest);

        GeneratorDriverRunResult unresolvedType = Run("[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Marker))] public class Marker {}", Manifest("Missing", [Entity("Missing.Entity")]));
        GeneratorDriverRunResult unresolvedMember = Run("[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Entity))] public class Entity { public int Id {get;set;} }", Manifest("Member", [Type("System.Int32", "Int32", "Scalar"), Type("Entity", "Entity", "Object", "Entity", properties: [Property("Id", "System.Int32", key: true), Property("Missing", "System.Int32")])]));
        GeneratorDriverRunResult projection = Run("[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(Entity))] public class Entity { public int Id {get;set;} public Other Value {get;set;} = new(); } public class Other {}", Manifest("Projection", [Type("System.Int32", "Int32", "Scalar"), Type("Other", "Other", "Object"), Type("Entity", "Entity", "Object", "Entity", properties: [Property("Id", "System.Int32", key: true), Property("Value", "Other")])]));

        MetadataReference sales = ModelReference("SalesA", "Sales-Model", "SalesA.Record");
        MetadataReference salesCollision = ModelReference("SalesB", "Sales_Model", "SalesB.Record");
        const string registrationSource = "[assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(SalesA.Marker))] [assembly: SemanticTypeModel.EFCore.GenerateSemanticEfModel(typeof(SalesB.Marker))]";
        GeneratorDriverRunResult registrationCollision = RunCore(registrationSource, [sales, salesCollision]);

        _ = await Assert.That(ownership.Has(DotNetExtractionDiagnosticIds.EfEntityOwnershipCollision)).IsTrue();
        _ = await Assert.That(configurationCollision.Has(DotNetExtractionDiagnosticIds.EfConfigurationNameCollision)).IsTrue();
        _ = await Assert.That(registrationCollision.Has(DotNetExtractionDiagnosticIds.EfRegistrationNameCollision)).IsTrue();
        _ = await Assert.That(unresolvedType.Has(DotNetExtractionDiagnosticIds.EfClrTypeUnresolved)).IsTrue();
        _ = await Assert.That(unresolvedMember.Has(DotNetExtractionDiagnosticIds.EfClrMemberUnresolved)).IsTrue();
        _ = await Assert.That(projection.Has(DotNetExtractionDiagnosticIds.EfProjectionError)).IsTrue();
    }

    private static GeneratorDriverRunResult Run(string source, object manifest)
    {
        return RunCore(Metadata(manifest) + source);
    }

    private static GeneratorDriverRunResult RunWithoutManifest(string source)
    {
        return RunCore(source);
    }

    private static GeneratorDriverRunResult RunCore(string source, IEnumerable<MetadataReference>? additionalReferences = null)
    {
        ImmutableArray<MetadataReference> references = [.. References().Concat(additionalReferences ?? [])];
        CSharpCompilation compilation = CSharpCompilation.Create("GeneratorTest", [CSharpSyntaxTree.ParseText(source)], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Diagnostic[] initialErrors = [.. compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id != "CS0759")];
        if (initialErrors.Length > 0) throw new InvalidOperationException(string.Join(" | ", initialErrors.Select(static error => error.ToString())));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SemanticEfConfigurationGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);
        Diagnostic[] generatedErrors = [.. output.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Id.StartsWith("CS", StringComparison.Ordinal))];
        if (generatedErrors.Length > 0) throw new InvalidOperationException(string.Join(" | ", generatedErrors.Select(static error => error.ToString())) + " | Generator: " + string.Join(" | ", driver.GetRunResult().Diagnostics));
        using var stream = new MemoryStream();
        if (!output.Emit(stream).Success) throw new InvalidOperationException("Generated compilation did not emit successfully.");
        return driver.GetRunResult();
    }

    private static MetadataReference ModelReference(string assemblyName, string modelName, string entityName)
    {
        string ns = entityName[..entityName.LastIndexOf('.')];
        string type = entityName[(entityName.LastIndexOf('.') + 1)..];
        object manifest = Manifest(modelName, [Type("System.Int32", "Int32", "Scalar"), Entity(entityName)]);
        string source = Metadata(manifest) + $"namespace {ns}; public class Marker {{ }} public class {type} {{ public int Id {{get;set;}} }}";
        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, [CSharpSyntaxTree.ParseText(source)], References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        EmitResult emit = compilation.Emit(stream);
        if (!emit.Success) throw new InvalidOperationException(string.Join(" | ", emit.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static ImmutableArray<MetadataReference> References()
    {
        IEnumerable<string> trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        string[] extra = [typeof(ModelBuilder).Assembly.Location, typeof(RelationalEntityTypeBuilderExtensions).Assembly.Location, typeof(GenerateSemanticEfModelAttribute).Assembly.Location];
        return [.. trusted.Concat(extra).Distinct(StringComparer.Ordinal).Select(static path => MetadataReference.CreateFromFile(path))];
    }

    private static string Metadata(object manifest)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest)));
        return $"[assembly: System.Reflection.AssemblyMetadata(\"SemanticTypeModel.Manifest\", \"{encoded}\")]\n";
    }

    private static string[] Text(GeneratorDriverRunResult result)
    {
        return [.. result.Results.SelectMany(run => run.GeneratedSources).OrderBy(source => source.HintName, StringComparer.Ordinal).Select(source => source.SourceText.ToString())];
    }

    private static object Manifest(string name, object[] types, int version = 1, string? semanticTypeModelVersion = null)
    {
        return new { Version = version, SemanticTypeModelVersion = semanticTypeModelVersion ?? SuiteVersion(), ModelName = name, Types = types };
    }

    private static string SuiteVersion()
    {
        return typeof(SemanticEfConfigurationGenerator).Assembly
        .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
        .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().Single().InformationalVersion.Split('+')[0];
    }

    private static object Entity(string name)
    {
        return Type(name, name.Split('.').Last(), "Object", "Entity", properties: [Property("Id", "System.Int32", key: true)]);
    }

    private static object Type(string id, string name, string kind, string? role = null, string? baseClr = null, object[]? properties = null)
    {
        return new { Id = id, Name = name, ClrName = id, BaseClrName = baseClr, Kind = kind, Role = role, ItemTypeId = (string?)null, Properties = properties ?? [] };
    }

    private static object Property(string name, string type, bool key = false, string? declaring = null, string? ownership = null, bool nullable = false)
    {
        return new { Name = name, MemberName = name, DeclaringClrName = declaring, TypeId = type, IsRequired = !nullable, IsNullable = nullable, IsPrimaryKey = key, KeyOrder = 0, Ownership = ownership, IsExtensionData = false };
    }
}

internal static class GeneratorTestExtensions
{
    internal static bool Has(this GeneratorDriverRunResult result, string id)
    {
        return result.Diagnostics.Any(diagnostic => diagnostic.Id == id);
    }

    internal static int CountOccurrences(this string value, string text)
    {
        return value.Split(text, StringSplitOptions.None).Length - 1;
    }
}
