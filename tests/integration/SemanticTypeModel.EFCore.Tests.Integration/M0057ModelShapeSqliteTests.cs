#pragma warning disable CA1707
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.EFCoreModelShapes;

namespace SemanticTypeModel.EFCore.Tests.Integration;

#pragma warning disable CS1591
public sealed class M0057ModelShapeSqliteTests
{
    [Test]
    public async Task M0057_Sqlite_flat_baseline_round_trips()
    {
        var id = Guid.NewGuid();
        await RoundTrip(ModelShapeModels.Flat(), new FlatOrder(id, "flat"), [typeof(FlatOrder)], async context =>
            _ = await Assert.That((await context.Set<FlatOrder>().SingleAsync()).Number).IsEqualTo("flat"));
    }

    [Test]
    public async Task M0057_Sqlite_NonSemanticBase_scalar_round_trips_on_first_semantic_entity()
    {
        await RoundTrip(ModelShapeModels.NonSemanticBaseScalar(), new VersionedOrder(Guid.NewGuid(), "versioned") { SchemaVersion = 7 }, [typeof(VersionedOrder)], async context =>
            _ = await Assert.That((await context.Set<VersionedOrder>().SingleAsync()).SchemaVersion).IsEqualTo(7));
    }

    [Test]
    public async Task M0057_Sqlite_NonSemanticBase_ExtensionData_round_trips_as_JSON()
    {
        var value = new ExtensibleOrder(Guid.NewGuid()) { ExtensionData = new() { ["kind"] = JsonDocument.Parse("\"fixture\"").RootElement } };
        await RoundTrip(ModelShapeModels.ExtensionData(), value, [typeof(ExtensibleOrder)], async context =>
            _ = await Assert.That((await context.Set<ExtensibleOrder>().SingleAsync()).ExtensionData!["kind"].GetString()).IsEqualTo("fixture"));
    }

    [Test]
    public async Task M0057_Sqlite_Tpt_semantic_inheritance_round_trips_base_and_derived_state()
    {
        var value = new ImportSpecification(Guid.NewGuid(), "display", "import") { SchemaVersion = 3 };
        Type[] inventory = [typeof(Specification), typeof(ImportSpecification), typeof(WorkflowSpecification)];
        await RoundTrip(ModelShapeModels.Tpt(), value, inventory, async context =>
        {
            ImportSpecification loaded = await context.Set<ImportSpecification>().SingleAsync();
            _ = await Assert.That(loaded.DisplayName).IsEqualTo("display");
            _ = await Assert.That(loaded.ImportName).IsEqualTo("import");
        });
    }

    [Test]
    public async Task M0057_Sqlite_Tpt_with_NonSemanticGrandbase_round_trips_grandbase_state()
    {
        var value = new WorkflowSpecification(Guid.NewGuid(), "display", "workflow") { SchemaVersion = 11 };
        Type[] inventory = [typeof(Specification), typeof(ImportSpecification), typeof(WorkflowSpecification)];
        await RoundTrip(ModelShapeModels.Tpt(), value, inventory, async context =>
            _ = await Assert.That((await context.Set<WorkflowSpecification>().SingleAsync()).SchemaVersion).IsEqualTo(11));
    }

    [Test]
    public async Task M0057_Sqlite_owned_JSON_object_round_trips()
    {
        var value = new SourceOrder(Guid.NewGuid()) { Source = new(new Uri("https://example.test/source"), new(2)) { Version = 4 } };
        await RoundTrip(ModelShapeModels.OwnedObject(), value, [typeof(SourceOrder)], async context =>
            _ = await Assert.That((await context.Set<SourceOrder>().SingleAsync()).Source!.Retry!.Attempts).IsEqualTo(2));
    }

    [Test]
    public async Task M0057_Sqlite_owned_JSON_array_round_trips()
    {
        var value = new FieldConfiguredOrder(Guid.NewGuid()) { DerivedFields = [new("total")] };
        await RoundTrip(ModelShapeModels.OwnedCollection(), value, [typeof(FieldConfiguredOrder)], async context =>
            _ = await Assert.That((await context.Set<FieldConfiguredOrder>().SingleAsync()).DerivedFields.Single().Name).IsEqualTo("total"));
    }

    [Test]
    public async Task M0057_Sqlite_nested_ValueKind_JSON_round_trips_without_ValueKind_entities()
    {
        var value = new SourceConsumer(Guid.NewGuid(), new(new Uri("https://example.test/nested"), new(5)) { Version = 9 });
        Type[] inventory = [typeof(SourceConsumer), typeof(AlternateSourceConsumer)];
        await RoundTrip(ModelShapeModels.ReusedNestedValueKind(), value, inventory, async context =>
            _ = await Assert.That((await context.Set<SourceConsumer>().SingleAsync()).Source.Retry!.Attempts).IsEqualTo(5));
    }

    private static async Task RoundTrip<T>(TypeSchemaModel semantic, T value, Type[] expectedEntities, Func<DbContext, Task> assert) where T : class
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DbContextOptions<ShapeContext<T>> options = new DbContextOptionsBuilder<ShapeContext<T>>().UseSqlite(connection).Options;
        await using (var context = new ShapeContext<T>(options, semantic))
        {
            _ = await context.Database.EnsureCreatedAsync();
            _ = await Assert.That(context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expectedEntities);
            _ = context.Set<T>().Add(value);
            _ = await context.SaveChangesAsync();
        }
        await using (var context = new ShapeContext<T>(options, semantic))
        {
            _ = await Assert.That(context.Model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expectedEntities);
            await assert(context);
        }
    }

    private sealed class ShapeContext<T>(DbContextOptions<ShapeContext<T>> options, TypeSchemaModel semantic) : DbContext(options) where T : class
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticRelationalModel(semantic.DeriveEfRelationalModel().Model);
        }
    }
}
#pragma warning restore CS1591
