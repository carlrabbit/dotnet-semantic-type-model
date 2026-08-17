using SemanticTypeModel.EFCore;
using SemanticTypeModel.EFCoreModelShapes;
using SemanticTypeModel.RealWorldFixtures;

namespace SemanticTypeModel.EFCore.Tests.Unit;

public sealed class RelationalDerivationTests
{
    [Test]
    public async Task Relational_derivation_preserves_TPT_and_inherited_member_placement()
    {
        EfRelationalModel model = ModelShapeModels.Tpt().DeriveEfRelationalModel().Model;
        EfEntity root = model.Entities.Single(entity => entity.ClrType == typeof(Specification));
        EfEntity derived = model.Entities.Single(entity => entity.ClrType == typeof(ImportSpecification));
        _ = await Assert.That(root.BaseEntityId).IsNull();
        _ = await Assert.That(derived.BaseEntityId).IsEqualTo(root.SemanticTypeId);
        _ = await Assert.That(derived.ScalarColumns.All(column => column.DeclaringClrType == typeof(ImportSpecification))).IsTrue();
    }

    [Test]
    public async Task Relational_derivation_preserves_JSON_and_binary_storage_rules()
    {
        EfRelationalModel json = ModelShapeModels.OwnedObject().DeriveEfRelationalModel().Model;
        EfRelationalModel binary = FixtureModels.CreateRunState().DeriveEfRelationalModel().Model;
        _ = await Assert.That(json.Entities.SelectMany(entity => entity.JsonColumns)).IsNotEmpty();
        _ = await Assert.That(binary.Entities.SelectMany(entity => entity.BinaryColumns)).IsNotEmpty();
    }

    [Test]
    public async Task Relational_projection_preserves_owned_JSON_property_use_nullability()
    {
        EfJsonColumn[] objects = [.. ModelShapeModels.JsonInheritance().DeriveEfRelationalModel().Model.Entities.SelectMany(entity => entity.JsonColumns)];
        EfJsonColumn[] collections = [.. ModelShapeModels.OwnedCollection().DeriveEfRelationalModel().Model.Entities.SelectMany(entity => entity.JsonColumns)];

        _ = await Assert.That(objects.Single(column => column.MemberName == nameof(JsonBase.OptionalSource)).IsNullable).IsTrue();
        _ = await Assert.That(objects.Single(column => column.MemberName == nameof(JsonDerived.RequiredSource)).IsNullable).IsFalse();
        _ = await Assert.That(collections.Single(column => column.MemberName == nameof(FieldConfiguredObject.DerivedFields)).IsNullable).IsFalse();
        _ = await Assert.That(collections.Single(column => column.MemberName == nameof(FieldConfiguredObject.OptionalDerivedFields)).IsNullable).IsTrue();
    }

    [Test]
    public async Task Relational_projection_covers_supported_storage_nullability_matrix()
    {
        EfEntity entity = ModelShapeModels.StorageNullabilityMatrix().DeriveEfRelationalModel().Model.Entities.Single();

        await AssertColumnNullability(entity.ScalarColumns, nameof(StorageMatrixEntity.RequiredText), nameof(StorageMatrixEntity.OptionalText), typeof(string));
        await AssertColumnNullability(entity.ScalarColumns, nameof(StorageMatrixEntity.RequiredState), nameof(StorageMatrixEntity.OptionalState), typeof(string));
        await AssertColumnNullability(entity.ScalarColumns, nameof(StorageMatrixEntity.RequiredStrongId), nameof(StorageMatrixEntity.OptionalStrongId), typeof(Guid));
        await AssertColumnNullability(entity.ScalarColumns, nameof(StorageMatrixEntity.RequiredUri), nameof(StorageMatrixEntity.OptionalUri), typeof(string));
        await AssertColumnNullability(entity.BinaryColumns, nameof(StorageMatrixEntity.RequiredBinary), nameof(StorageMatrixEntity.OptionalBinary), typeof(byte[]));
        await AssertColumnNullability(entity.BinaryColumns, nameof(StorageMatrixEntity.RequiredReadOnlyMemory), nameof(StorageMatrixEntity.OptionalReadOnlyMemory), typeof(byte[]));
        await AssertJsonNullability(entity.JsonColumns, nameof(StorageMatrixEntity.RequiredDetails), nameof(StorageMatrixEntity.OptionalDetails), EfJsonShape.Object);
        await AssertJsonNullability(entity.JsonColumns, nameof(StorageMatrixEntity.RequiredDetailsCollection), nameof(StorageMatrixEntity.OptionalDetailsCollection), EfJsonShape.Array);
        EfJsonColumn extensionData = entity.JsonColumns.Single(column => column.MemberName == nameof(StorageMatrixEntity.ExtensionData));
        _ = await Assert.That(extensionData.IsNullable).IsTrue();
        _ = await Assert.That(extensionData.JsonShape).IsEqualTo(EfJsonShape.ExtensionData);
    }

    [Test]
    public async Task Relational_derivation_preserves_enum_string_provider_rule()
    {
        EfRelationalModel model = FixtureModels.CreateM0059EnumRegression().DeriveEfRelationalModel().Model;
        EfScalarColumn[] enumColumns = [.. model.Entities.SelectMany(entity => entity.ScalarColumns).Where(column => (Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType).IsEnum)];
        _ = await Assert.That(enumColumns).IsNotEmpty();
        _ = await Assert.That(enumColumns.All(column => column.ProviderType == typeof(string))).IsTrue();
    }


    private static async Task AssertColumnNullability(
        IReadOnlyList<EfScalarColumn> columns,
        string requiredName,
        string optionalName,
        Type providerType)
    {
        EfScalarColumn required = columns.Single(column => column.MemberName == requiredName);
        EfScalarColumn optional = columns.Single(column => column.MemberName == optionalName);
        _ = await Assert.That(required.IsNullable).IsFalse();
        _ = await Assert.That(optional.IsNullable).IsTrue();
        _ = await Assert.That(required.ProviderType).IsEqualTo(providerType);
        _ = await Assert.That(optional.ProviderType).IsEqualTo(providerType);
    }

    private static async Task AssertJsonNullability(
        IReadOnlyList<EfJsonColumn> columns,
        string requiredName,
        string optionalName,
        EfJsonShape shape)
    {
        EfJsonColumn required = columns.Single(column => column.MemberName == requiredName);
        EfJsonColumn optional = columns.Single(column => column.MemberName == optionalName);
        _ = await Assert.That(required.IsNullable).IsFalse();
        _ = await Assert.That(optional.IsNullable).IsTrue();
        _ = await Assert.That(required.JsonShape).IsEqualTo(shape);
        _ = await Assert.That(optional.JsonShape).IsEqualTo(shape);
    }
}
