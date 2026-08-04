#pragma warning disable CA1707
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.EFCoreModelShapes;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
public sealed class M0057MemberPlacementTests
{
    // Focus areas: DeclaringType, NonSemanticBase, Tpt, ValueKind, ExtensionData, and ModelShape.
    [Test]
    public async Task ModelShape_fixture_inventory_contains_all_required_surgical_shapes()
    {
        _ = await Assert.That(ModelShapeInventory.RequiredShapes.Count).IsEqualTo(15);
        _ = await Assert.That(ModelShapeInventory.RequiredShapes.Distinct().Count()).IsEqualTo(15);
    }

    [Test]
    public async Task MemberPlacement_columns_preserve_declaring_and_storage_metadata()
    {
        EfRelationalModel relational = FixtureModels.CreateIntake().DeriveEfRelationalModel().Model;
        EfEntity root = relational.Entities.Single(entity => entity.ClrType == typeof(Intake.Specification));
        EfScalarColumn inherited = root.ScalarColumns.Single(column => column.MemberName == nameof(Intake.VersionedExtensibleObject.SchemaVersion));
        _ = await Assert.That(inherited.DeclaringClrType).IsEqualTo(typeof(Intake.VersionedExtensibleObject));
        _ = await Assert.That(inherited.StorageClrType).IsEqualTo(typeof(Intake.Specification));
        _ = await Assert.That(inherited.StorageSemanticTypeId).IsEqualTo(root.SemanticTypeId);
        EfJsonColumn extension = root.JsonColumns.Single(column => column.MemberName == nameof(Intake.VersionedExtensibleObject.ExtensionData));
        _ = await Assert.That(extension.DeclaringClrType).IsEqualTo(typeof(Intake.VersionedExtensibleObject));
        _ = await Assert.That(extension.StorageClrType).IsEqualTo(typeof(Intake.Specification));
    }

    [Test]
    public async Task Tpt_MemberPlacement_does_not_duplicate_inherited_properties_on_derived_entities()
    {
        EfRelationalModel relational = FixtureModels.CreateIntake().DeriveEfRelationalModel().Model;
        var builder = new ModelBuilder();
        EfRelationalApplicationResult result = builder.ApplySemanticRelationalModel(relational);
        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        IModel model = builder.FinalizeModel();
        Type[] expected = [typeof(Intake.Specification), typeof(Intake.ImportSpecification), typeof(Intake.WorkflowSpecification)];
        _ = await Assert.That(model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expected);
        IEntityType root = model.FindEntityType(typeof(Intake.Specification))!;
        IEntityType derived = model.FindEntityType(typeof(Intake.ImportSpecification))!;
        _ = await Assert.That(root.GetDeclaredProperties().Select(property => property.Name)).Contains(nameof(Intake.Specification.UpdatedAt));
        _ = await Assert.That(root.GetDeclaredProperties().Select(property => property.Name)).Contains(nameof(Intake.VersionedExtensibleObject.SchemaVersion));
        _ = await Assert.That(derived.GetDeclaredProperties().Select(property => property.Name)).DoesNotContain(nameof(Intake.Specification.UpdatedAt));
        _ = await Assert.That(derived.GetDeclaredProperties().Select(property => property.Name)).DoesNotContain(nameof(Intake.VersionedExtensibleObject.SchemaVersion));
    }

    [Test]
    public async Task ValueKind_ModelShape_remains_a_converted_property_not_an_entity()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<Intake.CsvSourceSpecification>().HasNoKey();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        IModel model = builder.FinalizeModel();
        _ = await Assert.That(model.FindEntityType(typeof(Intake.CsvSourceSpecification))).IsNull();
        IProperty property = model.FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.CsvSource))!;
        _ = await Assert.That(property.GetValueConverter()).IsNotNull();
    }
}
#pragma warning restore CS1591
