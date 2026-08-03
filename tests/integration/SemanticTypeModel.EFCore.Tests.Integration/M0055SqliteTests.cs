#pragma warning disable CA1707, IDE0058
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.EFCore.Tests.Integration;

#pragma warning disable CS1591
public sealed class M0055SqliteTests
{
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
        }
    }

    private sealed class IntakeContext(DbContextOptions<IntakeContext> options) : DbContext(options)
    {
        public DbSet<Intake.ImportSpecification> Specifications => Set<Intake.ImportSpecification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
