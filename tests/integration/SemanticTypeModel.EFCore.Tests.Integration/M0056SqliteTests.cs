#pragma warning disable CA1707, IDE0058
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.EFCore.Tests.Integration;

#pragma warning disable CS1591
public sealed class M0056SqliteTests
{
    [Test]
    public async Task Sqlite_EnsureCreated_Succeeds_When_ValueKinds_Were_ConventionDiscoverable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options);
        _ = await context.Database.EnsureCreatedAsync();
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.CsvSourceSpecification))).IsNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.DerivedProperty))).IsNull();
    }

    [Test]
    public async Task Sqlite_RoundTrip_Succeeds_With_Owned_Json_Object()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<IntakeContext> options = new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options;
        await using (var context = new IntakeContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Specifications.Add(FixtureModels.MinimalIntake() with { ExtensionData = [], CsvSource = new(new Uri("https://example.test/input.csv"), ','), DerivedProperties = [new("Total", "Price * Quantity")] });
            _ = await context.SaveChangesAsync();
        }
        await using (var context = new IntakeContext(options))
        {
            Intake.ImportSpecification loaded = await context.Specifications.SingleAsync();
            _ = await Assert.That(loaded.CsvSource!.Delimiter).IsEqualTo(',');
            _ = await Assert.That(loaded.DerivedProperties.Single().Name).IsEqualTo("Total");
        }
    }

    [Test]
    public async Task Sqlite_RoundTrip_Succeeds_With_Owned_Json_Array()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<IntakeContext> options = new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options;
        await using (var context = new IntakeContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Specifications.Add(FixtureModels.MinimalIntake() with { ExtensionData = [], DerivedProperties = [new("Total", "Price * Quantity")] });
            _ = await context.SaveChangesAsync();
        }
        await using (var context = new IntakeContext(options))
        {
            _ = await Assert.That((await context.Specifications.SingleAsync()).DerivedProperties.Single().Name).IsEqualTo("Total");
        }
    }

    [Test]
    public async Task Sqlite_Final_Model_Contains_Only_Semantic_Entities()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options);
        Type[] expected = [typeof(Intake.Specification), typeof(Intake.ImportSpecification), typeof(Intake.WorkflowSpecification)];
        _ = await Assert.That(context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expected);
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.CsvSourceSpecification))).IsNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.XmlSourceSpecification))).IsNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.DerivedProperty))).IsNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.CsvSource))!.GetValueConverter()).IsNotNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.DerivedProperties))!.GetValueConverter()).IsNotNull();
    }

    [Test]
    public async Task Specification_fixture_creates_inserts_and_loads_TPT_and_JSON_shapes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<IntakeContext> options = new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options;
        await using (var context = new IntakeContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Specifications.Add(FixtureModels.MinimalIntake() with { ExtensionData = new() { ["source"] = System.Text.Json.JsonDocument.Parse("\"fixture\"").RootElement } });
            _ = await context.SaveChangesAsync();
        }
        await using (var context = new IntakeContext(options))
        {
            Intake.ImportSpecification loaded = await context.Specifications.SingleAsync();
            _ = await Assert.That(loaded.DeliveryContract.PartnerCode).IsEqualTo("partner");
            _ = await Assert.That(loaded.ExtensionData!["source"].GetString()).IsEqualTo("fixture");
        }
    }

    [Test]
    public async Task Run_state_fixture_round_trips_strong_ids_binary_and_JSON_collections()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<RunStateContext> options = new DbContextOptionsBuilder<RunStateContext>().UseSqlite(connection).Options;
        RunState.OrderFulfillmentRunSnapshot expected = FixtureModels.MinimalRunState() with { Executions = [new(new(Guid.NewGuid()), DateTimeOffset.UtcNow, null)] };
        await using (var context = new RunStateContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Snapshots.Add(expected);
            _ = await context.SaveChangesAsync();
        }
        await using (var context = new RunStateContext(options))
        {
            RunState.OrderFulfillmentRunSnapshot loaded = await context.Snapshots.SingleAsync();
            _ = await Assert.That(loaded.RunId).IsEqualTo(expected.RunId);
            _ = await Assert.That(loaded.RawPayload.Span.SequenceEqual(new byte[] { 3, 4 })).IsTrue();
            _ = await Assert.That(loaded.Executions.Count).IsEqualTo(1);
            _ = await Assert.That(context.Model.FindEntityType(typeof(RunState.SaveFulfillmentRunRequest))).IsNull();
            Type[] expectedEntities = [typeof(RunState.OrderFulfillmentRunSnapshot)];
            _ = await Assert.That(context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expectedEntities);
        }
    }

    private sealed class IntakeContext(DbContextOptions<IntakeContext> options) : DbContext(options)
    {
        public DbSet<Intake.ImportSpecification> Specifications => Set<Intake.ImportSpecification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<Intake.CsvSourceSpecification>().HasNoKey();
            _ = modelBuilder.Entity<Intake.DerivedProperty>().HasNoKey();
            _ = modelBuilder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        }
    }

    private sealed class RunStateContext(DbContextOptions<RunStateContext> options) : DbContext(options)
    {
        public DbSet<RunState.OrderFulfillmentRunSnapshot> Snapshots => Set<RunState.OrderFulfillmentRunSnapshot>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticRelationalModel(FixtureModels.CreateRunState().DeriveEfRelationalModel().Model);
        }
    }
}
#pragma warning restore CS1591
