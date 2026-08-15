using SemanticTypeModel.EFCore;
using SemanticTypeModel.EFCoreModelShapes;
using SemanticTypeModel.RealWorldFixtures;

namespace SemanticTypeModel.EFCore.Tests.Unit;

public sealed class M0060RelationalDerivationTests
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
    public async Task Relational_derivation_preserves_enum_string_provider_rule()
    {
        EfRelationalModel model = FixtureModels.CreateM0059EnumRegression().DeriveEfRelationalModel().Model;
        EfScalarColumn[] enumColumns = [.. model.Entities.SelectMany(entity => entity.ScalarColumns).Where(column => (Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType).IsEnum)];
        _ = await Assert.That(enumColumns).IsNotEmpty();
        _ = await Assert.That(enumColumns.All(column => column.ProviderType == typeof(string))).IsTrue();
    }
}
