using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.EFCore.CompatibilityModel;
using SemanticTypeModel.EFCore.CompositionModel;
using SemanticTypeModel.Generated.EFCore;

[assembly: GenerateSemanticEfModel(typeof(InventoryItem))]
[assembly: GenerateSemanticEfModel(typeof(BillingRecord))]

namespace SemanticTypeModel.EFCore.Tests.Integration;

public sealed class GeneratedConfigurationTests
{
    [Test]
    public async Task MultiModel_generated_configurations_preserve_manual_entity_and_round_trip_with_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<GeneratedCompatibilityContext> options = new DbContextOptionsBuilder<GeneratedCompatibilityContext>().UseSqlite(connection).Options;
        await using (var context = new GeneratedCompatibilityContext(options))
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
                DetailHistory = [new InventoryDetails { Warehouse = "A", Quantity = 6 }],
            });
            context.Billing.Add(new BillingRecord { Id = Guid.NewGuid(), Amount = 12.5m });
            context.SpecializedBilling.Add(new SpecializedBillingRecord { Id = Guid.NewGuid(), Amount = 19.5m, Reference = "derived" });
            context.Manual.Add(new ManualAudit { Id = 1, Message = "kept" });
            context.ModelAExternal.Add(new ModelAExternalEntity { Id = 1, Note = "model-a external" });
            context.ModelBExternal.Add(new ModelBExternalEntity { Id = 1, Note = "model-b external" });
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new GeneratedCompatibilityContext(options))
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
                typeof(InventoryItem), typeof(InventoryDocument), typeof(SpecializedInventoryDocument), typeof(BillingRecord), typeof(SpecializedBillingRecord),
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
    public async Task Generated_nullable_owned_JSON_has_finalized_optional_converter_and_comparer_metadata()
    {
        DbContextOptions<GeneratedCompatibilityContext> options = new DbContextOptionsBuilder<GeneratedCompatibilityContext>().UseSqlite("Data Source=:memory:").Options;
        await using var context = new GeneratedCompatibilityContext(options);
        IEntityType entity = context.Model.FindEntityType(typeof(InventoryItem))!;

        foreach (var propertyName in new[] { nameof(InventoryItem.OptionalDetails), nameof(InventoryItem.OptionalDetailHistory) })
        {
            IProperty property = entity.FindProperty(propertyName)!;
            _ = await Assert.That(property.IsNullable).IsTrue();
            _ = await Assert.That(property.GetValueConverter()).IsNotNull();
            _ = await Assert.That(property.GetValueComparer()).IsNotNull();
        }

        foreach (var propertyName in new[] { nameof(InventoryItem.Details), nameof(InventoryItem.DetailHistory) })
        {
            IProperty property = entity.FindProperty(propertyName)!;
            _ = await Assert.That(property.IsNullable).IsFalse();
            _ = await Assert.That(property.GetValueConverter()).IsNotNull();
            _ = await Assert.That(property.GetValueComparer()).IsNotNull();
        }
    }

    [Test]
    public async Task Finalized_model_covers_supported_storage_nullability_matrix_and_inherited_reuse()
    {
        DbContextOptions<GeneratedCompatibilityContext> options = new DbContextOptionsBuilder<GeneratedCompatibilityContext>().UseSqlite("Data Source=:memory:").Options;
        await using var context = new GeneratedCompatibilityContext(options);
        IEntityType inventory = context.Model.FindEntityType(typeof(InventoryItem))!;

        await AssertPropertyPair(inventory, nameof(InventoryItem.DisplayName), nameof(InventoryItem.OptionalDisplayName), converterExpected: false);
        await AssertPropertyPair(inventory, nameof(InventoryItem.State), nameof(InventoryItem.OptionalState), converterExpected: true);
        await AssertPropertyPair(inventory, nameof(InventoryItem.Id), nameof(InventoryItem.OptionalExternalId), converterExpected: true);
        await AssertPropertyPair(inventory, nameof(InventoryItem.Endpoint), nameof(InventoryItem.OptionalEndpoint), converterExpected: true);
        await AssertPropertyPair(inventory, nameof(InventoryItem.Payload), nameof(InventoryItem.OptionalPayload), converterExpected: false);
        await AssertPropertyPair(inventory, nameof(InventoryItem.ReadOnlyPayload), nameof(InventoryItem.OptionalReadOnlyPayload), converterExpected: true);
        await AssertPropertyPair(inventory, nameof(InventoryItem.Details), nameof(InventoryItem.OptionalDetails), converterExpected: true, comparerExpected: true);
        await AssertPropertyPair(inventory, nameof(InventoryItem.DetailHistory), nameof(InventoryItem.OptionalDetailHistory), converterExpected: true, comparerExpected: true);
        IProperty extensionData = inventory.FindProperty(nameof(InventoryItem.ExtensionData))!;
        _ = await Assert.That(extensionData.IsNullable).IsTrue();
        _ = await Assert.That(extensionData.GetValueConverter()).IsNotNull();
        _ = await Assert.That(extensionData.GetValueComparer()).IsNotNull();

        IEntityType document = context.Model.FindEntityType(typeof(InventoryDocument))!;
        IEntityType specialized = context.Model.FindEntityType(typeof(SpecializedInventoryDocument))!;
        _ = await Assert.That(specialized.BaseType).IsEqualTo(document);
        _ = await Assert.That(document.FindProperty(nameof(InventoryDocument.OptionalDetails))!.IsNullable).IsTrue();
        _ = await Assert.That(specialized.FindProperty(nameof(SpecializedInventoryDocument.RequiredDetails))!.IsNullable).IsFalse();
        _ = await Assert.That(document.GetTableName()).IsNotEqualTo(specialized.GetTableName());
    }

    [Test]
    public async Task SQLite_round_trips_nullable_storage_matrix_extension_data_and_TPT_reuse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<GeneratedCompatibilityContext> options = new DbContextOptionsBuilder<GeneratedCompatibilityContext>().UseSqlite(connection).Options;
        var id = Guid.NewGuid();
        var optionalStrongId = new InventoryItemId(Guid.NewGuid());
        JsonElement extensionValue = JsonDocument.Parse("{\"source\":\"matrix\"}").RootElement.Clone();

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Inventory.Add(new InventoryItem
            {
                Id = new(id),
                DisplayName = "required",
                OptionalDisplayName = "optional",
                State = InventoryState.Active,
                OptionalState = InventoryState.Archived,
                OptionalExternalId = optionalStrongId,
                Endpoint = new("required", UriKind.Relative),
                OptionalEndpoint = new("optional", UriKind.Relative),
                Payload = [1],
                OptionalPayload = [2],
                ReadOnlyPayload = new byte[] { 3 },
                OptionalReadOnlyPayload = new byte[] { 4 },
                Details = new() { Warehouse = "required", Quantity = 1 },
                OptionalDetails = new() { Warehouse = "optional", Quantity = 2 },
                DetailHistory = [new() { Warehouse = "required", Quantity = 1 }],
                OptionalDetailHistory = [new() { Warehouse = "optional", Quantity = 2 }],
                ExtensionData = new() { ["metadata"] = extensionValue },
            });
            context.Documents.Add(new SpecializedInventoryDocument
            {
                Id = Guid.NewGuid(),
                OptionalDetails = null,
                RequiredDetails = new() { Warehouse = "derived", Quantity = 3 },
            });
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            InventoryItem item = await context.Inventory.SingleAsync(value => value.Id == new InventoryItemId(id));
            _ = await Assert.That(item.OptionalDisplayName).IsEqualTo("optional");
            _ = await Assert.That(item.OptionalState).IsEqualTo(InventoryState.Archived);
            _ = await Assert.That(item.OptionalExternalId).IsEqualTo(optionalStrongId);
            _ = await Assert.That(item.OptionalEndpoint?.ToString()).IsEqualTo("optional");
            _ = await Assert.That(item.OptionalPayload).IsEquivalentTo(new byte[] { 2 });
            _ = await Assert.That(item.OptionalReadOnlyPayload?.ToArray()).IsEquivalentTo(new byte[] { 4 });
            _ = await Assert.That(item.ExtensionData?["metadata"].GetProperty("source").GetString()).IsEqualTo("matrix");
            SpecializedInventoryDocument document = await context.Documents.OfType<SpecializedInventoryDocument>().SingleAsync();
            _ = await Assert.That(document.OptionalDetails).IsNull();
            _ = await Assert.That(document.RequiredDetails.Warehouse).IsEqualTo("derived");
        }
    }

    [Test]
    public async Task SQLite_nullable_owned_JSON_tracks_null_value_changed_value_null_transitions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<GeneratedCompatibilityContext> options = new DbContextOptionsBuilder<GeneratedCompatibilityContext>().UseSqlite(connection).Options;
        var id = Guid.NewGuid();

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            _ = await context.Database.EnsureCreatedAsync();
            context.Inventory.Add(new InventoryItem { Id = new(id), DisplayName = "transitions" });
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            InventoryItem item = await context.Inventory.SingleAsync(value => value.Id == new InventoryItemId(id));
            _ = await Assert.That(item.OptionalDetails).IsNull();
            _ = await Assert.That(item.OptionalDetailHistory).IsNull();
            item.OptionalDetails = new InventoryDetails { Warehouse = "A", Quantity = 1 };
            item.OptionalDetailHistory = [new InventoryDetails { Warehouse = "A", Quantity = 1 }];
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            InventoryItem item = await context.Inventory.SingleAsync(value => value.Id == new InventoryItemId(id));
            _ = await Assert.That(item.OptionalDetails?.Quantity).IsEqualTo(1);
            item.OptionalDetails!.Quantity = 2;
            item.OptionalDetailHistory = [new InventoryDetails { Warehouse = "B", Quantity = 2 }];
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            InventoryItem item = await context.Inventory.SingleAsync(value => value.Id == new InventoryItemId(id));
            _ = await Assert.That(item.OptionalDetails?.Quantity).IsEqualTo(2);
            _ = await Assert.That(item.OptionalDetailHistory?.Single().Warehouse).IsEqualTo("B");
            item.OptionalDetails = null;
            item.OptionalDetailHistory = null;
            _ = await context.SaveChangesAsync();
        }

        await using (var context = new GeneratedCompatibilityContext(options))
        {
            InventoryItem item = await context.Inventory.SingleAsync(value => value.Id == new InventoryItemId(id));
            _ = await Assert.That(item.OptionalDetails).IsNull();
            _ = await Assert.That(item.OptionalDetailHistory).IsNull();
        }
    }

    [Test]
    public async Task Each_generated_model_preserves_entities_owned_by_the_surrounding_context()
    {
        DbContextOptions<InventoryOnlyContext> inventoryOptions = new DbContextOptionsBuilder<InventoryOnlyContext>().UseSqlite("Data Source=:memory:").Options;
        await using (var inventory = new InventoryOnlyContext(inventoryOptions))
        {
            await AssertExactEntityTypes(inventory, typeof(InventoryItem), typeof(InventoryDocument), typeof(SpecializedInventoryDocument), typeof(ModelBExternalEntity));
        }

        DbContextOptions<BillingOnlyContext> billingOptions = new DbContextOptionsBuilder<BillingOnlyContext>().UseSqlite("Data Source=:memory:").Options;
        await using var billing = new BillingOnlyContext(billingOptions);
        await AssertExactEntityTypes(billing, typeof(BillingRecord), typeof(SpecializedBillingRecord), typeof(ModelAExternalEntity));
    }

    [Test]
    public async Task ConfigureAfterGenerated_changes_finalized_provider_model()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        DbContextOptions<GeneratedCompatibilityContext> options = new DbContextOptionsBuilder<GeneratedCompatibilityContext>().UseSqlite(connection).Options;
        await using var context = new GeneratedCompatibilityContext(options);
        IEnumerable<IIndex> indexes = context.Model.FindEntityType(typeof(InventoryItem))!.GetIndexes();
        _ = await Assert.That(indexes.Any(index => index.Properties.Single().Name == nameof(InventoryItem.DisplayName) && index.IsUnique)).IsTrue();
        _ = await Assert.That((bool?)context.Model.FindEntityType(typeof(InventoryItem))!.FindAnnotation("Compatibility.BeforeGenerated")?.Value).IsTrue();
    }

    private static async Task AssertExactEntityTypes(DbContext context, params Type[] expected)
    {
        Type[] actual = [.. context.Model.GetEntityTypes().Select(entity => entity.ClrType).OrderBy(type => type.FullName, StringComparer.Ordinal)];
        Type[] orderedExpected = [.. expected.OrderBy(type => type.FullName, StringComparer.Ordinal)];
        _ = await Assert.That(actual).IsEquivalentTo(orderedExpected);
    }

    private static async Task AssertPropertyPair(
        IEntityType entity,
        string requiredName,
        string optionalName,
        bool converterExpected,
        bool comparerExpected = false)
    {
        IProperty required = entity.FindProperty(requiredName)!;
        IProperty optional = entity.FindProperty(optionalName)!;
        _ = await Assert.That(required.IsNullable).IsFalse();
        _ = await Assert.That(optional.IsNullable).IsTrue();
        _ = await Assert.That(required.GetTypeMapping().Converter is not null).IsEqualTo(converterExpected);
        _ = await Assert.That(optional.GetTypeMapping().Converter is not null).IsEqualTo(converterExpected);
        if (comparerExpected)
        {
            _ = await Assert.That(required.GetValueComparer()).IsNotNull();
            _ = await Assert.That(optional.GetValueComparer()).IsNotNull();
        }
    }
}

internal sealed class GeneratedCompatibilityContext(DbContextOptions<GeneratedCompatibilityContext> options) : DbContext(options)
{
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<BillingRecord> Billing => Set<BillingRecord>();
    public DbSet<SpecializedBillingRecord> SpecializedBilling => Set<SpecializedBillingRecord>();
    public DbSet<InventoryDocument> Documents => Set<InventoryDocument>();
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
