using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;
using RunState = SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Milestone acceptance names are executable documentation.")]
public sealed class M0054RealWorldFixturesTests
{
    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_ReturnsNoLineageErrors_ForOrderIntakeSpecificationModel()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateIntake().DeriveEfCoreModel();
        _ = await Assert.That(result.Diagnostics.Any(IsLineageError)).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_DoesNotReportLineage_ForIEquatable()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateIntake().DeriveEfCoreModel();
        _ = await Assert.That(result.Diagnostics.Any(d => d.Code.Contains("SOURCE_LINEAGE", StringComparison.Ordinal) && d.Message.Contains("IEquatable", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_DoesNotReportLineage_ForGenericConfigurationInterface()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateIntake().DeriveEfCoreModel();
        _ = await Assert.That(result.Diagnostics.Any(d => d.Code.Contains("SOURCE_LINEAGE", StringComparison.Ordinal) && d.Message.Contains("IConfigurationKind", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_DoesNotReportLineage_ForSystemXmlOrJsonInfrastructure()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateIntake().DeriveEfCoreModel();
        _ = await Assert.That(result.Diagnostics.Any(d => d.Message.Contains("System.Xml", StringComparison.Ordinal) || d.Message.Contains("System.Text.Json", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_PreservesInheritedExtensionDataAsSuppressedMember()
    {
        EfCoreSourceTypeMapping source = FixtureModels.CreateIntake().DeriveEfCoreModel().Model.SourceTypes.Single(t => t.SourceSemanticTypeId == typeof(Intake.OrderIntakeSpecification).FullName);
        _ = await Assert.That(source.SuppressedMembers.Single().SourceDeclaringClrTypeName).Contains(nameof(Intake.VersionedExtensibleObject));
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_DoesNotTreatVersionedExtensibleObjectAsRootEntity()
    {
        EfCoreSemanticModel model = FixtureModels.CreateIntake().DeriveEfCoreModel().Model;
        _ = await Assert.That(model.SourceTypes.Any(t => t.SourceClrTypeName.Contains(nameof(Intake.VersionedExtensibleObject), StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_DoesNotTreatValueObjectsAsRootEntities()
    {
        EfCoreSemanticModel model = FixtureModels.CreateIntake().DeriveEfCoreModel().Model;
        _ = await Assert.That(model.SourceTypes.Any(t => t.IsValueObject && t.IsRootEntity)).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_ClosedClrModel_ReportsOwnedCollectionPolicyOnlyForActualOwnedCollection()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateIntake().DeriveEfCoreModel();
        _ = await Assert.That(result.Diagnostics.Count(d => d.Code == "EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED")).IsEqualTo(1);
    }

    [Test]
    public async Task DeriveEfCoreModel_ReturnsNoUnexpectedLineageErrors_ForRunStatePersistenceModel()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateRunState().DeriveEfCoreModel();
        _ = await Assert.That(result.Diagnostics.Any(IsLineageError)).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_HandlesRecordStructIdentifiers()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateRunState().DeriveEfCoreModel();
        EfCoreSourceTypeMapping snapshot = result.Model.SourceTypes.Single(type => type.IsRootEntity);
        _ = await Assert.That(snapshot.Properties.Single(property => property.SourceMemberName == nameof(RunState.OrderFulfillmentRunSnapshot.RunId)).StorageKind).IsEqualTo(EfCoreStorageKind.Suppressed);
        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "EFCORE_SOURCE_LINEAGE_STORAGE_UNSUPPORTED" && diagnostic.Message.Contains(nameof(RunState.FulfillmentRunId), StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task DeriveEfCoreModel_DoesNotProjectRequestDtosAsEntities()
    {
        EfCoreSemanticModel model = FixtureModels.CreateRunState().DeriveEfCoreModel().Model;
        _ = await Assert.That(model.SourceTypes.Any(t => t.SourceClrTypeName.Contains(nameof(RunState.SaveFulfillmentRunRequest), StringComparison.Ordinal))).IsFalse();
        _ = await Assert.That(model.SourceTypes.Any(t => t.SourceClrTypeName.Contains(nameof(RunState.FulfillmentRunOverview), StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_DoesNotProjectRepositoryAbstractionsAsEntities()
    {
        EfCoreSemanticModel model = FixtureModels.CreateRunState().DeriveEfCoreModel().Model;
        _ = await Assert.That(model.SourceTypes.Any(t => t.SourceClrTypeName.Contains(nameof(RunState.IFulfillmentRunStateRepository), StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_HandlesReadOnlyDictionaryReferences_WithConfiguredUnsupportedShapePolicy()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateRunState().DeriveEfCoreModel();
        EfCoreSourcePropertyMapping labels = result.Model.SourceTypes.Single(type => type.IsRootEntity).Properties.Single(property => property.SourceMemberName == nameof(RunState.OrderFulfillmentRunSnapshot.Labels));
        _ = await Assert.That(labels.StorageKind).IsEqualTo(EfCoreStorageKind.Suppressed);
        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("dictionary", StringComparison.OrdinalIgnoreCase))).IsTrue();
    }

    [Test]
    public async Task DeriveEfCoreModel_HandlesReadOnlyMemoryPayload_WithExpectedBinaryPolicy()
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = FixtureModels.CreateRunState().DeriveEfCoreModel();
        EfCoreSourceTypeMapping component = result.Model.SourceTypes.Single(type => type.SourceSemanticTypeId == typeof(RunState.ComponentStateEnvelope).FullName);
        _ = await Assert.That(component.Properties.Single(property => property.SourceMemberName == nameof(RunState.ComponentStateEnvelope.Payload)).StorageKind).IsEqualTo(EfCoreStorageKind.Suppressed);
        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "EFCORE_SOURCE_LINEAGE_STORAGE_UNSUPPORTED" && diagnostic.Message.Contains(nameof(ReadOnlyMemory<>), StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_BuildsModel_ForOrderIntakeSpecification()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_BuildsModel_ForOrderIntakeSpecification)).Options);
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.OrderIntakeSpecification))).IsNotNull();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_DoesNotMapExtensionData_AsPropertyNavigationOrRelationship()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_DoesNotMapExtensionData_AsPropertyNavigationOrRelationship)).Options);
        IEntityType entity = context.Model.FindEntityType(typeof(Intake.OrderIntakeSpecification))!;
        _ = await Assert.That(entity.FindProperty(nameof(Intake.VersionedExtensibleObject.ExtensionData))).IsNull();
        _ = await Assert.That(entity.FindNavigation(nameof(Intake.VersionedExtensibleObject.ExtensionData))).IsNull();
        _ = await Assert.That(entity.FindSkipNavigation(nameof(Intake.VersionedExtensibleObject.ExtensionData))).IsNull();
        _ = await Assert.That(entity.GetForeignKeys().Any(foreignKey => foreignKey.Properties.Any(property => property.Name == nameof(Intake.VersionedExtensibleObject.ExtensionData)))).IsFalse();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_DoesNotCreateEntity_ForVersionedExtensibleObject()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_DoesNotCreateEntity_ForVersionedExtensibleObject)).Options);
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.VersionedExtensibleObject))).IsNull();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_DoesNotCreateRootEntity_ForOwnedValueObjects()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_DoesNotCreateRootEntity_ForOwnedValueObjects)).Options);
        _ = await Assert.That(context.Model.FindEntityType(typeof(Intake.PartnerDeliveryAgreement))!.IsOwned()).IsTrue();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_DoesNotMapIEquatableOrConfigurationInterface()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_DoesNotMapIEquatableOrConfigurationInterface)).Options);
        _ = await Assert.That(context.Model.GetEntityTypes().Any(e => e.ClrType.IsInterface)).IsFalse();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_ConfiguresOwnedOptionalValueObjects()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_ConfiguresOwnedOptionalValueObjects)).Options);
        IEntityType root = context.Model.FindEntityType(typeof(Intake.OrderIntakeSpecification))!;
        _ = await Assert.That(root.FindNavigation(nameof(Intake.OrderIntakeSpecification.DelimitedFile))!.ForeignKey.IsRequiredDependent).IsFalse();
        _ = await Assert.That(root.FindNavigation(nameof(Intake.OrderIntakeSpecification.PrimaryApi))!.ForeignKey.IsRequiredDependent).IsFalse();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_HandlesOwnedCollectionPolicyDeterministically()
    {
        await using var context = new IntakeContext(new DbContextOptionsBuilder<IntakeContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_HandlesOwnedCollectionPolicyDeterministically)).Options);
        IEntityType root = context.Model.FindEntityType(typeof(Intake.OrderIntakeSpecification))!;
        _ = await Assert.That(root.FindNavigation(nameof(Intake.OrderIntakeSpecification.DerivedFields))).IsNull();
    }

    [Test]
    public async Task ClosedClrModel_ModelBuilder_BuildsModel_ForOrderFulfillmentRunState()
    {
        await using var context = new RunStateContext(new DbContextOptionsBuilder<RunStateContext>().UseInMemoryDatabase(nameof(ClosedClrModel_ModelBuilder_BuildsModel_ForOrderFulfillmentRunState)).Options);
        _ = await Assert.That(context.Model.FindEntityType(typeof(RunState.OrderFulfillmentRunSnapshot))).IsNotNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(RunState.SaveFulfillmentRunRequest))).IsNull();
        _ = await Assert.That(context.Model.FindEntityType(typeof(RunState.IFulfillmentRunStateRepository))).IsNull();
    }

    private static bool IsLineageError(SchemaDiagnostic diagnostic)
    {
        return diagnostic.Severity == SchemaDiagnosticSeverity.Error && diagnostic.Code.StartsWith("EFCORE_SOURCE_LINEAGE", StringComparison.Ordinal);
    }

    private sealed class IntakeContext(DbContextOptions<IntakeContext> options) : DbContext(options)
    {
        public DbSet<Intake.OrderIntakeSpecification> Specifications => Set<Intake.OrderIntakeSpecification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticTypeModel(FixtureModels.CreateIntake());
        }
    }

    private sealed class RunStateContext(DbContextOptions<RunStateContext> options) : DbContext(options)
    {
        public DbSet<RunState.OrderFulfillmentRunSnapshot> Snapshots => Set<RunState.OrderFulfillmentRunSnapshot>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.ApplySemanticTypeModel(FixtureModels.CreateRunState());
        }
    }
}

#pragma warning restore CS1591
