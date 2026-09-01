using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Diagnostics;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.DotNet.Diagnostics;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.Generators.Tests.Unit;

/// <summary>
/// Verifies diagnostic ID stability and uniqueness across all STM packages,
/// and that the source generator emits diagnostics with stable codes.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class DiagnosticIdStabilityTests
{
    [Test]
    public async Task DotNetExtractionDiagnosticIds_should_have_no_duplicate_values()
    {
        IReadOnlyList<string> ids = CollectStringConstants(typeof(DotNetExtractionDiagnosticIds));
        var duplicates = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToList();

        _ = await Assert.That(duplicates).IsEmpty()
            .Because($"Duplicate diagnostic IDs found in DotNetExtractionDiagnosticIds: {string.Join(", ", duplicates)}");
    }

    [Test]
    public async Task DotNetExtractionDiagnosticIds_should_all_use_stm5xxx_prefix()
    {
        IReadOnlyList<string> ids = CollectStringConstants(typeof(DotNetExtractionDiagnosticIds));

        foreach (string id in ids)
        {
            var isValid = id.StartsWith("STM5", StringComparison.Ordinal)
                && id.Length == 7
                && id[3..].All(char.IsDigit);

            _ = await Assert.That(isValid).IsTrue()
                .Because($"Diagnostic ID '{id}' does not match the STM5xxx format.");
        }
    }

    [Test]
    public async Task All_stm_diagnostic_ids_across_packages_should_be_unique()
    {
        // Collect all known STM codes from both ID classes.
        IReadOnlyList<string> coreIds = CollectStringConstants(typeof(StmDiagnosticIds));
        IReadOnlyList<string> dotNetIds = CollectStringConstants(typeof(DotNetExtractionDiagnosticIds));

        var all = coreIds.Concat(dotNetIds).ToList();
        var duplicates = all
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToList();

        _ = await Assert.That(duplicates).IsEmpty()
            .Because($"Duplicate diagnostic IDs found across packages: {string.Join(", ", duplicates)}");
    }

    [Test]
    public async Task Generator_should_emit_stm5008_for_invalid_discovery_mode()
    {
        // Arrange: build a compilation with an invalid SemanticTypeModelDiscoveryMode option.
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Widget
            {
                public required string Name { get; init; }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunGeneratorForDiagnostics(
            source,
            options: new Dictionary<string, string>
            {
                ["build_property.SemanticTypeModelDiscoveryMode"] = "InvalidModeValue",
            });

        Diagnostic? stm5008 = diagnostics.FirstOrDefault(
            static d => string.Equals(d.Id, DotNetExtractionDiagnosticIds.UnsupportedDiscoveryMode, StringComparison.Ordinal));

        _ = await Assert.That(stm5008).IsNotNull()
            .Because("STM5008 should be emitted for an invalid discovery mode option.");
        _ = await Assert.That(stm5008!.Severity).IsEqualTo(DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task Generator_should_emit_stm5018_for_invalid_naming_policy()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Widget
            {
                public required string Name { get; init; }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunGeneratorForDiagnostics(
            source,
            options: new Dictionary<string, string>
            {
                ["build_property.SemanticTypeModelNamingPolicy"] = "InvalidPolicyValue",
            });

        Diagnostic? stm5018 = diagnostics.FirstOrDefault(
            static d => string.Equals(d.Id, DotNetExtractionDiagnosticIds.UnsupportedNamingPolicy, StringComparison.Ordinal));

        _ = await Assert.That(stm5018).IsNotNull()
            .Because("STM5018 should be emitted for an invalid naming policy option.");
        _ = await Assert.That(stm5018!.Severity).IsEqualTo(DiagnosticSeverity.Warning);
    }

    [Test]
    public async Task Extractor_should_register_dictionary_key_and_value_types_for_extension_data()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Text.Json;
            using SemanticTypeModel.DotNet;

            [SemanticType(SemanticTypeRole.Entity)]
            public sealed class ExternalRecord
            {
                [SemanticKey]
                public required Guid Id { get; init; }

                [SemanticExtensionData]
                public Dictionary<string, JsonElement>? ExtensionData { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);

        DotNetDictionaryTypeDescriptor dictionary = extraction.TypesById.Values
            .OfType<DotNetDictionaryTypeDescriptor>()
            .Single(static descriptor => descriptor.Id.Contains("Dictionary", StringComparison.Ordinal));

        _ = await Assert.That(dictionary.KeyTypeId).IsEqualTo("string");
        _ = await Assert.That(dictionary.ValueTypeId).IsEqualTo("global::System.Text.Json.JsonElement");
        _ = await Assert.That(extraction.TypesById.ContainsKey(dictionary.KeyTypeId)).IsTrue()
            .Because("the 2.4.0 defect referenced global::System.String as a dictionary key without registering it, which later produced STM0002.");
        _ = await Assert.That(extraction.TypesById.ContainsKey(dictionary.ValueTypeId)).IsTrue();
    }

    [Test]
    public async Task Extractor_should_register_supported_ordinary_dictionary_key_types()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Text.Json;
            using SemanticTypeModel.DotNet;

            [SemanticType(SemanticTypeRole.Entity)]
            public sealed class DictionaryRecord
            {
                [SemanticKey]
                public required Guid Id { get; init; }

                public Dictionary<string, string> TextByName { get; init; } = new();
                public Dictionary<int, decimal> AmountByNumber { get; init; } = new();
                public Dictionary<Guid, JsonElement> DataById { get; init; } = new();
            }
            """;

        DotNetExtractionResult extraction = Extract(source);

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(extraction.TypesById.ContainsKey("string")).IsTrue();
        _ = await Assert.That(extraction.TypesById.ContainsKey("int")).IsTrue();
        _ = await Assert.That(extraction.TypesById.ContainsKey("global::System.Guid")).IsTrue();
        _ = await Assert.That(extraction.TypesById.ContainsKey("decimal")).IsTrue();
        _ = await Assert.That(extraction.TypesById.ContainsKey("global::System.Text.Json.JsonElement")).IsTrue();
    }

    [Test]
    public async Task Extractor_should_support_Uri_as_a_default_reference_formatted_scalar_and_preserve_STM5025()
    {
        const string source = """
            using System;
            using SemanticTypeModel.DotNet;

            [SemanticType(SemanticTypeRole.Entity)]
            public sealed class WebsiteRecord
            {
                [SemanticKey]
                public required Guid Id { get; init; }
                public required Uri Website { get; init; }
                [SemanticFormat(SemanticScalarFormat.Uri)]
                public Uri? OptionalWebsite { get; init; }
                [SemanticFormat(SemanticScalarFormat.Uri)]
                public required string WebsiteText { get; init; }
                [SemanticFormat(SemanticScalarFormat.Uri)]
                public int InvalidWebsite { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor record = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(static type => type.Name == "WebsiteRecord");

        _ = await Assert.That(extraction.TypesById.Values.OfType<DotNetScalarTypeDescriptor>().Any(static type => type.Name == "Uri" && type.ScalarKind == DotNetScalarKind.String && type.Format == "uri-reference")).IsTrue();
        _ = await Assert.That(record.Properties.Single(static property => property.Name == "Website").Annotations["schema.format"]).IsEqualTo("uri-reference");
        _ = await Assert.That(record.Properties.Single(static property => property.Name == "OptionalWebsite").IsNullable).IsTrue();
        _ = await Assert.That(record.Properties.Single(static property => property.Name == "OptionalWebsite").TypeId).IsEqualTo("global::System.Uri");
        _ = await Assert.That(record.Properties.Single(static property => property.Name == "OptionalWebsite").Annotations["schema.format"]).IsEqualTo("uri");
        _ = await Assert.That(record.Properties.Single(static property => property.Name == "WebsiteText").Annotations["schema.format"]).IsEqualTo("uri");
        _ = await Assert.That(extraction.Diagnostics.Count(static diagnostic => diagnostic.Code == "STM5025")).IsEqualTo(1);
    }

    [Test]
    public async Task Generator_should_diagnose_explicit_owned_kind_that_conflicts_with_CLR_shape()
    {
        const string source = """
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            [SemanticType(SemanticTypeRole.Entity)]
            public sealed class InvalidOwner
            {
                [SemanticOwned(Kind = SemanticOwnershipKind.Object)]
                public List<InvalidTarget> Targets { get; init; } = new();
            }

            [SemanticType(SemanticTypeRole.ValueObject)]
            public sealed class InvalidTarget { }
            """;

        Diagnostic[] diagnostics = RunGeneratorForDiagnostics(source);

        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == DotNetExtractionDiagnosticIds.MemberShapeUnsupported)).IsTrue();
    }

    [Test]
    public async Task M0058_TypedLiteral_enum_required_when_is_normalized_against_source_type()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            public enum ImportType { CsvFile, XmlFile, WebService1, WebService2 }
            [SemanticType(SemanticTypeRole.Entity)]
            public sealed class ImportSpecification
            {
                public ImportType ImportType { get; init; }
                [SemanticRequiredWhen(nameof(ImportType), nameof(ImportType.CsvFile))]
                public string? CsvSource { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        DotNetObjectTypeDescriptor import = extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>()
            .Single(static type => type.Name == "ImportSpecification");
        SemanticTypeModel.Abstractions.Model.ConditionalConstraint constraint = import.Properties
            .Single(static property => property.Name == "CsvSource").ConditionalConstraints.Single();

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(constraint.SourcePropertyName).IsEqualTo("ImportType");
        _ = await Assert.That(constraint.Literal.Kind).IsEqualTo(SemanticLiteralKind.EnumMember);
        _ = await Assert.That(constraint.Literal.EnumMemberName).IsEqualTo("CsvFile");
        _ = await Assert.That(constraint.Literal.EnumTypeId).IsEqualTo(constraint.SourceTypeId);
        _ = await Assert.That(extraction.TypesById[constraint.SourceTypeId.Value]).IsTypeOf<DotNetEnumTypeDescriptor>();
    }

    [Test]
    public async Task EnumLiteral_invalid_member_emits_stable_diagnostic()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            public enum ImportType { CsvFile }
            [SemanticType]
            public sealed class ImportSpecification
            {
                public ImportType ImportType { get; init; }
                [SemanticRequiredWhen(nameof(ImportType), "DoesNotExist")]
                public string? CsvSource { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        _ = await Assert.That(extraction.Diagnostics.Any(static diagnostic =>
            diagnostic.Code == DotNetExtractionDiagnosticIds.TypedLiteralEnumMemberNotFound)).IsTrue();
    }

    [Test]
    public async Task TypedLiteral_scalar_matrix_preserves_string_boolean_integer_and_null_kinds()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType]
            public sealed class Rules
            {
                public string Mode { get; init; } = "";
                public bool Enabled { get; init; }
                public int Priority { get; init; }
                public int? Optional { get; init; }
                [SemanticRequiredWhen(nameof(Mode), "CsvFile")] public string? ByMode { get; init; }
                [SemanticRequiredWhen(nameof(Enabled), "true")] public string? ByEnabled { get; init; }
                [SemanticRequiredWhen(nameof(Priority), "10")] public string? ByPriority { get; init; }
                [SemanticRequiredWhen(nameof(Optional), "null")] public string? ByNull { get; init; }
            }
            """;

        DotNetObjectTypeDescriptor rules = Extract(source).TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single();
        SemanticTypeModel.Abstractions.Model.SemanticLiteralKind[] kinds = [.. rules.Properties
            .Where(static property => property.ConditionalConstraints.Count > 0)
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .Select(static property => property.ConditionalConstraints.Single().Literal.Kind)];

        _ = await Assert.That(kinds).IsEquivalentTo([
            SemanticLiteralKind.Boolean,
            SemanticLiteralKind.String,
            SemanticLiteralKind.Null,
            SemanticLiteralKind.Integer]);
    }

    [Test]
    public async Task TypedLiteral_invalid_matrix_emits_specific_stable_diagnostics()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType] public sealed class Complex { }
            [SemanticType] public sealed class Rules
            {
                public bool Enabled { get; init; }
                public byte Count { get; init; }
                public int Number { get; init; }
                public System.DateOnly Date { get; init; }
                public Complex Complex { get; init; } = new();
                [SemanticRequiredWhen("NoSuchProperty", "x")] public string? Missing { get; init; }
                [SemanticRequiredWhen(nameof(Enabled), "yes")] public string? Boolean { get; init; }
                [SemanticRequiredWhen(nameof(Count), "256")] public string? Overflow { get; init; }
                [SemanticRequiredWhen(nameof(Number), "one")] public string? Numeric { get; init; }
                [SemanticRequiredWhen(nameof(Number), "null")] public string? Null { get; init; }
                [SemanticRequiredWhen(nameof(Date), "not-a-date")] public string? InvalidDate { get; init; }
                [SemanticRequiredWhen(nameof(Complex), "x")] public string? Unsupported { get; init; }
            }
            """;

        string[] codes = [.. Extract(source).Diagnostics.Select(static diagnostic => diagnostic.Code)];
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralSourceNotFound);
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralBooleanInvalid);
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralNumericOverflow);
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralNumericFormatInvalid);
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralNullNotAllowed);
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralValueInvalid);
        _ = await Assert.That(codes).Contains(DotNetExtractionDiagnosticIds.TypedLiteralSourceTypeUnsupported);
    }

    [Test]
    public async Task TypedLiteral_temporal_matrix_uses_invariant_normalization()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            [SemanticType] public sealed class Rules
            {
                public System.DateOnly Date { get; init; }
                public System.TimeOnly Time { get; init; }
                public System.DateTimeOffset Timestamp { get; init; }
                public System.TimeSpan Duration { get; init; }
                [SemanticRequiredWhen(nameof(Date), "2026-08-04")] public string? ByDate { get; init; }
                [SemanticRequiredWhen(nameof(Time), "13:14:15")] public string? ByTime { get; init; }
                [SemanticRequiredWhen(nameof(Timestamp), "2026-08-04T13:14:15+00:00")] public string? ByTimestamp { get; init; }
                [SemanticRequiredWhen(nameof(Duration), "01:02:03")] public string? ByDuration { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);
        SemanticLiteral[] literals = [.. extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single().Properties
            .Where(static property => property.ConditionalConstraints.Count != 0)
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .Select(static property => property.ConditionalConstraints.Single().Literal)];

        _ = await Assert.That(extraction.Diagnostics).IsEmpty();
        _ = await Assert.That(literals.Select(static literal => literal.Kind)).IsEquivalentTo([
            SemanticLiteralKind.Date,
            SemanticLiteralKind.Duration,
            SemanticLiteralKind.Time,
            SemanticLiteralKind.DateTimeOffset]);
        _ = await Assert.That(literals.Select(static literal => literal.NormalizedText)).IsEquivalentTo([
            "2026-08-04", "01:02:03", "13:14:15.0000000", "2026-08-04T13:14:15.0000000+00:00"]);
    }

    [Test]
    public async Task StrongIdentifier_literal_has_deterministic_unsupported_policy()
    {
        const string source = """
            using SemanticTypeModel.DotNet;
            public readonly record struct TenantId(System.Guid Value);
            [SemanticType] public sealed class Rules
            {
                public TenantId TenantId { get; init; }
                [SemanticRequiredWhen(nameof(TenantId), "8e531df9-c034-4b73-b2f6-879f2d254f4f")] public string? TenantRule { get; init; }
            }
            """;

        DotNetExtractionResult extraction = Extract(source);

        _ = await Assert.That(extraction.Diagnostics.Any(static diagnostic => diagnostic.Code == DotNetExtractionDiagnosticIds.TypedLiteralSourceTypeUnsupported)).IsTrue();
        _ = await Assert.That(extraction.TypesById.Values.OfType<DotNetObjectTypeDescriptor>().Single(static type => type.Name == "Rules").Properties.Single(static property => property.Name == "TenantRule").ConditionalConstraints).IsEmpty();
    }

    private static Diagnostic[] RunGeneratorForDiagnostics(
        string source,
        IReadOnlyDictionary<string, string>? options = null)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        AddAssemblyReference(references, typeof(object).Assembly);
        AddAssemblyReference(references, typeof(Enumerable).Assembly);
        AddAssemblyReference(references, typeof(SemanticTypeAttribute).Assembly);
        AddAssemblyReference(references, typeof(SemanticTypeModelSourceGenerator).Assembly);
        AddAssemblyReference(references, typeof(SemanticTypeModel.Abstractions.Model.TypeSchemaModel).Assembly);
        AddAssemblyReference(references, typeof(JsonSchemaExporter).Assembly);
        AddAssemblyReference(references, typeof(System.Text.Json.JsonDocument).Assembly);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: $"SemanticTypeModel.DiagnosticTest_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: [.. references.Values],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        IIncrementalGenerator generator = new SemanticTypeModelSourceGenerator();
        CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        AnalyzerConfigOptionsProvider optionsProvider = new TestOptionsProvider(options);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider);

        // Diagnostics are collected via GetRunResult() below; the updated compilation and
        // per-run diagnostic array from RunGeneratorsAndUpdateCompilation are intentionally discarded.
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> _);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return runResult.Results.SelectMany(static result => result.Diagnostics).ToArray();
    }

    private static DotNetExtractionResult Extract(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        AddAssemblyReference(references, typeof(object).Assembly);
        AddAssemblyReference(references, typeof(Enumerable).Assembly);
        AddAssemblyReference(references, typeof(SemanticTypeAttribute).Assembly);
        AddAssemblyReference(references, typeof(System.Text.Json.JsonElement).Assembly);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: $"SemanticTypeModel.ExtractionTest_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: [.. references.Values],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return new RoslynDotNetTypeExtractor().Extract(compilation);
    }

    private static void AddAssemblyReference(Dictionary<string, PortableExecutableReference> references, Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
        }
    }

    private static IReadOnlyList<string> CollectStringConstants(Type type)
    {
        return
        [
            .. type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(static field => (string)field.GetRawConstantValue()!)
                .Where(static value => value is not null),
        ];
    }

    private sealed class TestOptionsProvider(IReadOnlyDictionary<string, string>? values) : AnalyzerConfigOptionsProvider
    {
        private readonly TestConfigOptions _global = new(values ?? new Dictionary<string, string>(StringComparer.Ordinal));

        public override AnalyzerConfigOptions GlobalOptions => _global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return _global;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return _global;
        }
    }

    private sealed class TestConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (values.TryGetValue(key, out string? configured))
            {
                value = configured;
                return true;
            }

            value = null;
            return false;
        }
    }
}
