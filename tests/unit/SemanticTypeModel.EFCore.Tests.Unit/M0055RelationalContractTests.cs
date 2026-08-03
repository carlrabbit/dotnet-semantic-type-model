#pragma warning disable CA1707
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
public sealed class M0055RelationalContractTests
{
    [Test]
    public async Task Specification_fixture_derives_three_TPT_tables_and_required_JSON_columns()
    {
        EfRelationalModel model = FixtureModels.CreateIntake().DeriveEfRelationalModel().Model;
        _ = await Assert.That(model.Diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(model.Entities.Select(e => e.Table)).IsEquivalentTo([nameof(Intake.Specification), nameof(Intake.ImportSpecification), nameof(Intake.WorkflowSpecification)]);
        EfEntity import = model.Entities.Single(e => e.ClrType == typeof(Intake.ImportSpecification));
        _ = await Assert.That(import.BaseEntityId).IsEqualTo(typeof(Intake.Specification).FullName);
        _ = await Assert.That(import.JsonColumns.Select(c => c.MemberName)).IsEquivalentTo(["DeliveryContract", "Schedule", "Polling", "CsvSource", "XmlSource", "PrimaryApiSource", "SecondaryApiSource", "PostProcessing", "DerivedProperties"]);
        EfEntity root = model.Entities.Single(e => e.ClrType == typeof(Intake.Specification));
        _ = await Assert.That(root.JsonColumns.Single(c => c.MemberName == nameof(Intake.VersionedExtensibleObject.ExtensionData)).JsonShape).IsEqualTo(EfJsonShape.ExtensionData);
        _ = await Assert.That(model.Entities.Any(e => e.ClrType == typeof(Intake.CsvSourceSpecification))).IsFalse();
    }

    [Test]
    public async Task Run_state_derivation_maps_strong_identifiers_binary_and_owned_collections()
    {
        EfRelationalModel model = FixtureModels.CreateRunState().DeriveEfRelationalModel().Model;
        _ = await Assert.That(model.Diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        EfEntity entity = model.Entities.Single();
        _ = await Assert.That(entity.ScalarColumns.Single(c => c.MemberName == nameof(RunState.OrderFulfillmentRunSnapshot.RunId)).ProviderType).IsEqualTo(typeof(Guid));
        _ = await Assert.That(entity.BinaryColumns.Single(c => c.MemberName == nameof(RunState.OrderFulfillmentRunSnapshot.RawPayload)).ProviderType).IsEqualTo(typeof(byte[]));
        _ = await Assert.That(entity.JsonColumns.Select(c => c.MemberName)).IsEquivalentTo(["Statistics", "ComponentState", "Executions", "Failures", "ControlOperations"]);
        _ = await Assert.That(model.Entities.Any(e => e.ClrType == typeof(RunState.SaveFulfillmentRunRequest))).IsFalse();
    }

    [Test]
    public async Task ModelBuilder_uses_TPT_JSON_columns_and_no_relationships()
    {
        var builder = new ModelBuilder();
        EfRelationalApplicationResult result = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        _ = await Assert.That(result.Diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        IModel model = builder.FinalizeModel();
        _ = await Assert.That(model.FindEntityType(typeof(Intake.Specification))!.GetTableName()).IsEqualTo(nameof(Intake.Specification));
        _ = await Assert.That(model.FindEntityType(typeof(Intake.ImportSpecification))!.GetTableName()).IsEqualTo(nameof(Intake.ImportSpecification));
        _ = await Assert.That(model.FindEntityType(typeof(Intake.WorkflowSpecification))!.GetTableName()).IsEqualTo(nameof(Intake.WorkflowSpecification));
        _ = await Assert.That(model.GetEntityTypes().Any(e => e.ClrType == typeof(Intake.DeliveryContract))).IsFalse();
        _ = await Assert.That(model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).Any(f => !f.IsOwnership && f.DeclaringEntityType.BaseType is null)).IsFalse();
    }

    [Test]
    public async Task Application_reports_unexpected_convention_entity_without_mutating_the_model()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<UnexpectedConventionEntity>();
        EfRelationalApplicationResult result = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        _ = await Assert.That(result.Diagnostics.Any(d => d.Code == "EF_UNEXPECTED_CONVENTION_ENTITY")).IsTrue();
        _ = await Assert.That(builder.Model.FindEntityType(typeof(UnexpectedConventionEntity))).IsNotNull();
        _ = await Assert.That(builder.Model.FindEntityType(typeof(Intake.Specification))).IsNull();
    }

    [Test]
    public async Task Semantic_inheritance_disagreement_is_diagnostic_and_does_not_use_CLR_inheritance()
    {
        TypeSchemaModel original = FixtureModels.CreateIntake();
        TypeDefinition[] types = [.. original.Types.Select(type => type is ObjectTypeDefinition entity && entity.Id.Value == typeof(Intake.ImportSpecification).FullName
            ? entity with { Annotations = ReplaceAnnotation(entity.Annotations, "dotnet.baseType", typeof(Intake.WorkflowSpecification).FullName!) }
            : type)];
        TypeSchemaModel invalid = new() { Id = original.Id, Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = original.Annotations };
        EfRelationalModel model = invalid.DeriveEfRelationalModel().Model;
        _ = await Assert.That(model.Diagnostics.Any(diagnostic => diagnostic.Code == "EF_SEMANTIC_BASE_INHERITANCE_INVALID")).IsTrue();
        _ = await Assert.That(model.Entities.Single(entity => entity.ClrType == typeof(Intake.ImportSpecification)).BaseEntityId).IsNull();
    }

    private static AnnotationBag ReplaceAnnotation(AnnotationBag source, string key, string value)
    {
        return new AnnotationBag
        {
            Items = [.. source.Items.Where(annotation => annotation.Key.Value != key), new Annotation { Key = new AnnotationKey(key), Value = value, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }],
        };
    }

    private sealed class UnexpectedConventionEntity { public int Id { get; set; } }
}
#pragma warning restore CS1591
