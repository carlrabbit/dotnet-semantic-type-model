using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
        var coreIds = CollectStringConstants(typeof(StmDiagnosticIds));
        var dotNetIds = CollectStringConstants(typeof(DotNetExtractionDiagnosticIds));

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

        var stm5008 = diagnostics.FirstOrDefault(
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

        var stm5018 = diagnostics.FirstOrDefault(
            static d => string.Equals(d.Id, DotNetExtractionDiagnosticIds.UnsupportedNamingPolicy, StringComparison.Ordinal));

        _ = await Assert.That(stm5018).IsNotNull()
            .Because("STM5018 should be emitted for an invalid naming policy option.");
        _ = await Assert.That(stm5018!.Severity).IsEqualTo(DiagnosticSeverity.Warning);
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

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> _);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return runResult.Results.SelectMany(static result => result.Diagnostics).ToArray();
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

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _global;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _global;
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

