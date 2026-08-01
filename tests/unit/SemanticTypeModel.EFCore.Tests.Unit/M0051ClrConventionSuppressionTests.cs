using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0051ClrConventionSuppressionTests
{
    [Test]
    public async Task EfCoreSemanticModel_preserves_closed_application_lineage()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = AppSemanticTypeModel.Create().DeriveEfCoreModel();
        EfCoreSourceTypeMapping money = result.Model.SourceTypes.Single(type => type.SourceSemanticTypeId == typeof(Money).FullName);
        EfCoreSourcePropertyMapping extensionData = money.Properties.Single(property => property.SourceMemberName == nameof(ExtensibleObject.ExtensionData));

        _ = await Assert.That(result.Model.SourceModelId).IsEqualTo("M0051");
        _ = await Assert.That(money.IsValueObject).IsTrue();
        _ = await Assert.That(money.IsOwned).IsTrue();
        _ = await Assert.That(extensionData.SourceDeclaringClrTypeName).Contains(typeof(ExtensibleObject).FullName!);
        _ = await Assert.That(extensionData.SemanticOnlyKind).IsEqualTo(EfCoreSemanticOnlyKind.ExtensionData);
        _ = await Assert.That(result.Model.SourceTypes.Single(type => type.IsRootEntity).OwnedMappings.Single().TargetSourceTypeId).IsEqualTo(typeof(Money).FullName);
    }

    [Test]
    public async Task ApplyEfCoreSemanticModel_requires_source_lineage_for_closed_application()
    {
        var builder = new ModelBuilder();
        var model = new EfCoreSemanticModel { Name = "lossy", EntityTypes = [], Diagnostics = [] };
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => builder.ApplyEfCoreSemanticModel(model));
        _ = await Assert.That(error.Message).Contains("EFCORE_SOURCE_LINEAGE_REQUIRED");
    }

    [Test]
    public async Task ApplyEfCoreSemanticModel_uses_same_closed_application_as_convenience_path()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<Order>();
        EfCoreSemanticModel model = AppSemanticTypeModel.Create().DeriveEfCoreModel().Model;
        builder.ApplyEfCoreSemanticModel(model);

        IMutableEntityType money = builder.Model.FindEntityType(typeof(Money))!;
        _ = await Assert.That(money.IsOwned()).IsTrue();
        _ = await Assert.That(money.FindProperty(nameof(ExtensibleObject.ExtensionData))).IsNull();
    }

    [Test]
    public async Task ClosedModelBuilder_suppresses_inherited_extension_data_in_real_DbContext()
    {
        await using var context = new AppDbContext();
        IModel model = context.Model;
        IEntityType money = model.FindEntityType(typeof(Money))!;

        _ = await Assert.That(model.FindEntityType(typeof(Order))).IsNotNull();
        _ = await Assert.That(money.IsOwned()).IsTrue();
        _ = await Assert.That(money.FindProperty(nameof(ExtensibleObject.ExtensionData))).IsNull();
        _ = await Assert.That(money.FindNavigation(nameof(ExtensibleObject.ExtensionData))).IsNull();
        _ = await Assert.That(money.FindSkipNavigation(nameof(ExtensibleObject.ExtensionData))).IsNull();
        _ = await Assert.That(money.GetForeignKeys().Any(fk => fk.Properties.Any(p => p.Name == nameof(ExtensibleObject.ExtensionData)))).IsFalse();
        _ = await Assert.That(model.FindEntityType(typeof(ExtensibleObject))).IsNull();

        PropertyDefinition extensionData = AppSemanticTypeModel.Create().Types.OfType<ObjectTypeDefinition>()
            .Single(type => type.Id.Value == typeof(Money).FullName).Properties.Single(property => property.Name == nameof(ExtensibleObject.ExtensionData));
        _ = await Assert.That(extensionData.Annotations.Items.Any(a => a.Key.Value == "schema.extensionData")).IsTrue();
    }

    [Test]
    public async Task ClosedModelBuilder_rejects_ValueObject_DbSet_root()
    {
        await using var context = new InvalidValueObjectDbContext();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => _ = context.Model);
        _ = await Assert.That(error.Message).Contains("cannot be used as a root EF entity or DbSet<T>");
    }

    [SemanticType(SemanticTypeRole.Entity)]
    public sealed class Order
    {
        [SemanticKey]
        public required Guid Id { get; init; }

        [SemanticOwned]
        public required Money Amount { get; init; }
    }

    [SemanticType(SemanticTypeRole.ValueObject)]
    public sealed class Money : ExtensibleObject
    {
        public required decimal Value { get; init; }
        public required string Currency { get; init; }
    }

    public abstract class ExtensibleObject
    {
        [SemanticExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; init; }
    }

    private sealed class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseInMemoryDatabase(nameof(AppDbContext));
        }

        public DbSet<Order> Orders => Set<Order>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticTypeModel(AppSemanticTypeModel.Create());
        }
    }

    private sealed class InvalidValueObjectDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseInMemoryDatabase(nameof(InvalidValueObjectDbContext));
        }

        public DbSet<Money> MoneyValues => Set<Money>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticTypeModel(AppSemanticTypeModel.Create());
        }
    }

    private static class AppSemanticTypeModel
    {
        public static TypeSchemaModel Create()
        {
            ScalarTypeDefinition guid = Scalar<Guid>(ScalarKind.Guid);
            ScalarTypeDefinition moneyValue = Scalar<decimal>(ScalarKind.Decimal);
            ScalarTypeDefinition text = Scalar<string>(ScalarKind.String);
            ObjectTypeDefinition money = new()
            {
                Id = new(typeof(Money).FullName!),
                Name = nameof(Money),
                Kind = TypeKind.Object,
                Nullability = Nullability.NonNullable,
                Annotations = Clr(typeof(Money)),
                Semantics = new EntitySemantics { Role = EntityRole.ValueObject, IsValueObject = true },
                Properties =
                [
                    Property(nameof(Money.Value), moneyValue.Id), Property(nameof(Money.Currency), text.Id),
                    Property(nameof(ExtensibleObject.ExtensionData), text.Id, ("schema.extensionData", "true")),
                ],
                Keys = [],
                Relationships = [],
            };
            ObjectTypeDefinition order = new()
            {
                Id = new(typeof(Order).FullName!),
                Name = nameof(Order),
                Kind = TypeKind.Object,
                Nullability = Nullability.NonNullable,
                Annotations = Clr(typeof(Order)),
                Semantics = new EntitySemantics { Role = EntityRole.Entity },
                Properties = [Property(nameof(Order.Id), guid.Id), Property(nameof(Order.Amount), money.Id, ("schema.ownedObject", "true"))],
                Keys = [new KeyDefinition { Name = "PK_Order", Kind = Abstractions.Model.KeyKind.Primary, Properties = [new PropertyRef(new(nameof(Order.Id)))], Annotations = Empty }],
                Relationships = [],
            };
            TypeDefinition[] types = [guid, moneyValue, text, money, order];
            return new TypeSchemaModel { Id = new("M0051"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = Empty };
        }

        private static readonly AnnotationBag Empty = new();
        private static ScalarTypeDefinition Scalar<T>(ScalarKind kind)
        {
            return new() { Id = new(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = kind, Annotations = Clr(typeof(T)) };
        }

        private static PropertyDefinition Property(string name, TypeId type, params (string Key, string Value)[] annotations)
        {
            return new() { Id = new(name), Name = name, Type = new(type), Cardinality = new() { IsRequired = true }, Mutability = Mutability.InitOnly, Constraints = new(), Annotations = new() { Items = [(new Annotation { Key = new("dotnet.memberName"), Value = name, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }), .. annotations.Select(a => new Annotation { Key = new(a.Key), Value = a.Value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared })] } };
        }

        private static AnnotationBag Clr(Type type)
        {
            return new() { Items = [new Annotation { Key = new("dotnet.clrType"), Value = $"global::{type.FullName}", Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }] };
        }
    }
}
#pragma warning restore CS1591
