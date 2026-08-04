#pragma warning disable CA1707
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
public sealed class M0056ConventionSuppressionTests
{
    // Keyless metadata is pollution to correct, never an alternative ValueKind mapping. SQLite
    // (Sqlite) provider coverage for the same contract lives in the matching M0056 integration fixture.
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
    public async Task Apply_DoesNot_Return_EF_UNEXPECTED_CONVENTION_ENTITY_When_Correction_Succeeds()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<UnexpectedConventionEntity>();
        EfRelationalApplicationResult result = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        _ = await Assert.That(result.Diagnostics.Any(d => d.Code == "EF_UNEXPECTED_CONVENTION_ENTITY")).IsFalse();
        _ = await Assert.That(builder.Model.FindEntityType(typeof(UnexpectedConventionEntity))).IsNull();
        await AssertExactIntakeEntities(builder.FinalizeModel());
    }

    [Test]
    public async Task Apply_Reports_EF_UNEXPECTED_CONVENTION_ENTITY_When_Type_Remains_After_Correction()
    {
        var conventions = new ConventionSet();
        conventions.EntityTypeRemovedConventions.Add(new ReintroducingEntityConvention());
        var builder = new ModelBuilder(conventions);
        _ = builder.Entity<UnexpectedConventionEntity>();

        EfRelationalApplicationResult result = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);

        SchemaDiagnostic diagnostic = result.Diagnostics.Single(d => d.Code == "EF_UNEXPECTED_CONVENTION_ENTITY");
        _ = await Assert.That(diagnostic.Message).Contains(typeof(ResidualConventionEntityTwo).FullName!);
        _ = await Assert.That(diagnostic.Message).Contains("keyless: True");
        _ = await Assert.That(diagnostic.Message).Contains(typeof(Intake.ImportSpecification).FullName!);
        _ = await Assert.That(builder.Model.FindEntityType(typeof(ResidualConventionEntityTwo))).IsNotNull();
    }

    [Test]
    public async Task Apply_Removes_PreDiscovered_ValueKind_Entity()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<Intake.CsvSourceSpecification>().HasNoKey();
        EfRelationalApplicationResult result = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        _ = await Assert.That(result.Diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        IModel final = builder.FinalizeModel();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.CsvSourceSpecification))).IsNull();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.XmlSourceSpecification))).IsNull();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.CsvSource))!.GetValueConverter()).IsNotNull();
        await AssertExactIntakeEntities(final);
    }

    [Test]
    public async Task Apply_Removes_PreDiscovered_ValueKind_CollectionItem()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<Intake.DerivedProperty>().HasNoKey();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        IModel final = builder.FinalizeModel();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.DerivedProperty))).IsNull();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.DerivedProperties))!.GetValueConverter()).IsNotNull();
        await AssertExactIntakeEntities(final);
    }

    [Test]
    public async Task Apply_Removes_PreDiscovered_NonSemantic_Base()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<Intake.VersionedExtensibleObject>().HasNoKey();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        IModel final = builder.FinalizeModel();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.VersionedExtensibleObject))).IsNull();
        await AssertExactIntakeEntities(final);
    }

    [Test]
    public async Task Apply_Preserves_Allowed_Semantic_Entities()
    {
        var builder = new ModelBuilder();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        await AssertExactIntakeEntities(builder.FinalizeModel());
    }

    [Test]
    public async Task Apply_Preserves_Exact_RunState_Entity_Inventory()
    {
        var builder = new ModelBuilder();
        EfRelationalModel relational = FixtureModels.CreateRunState().DeriveEfRelationalModel().Model;
        _ = builder.ApplySemanticRelationalModel(relational);
        Type[] expected = [.. relational.Entities.Select(entity => entity.ClrType)];
        _ = await Assert.That(builder.FinalizeModel().GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Apply_Preserves_Tpt_Base_And_Derived_Entities()
    {
        var builder = new ModelBuilder();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        IModel final = builder.FinalizeModel();
        _ = await Assert.That(final.FindEntityType(typeof(Intake.Specification))!.GetMappingStrategy()).IsEqualTo(RelationalAnnotationNames.TptMappingStrategy);
        await AssertExactIntakeEntities(final);
    }

    [Test]
    public async Task Apply_Maps_ValueKind_Object_As_Json_Property()
    {
        var builder = new ModelBuilder();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        IProperty property = builder.FinalizeModel().FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.CsvSource))!;
        _ = await Assert.That(property.GetValueConverter()).IsNotNull();
        _ = await Assert.That(property.ClrType).IsEqualTo(typeof(Intake.CsvSourceSpecification));
    }

    [Test]
    public async Task Apply_Maps_ValueKind_Collection_As_Json_Array_Property()
    {
        var builder = new ModelBuilder();
        _ = builder.ApplySemanticRelationalModel(FixtureModels.CreateIntake().DeriveEfRelationalModel().Model);
        IProperty property = builder.FinalizeModel().FindEntityType(typeof(Intake.ImportSpecification))!.FindProperty(nameof(Intake.ImportSpecification.DerivedProperties))!;
        _ = await Assert.That(property.GetValueConverter()).IsNotNull();
        _ = await Assert.That(property.ClrType).IsEqualTo(typeof(IReadOnlyList<Intake.DerivedProperty>));
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

    private static async Task AssertExactIntakeEntities(IModel model)
    {
        Type[] expected = [typeof(Intake.Specification), typeof(Intake.ImportSpecification), typeof(Intake.WorkflowSpecification)];
        _ = await Assert.That(model.GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expected);
    }

    private sealed class UnexpectedConventionEntity { public int Id { get; set; } }
    private sealed class ResidualConventionEntityOne;
    private sealed class ResidualConventionEntityTwo;

    private sealed class ReintroducingEntityConvention : IEntityTypeRemovedConvention
    {
        public void ProcessEntityTypeRemoved(IConventionModelBuilder modelBuilder, IConventionEntityType entityType, IConventionContext<IConventionEntityType> context)
        {
            if (entityType.ClrType == typeof(UnexpectedConventionEntity))
            {
                _ = modelBuilder.Entity(typeof(ResidualConventionEntityOne), fromDataAnnotation: false);
            }
            else if (entityType.ClrType == typeof(ResidualConventionEntityOne))
            {
                _ = modelBuilder.Entity(typeof(ResidualConventionEntityTwo), fromDataAnnotation: false);
            }
        }
    }
}
#pragma warning restore CS1591
