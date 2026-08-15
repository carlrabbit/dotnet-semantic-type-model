using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.Generated.EFCore;
using SemanticTypeModel.M0060.ModelA;
using SemanticTypeModel.M0060.ModelB;

[assembly: GenerateSemanticEfModel(typeof(InventoryItem))]
[assembly: GenerateSemanticEfModel(typeof(BillingRecord))]

namespace SemanticTypeModel.EFCore.Tests.Integration;

public sealed class M0060GeneratedConfigurationTests
{
    [Test]
    public async Task MultiModel_generated_configurations_preserve_manual_entity_and_round_trip_with_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<M0060Context> options = new DbContextOptionsBuilder<M0060Context>().UseSqlite(connection).Options;
        await using (var context = new M0060Context(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Inventory.Add(new InventoryItem
            {
                Id = new InventoryItemId(Guid.NewGuid()),
                DisplayName = "Widget",
                State = InventoryState.Active,
                Payload = [1, 2],
                ReadOnlyPayload = new byte[] { 3, 4 },
                Endpoint = new Uri("inventory/7", UriKind.Relative),
                OptionalEndpoint = null,
                Details = new InventoryDetails { Warehouse = "A", Quantity = 7 },
            });
            context.Billing.Add(new BillingRecord { Id = Guid.NewGuid(), Amount = 12.5m });
            context.SpecializedBilling.Add(new SpecializedBillingRecord { Id = Guid.NewGuid(), Amount = 19.5m, Reference = "derived" });
            context.Manual.Add(new ManualAudit { Id = 1, Message = "kept" });
            context.ModelAExternal.Add(new ModelAExternalEntity { Id = 1, Note = "model-a external" });
            context.ModelBExternal.Add(new ModelBExternalEntity { Id = 1, Note = "model-b external" });
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new M0060Context(options))
        {
            InventoryItem item = await context.Inventory.SingleAsync();
            _ = await Assert.That(item.Details.Quantity).IsEqualTo(7);
            _ = await Assert.That(item.Endpoint.ToString()).IsEqualTo("inventory/7");
            _ = await Assert.That(item.OptionalEndpoint).IsNull();
            _ = await Assert.That(await context.Billing.CountAsync()).IsEqualTo(2);
            SpecializedBillingRecord specialized = await context.SpecializedBilling.SingleAsync();
            _ = await Assert.That(specialized.Reference).IsEqualTo("derived");
            _ = await Assert.That(await context.Manual.CountAsync()).IsEqualTo(1);
            _ = await Assert.That(await context.ModelAExternal.CountAsync()).IsEqualTo(1);
            _ = await Assert.That(await context.ModelBExternal.CountAsync()).IsEqualTo(1);
            await AssertExactEntityTypes(context,
                typeof(InventoryItem), typeof(BillingRecord), typeof(SpecializedBillingRecord),
                typeof(ManualAudit), typeof(ModelAExternalEntity), typeof(ModelBExternalEntity));
            _ = await Assert.That(context.Model.FindEntityType(typeof(SpecializedBillingRecord))!.BaseType!.ClrType).IsEqualTo(typeof(BillingRecord));
            _ = await Assert.That(context.Model.FindEntityType(typeof(InventoryDetails))).IsNull();
            _ = await Assert.That(context.Model.FindEntityType(typeof(InventoryItemId))).IsNull();
            _ = await Assert.That(context.Model.FindEntityType(typeof(InventoryState))).IsNull();
            _ = await Assert.That(context.Model.FindEntityType(typeof(InventoryOptions))).IsNull();
            _ = await Assert.That(context.Model.FindEntityType(typeof(ModelAIgnoredPoco))).IsNull();
            _ = await Assert.That(context.Model.FindEntityType(typeof(ModelBIgnoredPoco))).IsNull();
        }
    }

    [Test]
    public async Task Each_generated_model_preserves_entities_owned_by_the_surrounding_context()
    {
        DbContextOptions<InventoryOnlyContext> inventoryOptions = new DbContextOptionsBuilder<InventoryOnlyContext>().UseSqlite("Data Source=:memory:").Options;
        await using (var inventory = new InventoryOnlyContext(inventoryOptions))
        {
            await AssertExactEntityTypes(inventory, typeof(InventoryItem), typeof(ModelBExternalEntity));
        }

        DbContextOptions<BillingOnlyContext> billingOptions = new DbContextOptionsBuilder<BillingOnlyContext>().UseSqlite("Data Source=:memory:").Options;
        await using var billing = new BillingOnlyContext(billingOptions);
        await AssertExactEntityTypes(billing, typeof(BillingRecord), typeof(SpecializedBillingRecord), typeof(ModelAExternalEntity));
    }

    [Test]
    public async Task ConfigureAfterGenerated_changes_finalized_provider_model()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        DbContextOptions<M0060Context> options = new DbContextOptionsBuilder<M0060Context>().UseSqlite(connection).Options;
        await using var context = new M0060Context(options);
        IEnumerable<IIndex> indexes = context.Model.FindEntityType(typeof(InventoryItem))!.GetIndexes();
        _ = await Assert.That(indexes.Any(index => index.Properties.Single().Name == nameof(InventoryItem.DisplayName) && index.IsUnique)).IsTrue();
        _ = await Assert.That((bool?)context.Model.FindEntityType(typeof(InventoryItem))!.FindAnnotation("M0060.BeforeGenerated")?.Value).IsTrue();
    }

    private static async Task AssertExactEntityTypes(DbContext context, params Type[] expected)
    {
        Type[] actual = [.. context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.FullName, StringComparer.Ordinal)];
        Type[] orderedExpected = [.. expected.OrderBy(type => type.FullName, StringComparer.Ordinal)];
        _ = await Assert.That(actual).IsEquivalentTo(orderedExpected);
    }
}

internal sealed class M0060Context(DbContextOptions<M0060Context> options) : DbContext(options)
{
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<BillingRecord> Billing => Set<BillingRecord>();
    public DbSet<SpecializedBillingRecord> SpecializedBilling => Set<SpecializedBillingRecord>();
    public DbSet<ManualAudit> Manual => Set<ManualAudit>();
    public DbSet<ModelAExternalEntity> ModelAExternal => Set<ModelAExternalEntity>();
    public DbSet<ModelBExternalEntity> ModelBExternal => Set<ModelBExternalEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyInventorySemanticModel();
        _ = modelBuilder.ApplyBillingSemanticModel();
        modelBuilder.ApplyConfiguration(new ManualAuditConfiguration());
        modelBuilder.ApplyConfiguration(new ModelAExternalEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ModelBExternalEntityConfiguration());
    }
}

internal sealed class InventoryOnlyContext(DbContextOptions<InventoryOnlyContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyInventorySemanticModel();
        modelBuilder.ApplyConfiguration(new ModelBExternalEntityConfiguration());
    }
}

internal sealed class BillingOnlyContext(DbContextOptions<BillingOnlyContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyBillingSemanticModel();
        modelBuilder.ApplyConfiguration(new ModelAExternalEntityConfiguration());
    }
}

internal sealed class ManualAudit { public int Id { get; set; } public string Message { get; set; } = string.Empty; }
internal sealed class ManualAuditConfiguration : IEntityTypeConfiguration<ManualAudit>
{
    public void Configure(EntityTypeBuilder<ManualAudit> builder) { _ = builder.HasKey(entity => entity.Id); _ = builder.ToTable("ManualAudit"); }
}

internal sealed class ModelAExternalEntityConfiguration : IEntityTypeConfiguration<ModelAExternalEntity>
{
    public void Configure(EntityTypeBuilder<ModelAExternalEntity> builder) { _ = builder.HasKey(entity => entity.Id); _ = builder.ToTable("ModelAExternal"); }
}

internal sealed class ModelBExternalEntityConfiguration : IEntityTypeConfiguration<ModelBExternalEntity>
{
    public void Configure(EntityTypeBuilder<ModelBExternalEntity> builder) { _ = builder.HasKey(entity => entity.Id); _ = builder.ToTable("ModelBExternal"); }
}
