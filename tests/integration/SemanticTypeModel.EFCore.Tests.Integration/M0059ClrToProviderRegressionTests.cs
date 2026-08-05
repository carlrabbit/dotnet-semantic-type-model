#pragma warning disable CA1707, CS1591
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.Generators;
using Regression = SemanticTypeModel.RealWorldFixtures.M0059ClrEnumRegression;
namespace SemanticTypeModel.EFCore.Tests.Integration;

public sealed class M0059ClrToProviderRegressionTests
{
    [Test]
    public async Task M0059_generated_CLR_provider_model_finalizes_creates_saves_and_reloads_enum_guarded_value_kinds()
    {
        TypeSchemaModel semantic = ExtractCompiledClrSemanticModel();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<M0059RegressionContext> options = new DbContextOptionsBuilder<M0059RegressionContext>().UseSqlite(connection).Options;

        await using (var context = new M0059RegressionContext(options, semantic))
        {
            IModel model = context.Model;
            await AssertFinalMetadata(model);
            _ = await context.Database.EnsureCreatedAsync();
            _ = context.ImportJobs.Add(new Regression.ImportJob
            {
                Id = Guid.NewGuid(),
                SourceKind = Regression.ImportSourceKind.CsvFile,
                OptionalSourceKind = Regression.ImportSourceKind.XmlFile,
                CsvSource = new("https://example.test/orders.csv", ","),
            });
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new M0059RegressionContext(options, semantic))
        {
            Regression.ImportJob loaded = await context.ImportJobs.SingleAsync();
            _ = await Assert.That(loaded.SourceKind).IsEqualTo(Regression.ImportSourceKind.CsvFile);
            _ = await Assert.That(loaded.OptionalSourceKind).IsEqualTo(Regression.ImportSourceKind.XmlFile);
            _ = await Assert.That(loaded.CsvSource!.Location).IsEqualTo("https://example.test/orders.csv");
        }
    }

    private static TypeSchemaModel ExtractCompiledClrSemanticModel()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../../fixtures/SemanticTypeModel.RealWorldFixtures/M0059ClrEnumRegression/Model.cs"));
        MetadataReference[] references = [.. AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)).Select(a => MetadataReference.CreateFromFile(a.Location))];
        var compilation = CSharpCompilation.Create(
            $"M0059ClrFixture_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SemanticTypeModelSourceGenerator());
        _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation generatedCompilation, out ImmutableArray<Diagnostic> generatorDiagnostics);
        Diagnostic[] failedGeneratorDiagnostics = [.. generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
        if (failedGeneratorDiagnostics.Length != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, failedGeneratorDiagnostics));
        }
        SyntaxTree providerTree = generatedCompilation.SyntaxTrees.Single(tree => tree.FilePath.EndsWith("SemanticTypeModel.Generated.g.cs", StringComparison.Ordinal));
        var providerCompilation = CSharpCompilation.Create(
            $"M0059GeneratedProvider_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText("using System.Linq;" + Environment.NewLine + providerTree.GetText())],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        EmitResult emit = providerCompilation.Emit(stream);
        if (!emit.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics));
        }
        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        return (TypeSchemaModel)assembly.GetType("SemanticTypeModel.Generated.AppSemanticTypeModel")!.GetMethod("Create")!.Invoke(null, null)!;
    }

    private static async Task AssertFinalMetadata(IModel model)
    {
        Type[] approvedEntities = [typeof(Regression.ImportJob)];
        _ = await Assert.That(model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(approvedEntities);
        foreach (IEntityType entity in model.GetEntityTypes())
        {
            _ = await Assert.That(entity.IsOwned()).IsFalse();
            _ = await Assert.That(entity.FindPrimaryKey()).IsNotNull();
            _ = await Assert.That(entity.GetNavigations()).IsEmpty();
            _ = await Assert.That(entity.GetSkipNavigations()).IsEmpty();
            _ = await Assert.That(entity.GetForeignKeys().Where(fk => !fk.IsBaseLinking()).SelectMany(fk => fk.Properties)).IsEmpty();
            _ = await Assert.That(entity.GetProperties().Where(property => property.IsShadowProperty())).IsEmpty();
        }
        _ = await Assert.That(model.GetEntityTypes().Any(entity => entity.ClrType == typeof(Dictionary<string, object>))).IsFalse();
        _ = await Assert.That(model.GetEntityTypes().Any(entity => entity.GetForeignKeys().Count() > 1 && !approvedEntities.Contains(entity.ClrType))).IsFalse();
        _ = await Assert.That(model.GetEntityTypes().Any(entity => entity.ClrType == typeof(Regression.ImportSourceKind))).IsFalse();
        _ = await Assert.That(model.GetEntityTypes().Any(entity => entity.ClrType == typeof(Regression.CsvSource))).IsFalse();
        IEntityType import = model.FindEntityType(typeof(Regression.ImportJob))!;
        _ = await Assert.That(import.FindProperty(nameof(Regression.ImportJob.SourceKind))!.GetProviderClrType()).IsEqualTo(typeof(string));
        _ = await Assert.That(import.FindProperty(nameof(Regression.ImportJob.OptionalSourceKind))!.GetProviderClrType()).IsEqualTo(typeof(string));
        _ = await Assert.That(import.FindProperty(nameof(Regression.ImportJob.CsvSource))!.GetValueConverter()).IsNotNull();
    }

    private sealed class M0059RegressionContext(DbContextOptions<M0059RegressionContext> options, TypeSchemaModel semantic) : DbContext(options)
    {
        public DbSet<Regression.ImportJob> ImportJobs => Set<Regression.ImportJob>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<Regression.CsvSource>().HasNoKey();
            SemanticDerivationResult<EfRelationalModel> result = modelBuilder.ApplySemanticTypeModel(semantic);
            if (result.Diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error))
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == SchemaDiagnosticSeverity.Error).Select(d => $"{d.Code}: {d.Message}")));
            }
        }
    }
}

file static class M0059EfMetadataExtensions
{
    public static bool IsBaseLinking(this IForeignKey foreignKey)
    {
        return foreignKey.DeclaringEntityType.BaseType == foreignKey.PrincipalEntityType;
    }
}
