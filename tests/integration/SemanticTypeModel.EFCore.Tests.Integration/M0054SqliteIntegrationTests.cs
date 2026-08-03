using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.EFCore.Tests.Integration;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Milestone acceptance names are executable documentation.")]
public sealed class M0054SqliteIntegrationTests
{
    [Test]
    public async Task Sqlite_EnsureCreated_Succeeds_ForOrderIntakeSpecificationModel()
    {
        await using SqliteConnection connection = await OpenConnection();
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options);
        _ = await context.Database.EnsureCreatedAsync();
    }

    [Test]
    public async Task Sqlite_InsertAndLoad_Succeeds_ForMinimalOrderIntakeSpecification()
    {
        await using SqliteConnection connection = await OpenConnection();
        DbContextOptions<IntakeContext> options = new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options;
        Intake.OrderIntakeSpecification expected = FixtureModels.MinimalIntake();
        await using (var context = new IntakeContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            _ = context.Specifications.Add(expected);
            _ = await context.SaveChangesAsync();
        }
        await using var verification = new IntakeContext(options);
        Intake.OrderIntakeSpecification actual = await verification.Specifications.SingleAsync();
        _ = await Assert.That(actual.Id).IsEqualTo(expected.Id);
        _ = await Assert.That(actual.SchemaVersion).IsEqualTo(expected.SchemaVersion);
        _ = await Assert.That(actual.Delivery.PartnerCode).IsEqualTo(expected.Delivery.PartnerCode);
    }

    [Test]
    public async Task Sqlite_EnsureCreated_Succeeds_ForOrderFulfillmentRunStateModel()
    {
        await using SqliteConnection connection = await OpenConnection();
        await using var context = new RunStateContext(new DbContextOptionsBuilder<RunStateContext>().UseSqlite(connection).Options);
        _ = await context.Database.EnsureCreatedAsync();
    }

    [Test]
    public async Task Sqlite_InsertAndLoad_Succeeds_ForMinimalFulfillmentRunSnapshot()
    {
        await using SqliteConnection connection = await OpenConnection();
        DbContextOptions<RunStateContext> options = new DbContextOptionsBuilder<RunStateContext>().UseSqlite(connection).Options;
        RunState.OrderFulfillmentRunSnapshot expected = FixtureModels.MinimalRunState();
        await using (var context = new RunStateContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            _ = context.Snapshots.Add(expected);
            _ = await context.SaveChangesAsync();
        }
        await using var verification = new RunStateContext(options);
        RunState.OrderFulfillmentRunSnapshot actual = await verification.Snapshots.SingleAsync();
        _ = await Assert.That(actual.Id).IsEqualTo(expected.Id);
        IEntityType snapshot = verification.Model.FindEntityType(typeof(RunState.OrderFulfillmentRunSnapshot))!;
        _ = await Assert.That(snapshot.FindProperty(nameof(RunState.OrderFulfillmentRunSnapshot.RunId))).IsNull();
        _ = await Assert.That(snapshot.FindProperty(nameof(RunState.OrderFulfillmentRunSnapshot.Labels))).IsNull();
    }

    [Test]
    public async Task Sqlite_DoesNotCreateColumns_ForExtensionData()
    {
        await using SqliteConnection connection = await OpenConnection();
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options);
        _ = await context.Database.EnsureCreatedAsync();
        _ = await Assert.That(context.Model.GetEntityTypes().SelectMany(e => e.GetProperties()).Any(p => p.Name == nameof(Intake.VersionedExtensibleObject.ExtensionData))).IsFalse();
    }

    [Test]
    public async Task Sqlite_DoesNotCreateTables_ForValueObjectRoots()
    {
        await using SqliteConnection connection = await OpenConnection();
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseSqlite(connection).Options);
        _ = await context.Database.EnsureCreatedAsync();
        _ = await Assert.That(context.Model.GetEntityTypes().Any(e => e.ClrType == typeof(Intake.PartnerDeliveryAgreement) && !e.IsOwned())).IsFalse();
    }

    private static async Task<SqliteConnection> OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private sealed class IntakeContext(DbContextOptions<IntakeContext> options) : DbContext(options)
    {
        public DbSet<Intake.OrderIntakeSpecification> Specifications => Set<Intake.OrderIntakeSpecification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticTypeModel(FixtureModels.CreateIntake());
        }
    }

    private sealed class RunStateContext(DbContextOptions<RunStateContext> options) : DbContext(options)
    {
        public DbSet<RunState.OrderFulfillmentRunSnapshot> Snapshots => Set<RunState.OrderFulfillmentRunSnapshot>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticTypeModel(FixtureModels.CreateRunState());
        }
    }
}
#pragma warning restore CS1591
