#pragma warning disable CA1707
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.EFCoreModelShapes;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
public sealed class M0057ModelShapeMatrixTests
{
    [Test]
    public async Task ModelShape_every_valid_ModelBuilder_has_exact_semantic_entity_inventory()
    {
        (TypeSchemaModel Model, Type[] Expected)[] cases =
        [
            (ModelShapeModels.Flat(), [typeof(FlatOrder)]),
            (ModelShapeModels.NonSemanticBaseScalar(), [typeof(VersionedOrder)]),
            (ModelShapeModels.ExtensionData(), [typeof(ExtensibleOrder)]),
            (ModelShapeModels.OwnedObject(), [typeof(SourceOrder)]),
            (ModelShapeModels.OwnedCollection(), [typeof(FieldConfiguredOrder)]),
            (ModelShapeModels.Tpt(), [typeof(Specification), typeof(ImportSpecification), typeof(WorkflowSpecification)]),
            (ModelShapeModels.ReusedNestedValueKind(), [typeof(SourceConsumer), typeof(AlternateSourceConsumer)]),
            (ModelShapeModels.SemanticChain(), [typeof(SemanticChainBase), typeof(SemanticChainDerived)]),
            (ModelShapeModels.JsonInheritance(), [typeof(JsonBase), typeof(JsonDerived)]),
        ];

        foreach ((TypeSchemaModel semantic, Type[] expected) in cases)
        {
            var builder = new ModelBuilder();
            EfRelationalApplicationResult application = builder.ApplySemanticRelationalModel(semantic.DeriveEfRelationalModel().Model);
            _ = await Assert.That(application.Diagnostics.Any(diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
            _ = await Assert.That(builder.FinalizeModel().GetEntityTypes().Select(entity => entity.ClrType).ToHashSet()).IsEquivalentTo(expected);
        }
    }

    [Test]
    public async Task NonSemanticBase_scalar_and_ExtensionData_use_first_semantic_storage_entity()
    {
        EfRelationalModel scalar = ModelShapeModels.NonSemanticBaseScalar().DeriveEfRelationalModel().Model;
        EfScalarColumn version = scalar.Entities.Single().ScalarColumns.Single(column => column.MemberName == nameof(VersionedObject.SchemaVersion));
        _ = await Assert.That(version.DeclaringClrType).IsEqualTo(typeof(VersionedObject));
        _ = await Assert.That(version.StorageClrType).IsEqualTo(typeof(VersionedOrder));
        _ = await Assert.That(version.SemanticDeclaringTypeId).IsEqualTo(typeof(VersionedOrder).FullName);
        _ = await Assert.That(version.StorageSemanticTypeId).IsEqualTo(typeof(VersionedOrder).FullName);

        EfJsonColumn extension = ModelShapeModels.ExtensionData().DeriveEfRelationalModel().Model.Entities.Single().JsonColumns.Single();
        _ = await Assert.That(extension.DeclaringClrType).IsEqualTo(typeof(ExtensibleObject));
        _ = await Assert.That(extension.StorageClrType).IsEqualTo(typeof(ExtensibleOrder));
        _ = await Assert.That(extension.JsonShape).IsEqualTo(EfJsonShape.ExtensionData);
    }

    [Test]
    public async Task Tpt_and_NonSemanticBase_chain_place_only_local_properties_on_derived_entities()
    {
        var builder = new ModelBuilder();
        _ = builder.ApplySemanticRelationalModel(ModelShapeModels.Tpt().DeriveEfRelationalModel().Model);
        IModel model = builder.FinalizeModel();
        IEntityType root = model.FindEntityType(typeof(Specification))!;
        IEntityType import = model.FindEntityType(typeof(ImportSpecification))!;
        _ = await Assert.That(root.GetDeclaredProperties().Select(property => property.Name)).Contains(nameof(VersionedExtensibleObject.SchemaVersion));
        _ = await Assert.That(root.GetDeclaredProperties().Select(property => property.Name)).Contains(nameof(Specification.DisplayName));
        _ = await Assert.That(import.GetDeclaredProperties().Select(property => property.Name)).Contains(nameof(ImportSpecification.ImportName));
        _ = await Assert.That(import.GetDeclaredProperties().Select(property => property.Name)).DoesNotContain(nameof(Specification.DisplayName));
        _ = await Assert.That(import.GetDeclaredProperties().Select(property => property.Name)).DoesNotContain(nameof(VersionedExtensibleObject.SchemaVersion));

        var chainBuilder = new ModelBuilder();
        _ = chainBuilder.ApplySemanticRelationalModel(ModelShapeModels.SemanticChain().DeriveEfRelationalModel().Model);
        IModel chain = chainBuilder.FinalizeModel();
        _ = await Assert.That(chain.FindEntityType(typeof(SemanticChainBase))!.GetDeclaredProperties().Select(property => property.Name)).Contains(nameof(StructuralGrandbase.Tenant));
        _ = await Assert.That(chain.FindEntityType(typeof(SemanticChainDerived))!.GetDeclaredProperties().Select(property => property.Name)).DoesNotContain(nameof(StructuralGrandbase.Tenant));
    }

    [Test]
    public async Task ValueKind_object_collection_nested_reused_and_polluted_shapes_remain_converted_properties()
    {
        (TypeSchemaModel Model, Type[] Values, (Type Owner, string Member)[] Owners)[] cases =
        [
            (ModelShapeModels.OwnedObject(), [typeof(SourceOptions), typeof(RetryPolicy)], [(typeof(SourceOrder), nameof(SourceConfiguredObject.Source))]),
            (ModelShapeModels.OwnedCollection(), [typeof(DerivedField)], [(typeof(FieldConfiguredOrder), nameof(FieldConfiguredObject.DerivedFields))]),
            (ModelShapeModels.ReusedNestedValueKind(), [typeof(SourceOptions), typeof(RetryPolicy)], [(typeof(SourceConsumer), nameof(SourceConsumer.Source)), (typeof(AlternateSourceConsumer), nameof(AlternateSourceConsumer.Source))]),
            (ModelShapeModels.JsonInheritance(), [typeof(SourceOptions), typeof(RetryPolicy)], [(typeof(JsonBase), nameof(JsonBase.OptionalSource)), (typeof(JsonDerived), nameof(JsonDerived.RequiredSource))]),
        ];

        foreach ((TypeSchemaModel semantic, Type[] values, (Type owner, string member)[] owners) in cases)
        {
            var builder = new ModelBuilder();
            foreach (Type value in values)
            {
                _ = builder.Entity(value).HasNoKey();
            }
            _ = builder.ApplySemanticRelationalModel(semantic.DeriveEfRelationalModel().Model);
            IModel model = builder.FinalizeModel();
            foreach (Type value in values)
            {
                _ = await Assert.That(model.GetEntityTypes().Any(entity => entity.ClrType == value)).IsFalse();
                _ = await Assert.That(model.FindEntityType(value)).IsNull();
            }
            foreach ((Type owner, var member) in owners)
            {
                _ = await Assert.That(model.FindEntityType(owner)!.FindProperty(member)!.GetValueConverter()).IsNotNull();
            }
        }
    }

    [Test]
    public async Task Hidden_new_member_produces_EF_MEMBER_DECLARATION_AMBIGUOUS()
    {
        EfRelationalModel relational = ModelShapeModels.Hidden().DeriveEfRelationalModel().Model;
        _ = await Assert.That(relational.Diagnostics.Select(diagnostic => diagnostic.Code)).Contains("EF_MEMBER_DECLARATION_AMBIGUOUS");
    }

    [Test]
    public async Task Semantic_base_and_derived_duplicate_name_is_resolved_to_each_local_declaration()
    {
        EfRelationalModel relational = ModelShapeModels.SemanticDuplicate().DeriveEfRelationalModel().Model;
        _ = await Assert.That(relational.Diagnostics.Any(diagnostic => diagnostic.Code == "EF_MEMBER_DECLARATION_AMBIGUOUS")).IsFalse();
        EfEntity root = relational.Entities.Single(entity => entity.ClrType == typeof(SemanticDuplicateBase));
        EfEntity derived = relational.Entities.Single(entity => entity.ClrType == typeof(SemanticDuplicateDerived));
        _ = await Assert.That(root.ScalarColumns.Single(column => column.MemberName == nameof(SemanticDuplicateBase.Name)).DeclaringClrType).IsEqualTo(typeof(SemanticDuplicateBase));
        _ = await Assert.That(derived.ScalarColumns.Single(column => column.MemberName == nameof(SemanticDuplicateDerived.Name)).DeclaringClrType).IsEqualTo(typeof(SemanticDuplicateDerived));
    }

    [Test]
    public async Task DeclaringType_and_storage_metadata_defects_produce_diagnostics_before_EF_application()
    {
        EfRelationalModel valid = ModelShapeModels.Flat().DeriveEfRelationalModel().Model;
        EfEntity entity = valid.Entities.Single();
        EfScalarColumn column = entity.ScalarColumns.Single(column => column.MemberName == nameof(FlatOrder.Number));

        EfRelationalModel unresolved = valid with
        {
            Entities = [entity with { ScalarColumns = [column with { StorageClrType = typeof(VersionedObject) }] }],
        };
        EfRelationalApplicationResult unresolvedResult = new ModelBuilder().ApplySemanticRelationalModel(unresolved);
        _ = await Assert.That(unresolvedResult.Diagnostics.Select(diagnostic => diagnostic.Code)).Contains("EF_MEMBER_STORAGE_ENTITY_UNRESOLVED");

        EfRelationalModel mismatch = valid with
        {
            Entities = [entity with { ScalarColumns = [column with { DeclaringClrType = typeof(VersionedObject) }] }],
        };
        EfRelationalApplicationResult mismatchResult = new ModelBuilder().ApplySemanticRelationalModel(mismatch);
        _ = await Assert.That(mismatchResult.Diagnostics.Select(diagnostic => diagnostic.Code)).Contains("EF_MEMBER_DECLARING_TYPE_MISMATCH");
    }
}
#pragma warning restore CS1591
