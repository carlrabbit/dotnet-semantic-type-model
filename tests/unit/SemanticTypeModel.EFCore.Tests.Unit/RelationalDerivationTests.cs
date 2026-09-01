using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.EFCore;
using Intake = SemanticTypeModel.TestModels.ModelA.Intake;
using ModelA = SemanticTypeModel.TestModels.ModelA;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;

namespace SemanticTypeModel.EFCore.Tests.Unit;

public sealed class RelationalDerivationTests
{
    [Test]
    public async Task Relational_derivation_preserves_TPT_and_inherited_member_placement()
    {
        EfRelationalModel model = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        EfEntity root = model.Entities.Single(entity => entity.ClrType == typeof(Intake.Specification));
        EfEntity derived = model.Entities.Single(entity => entity.ClrType == typeof(Intake.ImportSpecification));
        _ = await Assert.That(root.BaseEntityId).IsNull();
        _ = await Assert.That(derived.BaseEntityId).IsEqualTo(root.SemanticTypeId);
        _ = await Assert.That(derived.ScalarColumns.All(column => column.DeclaringClrType == typeof(Intake.ImportSpecification))).IsTrue();
    }

    [Test]
    public async Task Relational_derivation_preserves_JSON_and_binary_storage_rules()
    {
        EfRelationalModel json = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        EfRelationalModel binary = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        _ = await Assert.That(json.Entities.SelectMany(entity => entity.JsonColumns)).IsNotEmpty();
        _ = await Assert.That(binary.Entities.SelectMany(entity => entity.BinaryColumns)).IsNotEmpty();
    }

    [Test]
    public async Task Relational_projection_preserves_owned_JSON_property_use_nullability()
    {
        EfJsonColumn[] objects = [.. ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model.Entities.Where(entity => entity.ClrType == typeof(ModelA.InventoryItem)).SelectMany(entity => entity.JsonColumns)];
        EfJsonColumn[] collections = objects;

        _ = await Assert.That(objects.Single(column => column.MemberName == nameof(ModelA.InventoryItem.OptionalDetails)).IsNullable).IsTrue();
        _ = await Assert.That(objects.Single(column => column.MemberName == nameof(ModelA.InventoryItem.Details)).IsNullable).IsFalse();
        _ = await Assert.That(collections.Single(column => column.MemberName == nameof(ModelA.InventoryItem.DetailHistory)).IsNullable).IsFalse();
        _ = await Assert.That(collections.Single(column => column.MemberName == nameof(ModelA.InventoryItem.OptionalDetailHistory)).IsNullable).IsTrue();
    }

    [Test]
    public async Task Relational_projection_covers_supported_storage_nullability_matrix()
    {
        EfEntity entity = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model.Entities.Single(value => value.ClrType == typeof(ModelA.StorageMatrixEntity));

        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredText), nameof(ModelA.StorageMatrixEntity.OptionalText), typeof(string));
        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredState), nameof(ModelA.StorageMatrixEntity.OptionalState), typeof(string));
        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredStrongId), nameof(ModelA.StorageMatrixEntity.OptionalStrongId), typeof(Guid));
        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredUri), nameof(ModelA.StorageMatrixEntity.OptionalUri), typeof(string));
        await AssertColumnNullability(entity.BinaryColumns, nameof(ModelA.StorageMatrixEntity.RequiredBinary), nameof(ModelA.StorageMatrixEntity.OptionalBinary), typeof(byte[]));
        await AssertColumnNullability(entity.BinaryColumns, nameof(ModelA.StorageMatrixEntity.RequiredReadOnlyMemory), nameof(ModelA.StorageMatrixEntity.OptionalReadOnlyMemory), typeof(byte[]));
        await AssertJsonNullability(entity.JsonColumns, nameof(ModelA.StorageMatrixEntity.RequiredDetails), nameof(ModelA.StorageMatrixEntity.OptionalDetails), EfJsonShape.Object);
        await AssertJsonNullability(entity.JsonColumns, nameof(ModelA.StorageMatrixEntity.RequiredDetailsCollection), nameof(ModelA.StorageMatrixEntity.OptionalDetailsCollection), EfJsonShape.Array);
        EfJsonColumn extensionData = entity.JsonColumns.Single(column => column.MemberName == nameof(ModelA.StorageMatrixEntity.MatrixExtensionData));
        _ = await Assert.That(extensionData.IsNullable).IsTrue();
        _ = await Assert.That(extensionData.JsonShape).IsEqualTo(EfJsonShape.ExtensionData);
    }

    [Test]
    public async Task Relational_derivation_preserves_enum_string_provider_rule()
    {
        EfRelationalModel model = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        EfScalarColumn[] enumColumns = [.. model.Entities.SelectMany(entity => entity.ScalarColumns).Where(column => (Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType).IsEnum)];
        _ = await Assert.That(enumColumns).IsNotEmpty();
        _ = await Assert.That(enumColumns.All(column => column.ProviderType == typeof(string))).IsTrue();
    }

    [Test]
    public async Task Relational_projection_covers_complete_scalar_and_strong_scalar_matrix()
    {
        EfEntity entity = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model.Entities.Single(value => value.ClrType == typeof(ModelA.ProjectionMatrixEntity));
        EfScalarColumn[] columns = [.. entity.ScalarColumns.Concat(entity.BinaryColumns)];

        foreach (ModelA.ProjectionMatrixCase matrixCase in ModelA.ProjectionMatrix.Cases)
        {
            EfScalarColumn scalar = columns.Single(column => column.MemberName == matrixCase.PropertyName);
            EfScalarColumn optionalScalar = columns.Single(column => column.MemberName == matrixCase.OptionalPropertyName);
            EfScalarColumn strong = columns.Single(column => column.MemberName == matrixCase.StrongScalarPropertyName);
            EfScalarColumn optionalStrong = columns.Single(column => column.MemberName == matrixCase.OptionalStrongScalarPropertyName);

            _ = await Assert.That(scalar.ProviderType).IsEqualTo(ExpectedProviderType(matrixCase.ScalarKind));
            _ = await Assert.That(strong.ProviderType).IsEqualTo(ExpectedProviderType(matrixCase.ScalarKind));
            if (scalar.IsNullable)
            {
                throw new InvalidOperationException($"Required scalar column is nullable: {matrixCase.PropertyName}.");
            }
            _ = await Assert.That(optionalScalar.IsNullable).IsTrue();
            if (strong.IsNullable)
            {
                throw new InvalidOperationException($"Required strong scalar column is nullable: {matrixCase.StrongScalarPropertyName}.");
            }
            _ = await Assert.That(optionalStrong.IsNullable).IsTrue();
        }
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

    private static readonly Dictionary<ScalarKind, Type> ExpectedProviderTypes = new()
    {
        [ScalarKind.Boolean] = typeof(bool),
        [ScalarKind.String] = typeof(string),
        [ScalarKind.Integer] = typeof(long),
        [ScalarKind.Number] = typeof(double),
        [ScalarKind.Decimal] = typeof(decimal),
        [ScalarKind.Date] = typeof(DateOnly),
        [ScalarKind.Time] = typeof(TimeOnly),
        [ScalarKind.DateTime] = typeof(DateTime),
        [ScalarKind.DateTimeOffset] = typeof(DateTimeOffset),
        [ScalarKind.Duration] = typeof(TimeSpan),
        [ScalarKind.Guid] = typeof(Guid),
        [ScalarKind.Binary] = typeof(byte[]),
    };

    private static Type ExpectedProviderType(ScalarKind kind)
    {
        return ExpectedProviderTypes[kind];
    }
}
