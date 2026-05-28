using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.EFCore.Tests.Unit;

// CS1591 and IDE0305 are disabled in this test fixture to keep focus on projection behavior.
#pragma warning disable CS1591
#pragma warning disable IDE0305
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class EfCoreModelBuilderProjectionTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task ApplySemanticTypeModel_should_configure_modelbuilder_and_return_projection_metadata()
    {
        TypeSchemaModel model = BuildSimpleEntityModel(withEntityRole: true, tableName: "customers");
        var modelBuilder = new ModelBuilder(new ConventionSet());

        EfCoreModelBuilderProjectionResult result = modelBuilder.ApplySemanticTypeModel(
            model,
            options => options.DefaultSchema = "app");

        IMutableEntityType? entityType = modelBuilder.Model.FindEntityType("Customer");

        _ = await Assert.That(result.Model.EntityTypes.Count).IsEqualTo(1);
        _ = await Assert.That(result.Diagnostics).Count().IsEqualTo(result.Model.Diagnostics.Count);
        _ = await Assert.That(entityType).IsNotNull();
        _ = await Assert.That(entityType!.GetSchema()).IsEqualTo("app");
        _ = await Assert.That(entityType.GetTableName()).IsEqualTo("customers");
        _ = await Assert.That(entityType.FindProperty("name")).IsNotNull();
    }

    [Test]
    public async Task ApplySemanticTypeModel_should_apply_options_callback_for_unannotated_objects()
    {
        TypeSchemaModel model = BuildSimpleEntityModel(withEntityRole: false);
        var modelBuilder = new ModelBuilder(new ConventionSet());

        EfCoreModelBuilderProjectionResult result = modelBuilder.ApplySemanticTypeModel(
            model,
            options => options.ProjectUnannotatedObjectsAsEntities = true);

        _ = await Assert.That(result.Model.EntityTypes.Count).IsEqualTo(1);
        _ = await Assert.That(modelBuilder.Model.FindEntityType("Customer")).IsNotNull();
    }

    [Test]
    public async Task ApplySemanticTypeModel_should_expose_projection_diagnostics()
    {
        TypeSchemaModel model = BuildSimpleEntityModel(withEntityRole: true, keyTypeId: new TypeId("MissingType"));
        var modelBuilder = new ModelBuilder(new ConventionSet());

        EfCoreModelBuilderProjectionResult result = modelBuilder.ApplySemanticTypeModel(model);

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_PROPERTY_TYPE_NOT_FOUND")).IsTrue();
    }

    private static TypeSchemaModel BuildSimpleEntityModel(bool withEntityRole, string? tableName = null, TypeId? keyTypeId = null)
    {
        ScalarTypeDefinition stringType = new()
        {
            Id = new TypeId("String"),
            Name = "String",
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = ScalarKind.String,
        };

        TypeId idTypeId = keyTypeId ?? stringType.Id;
        Dictionary<TypeId, TypeDefinition> typesById = new()
        {
            [stringType.Id] = stringType,
        };

        AnnotationBag typeAnnotations = string.IsNullOrWhiteSpace(tableName)
            ? EmptyAnnotations
            : new AnnotationBag
            {
                Items =
                [
                    new Annotation
                    {
                        Key = new AnnotationKey("efCore.tableName"),
                        Value = tableName,
                        Scope = AnnotationScope.Type,
                        Source = AnnotationSource.Declared,
                    },
                ],
            };

        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = typeAnnotations,
            Semantics = withEntityRole ? new EntitySemantics { Role = EntityRole.Entity } : new EntitySemantics(),
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("CustomerId"),
                    Name = "id",
                    Type = new TypeRef(idTypeId),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
                new PropertyDefinition
                {
                    Id = new PropertyId("CustomerName"),
                    Name = "name",
                    Type = new TypeRef(stringType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_Customer",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("CustomerId"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        typesById[customer.Id] = customer;

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("CustomerSchema"),
            Types = [stringType, customer],
            TypesById = typesById,
            Annotations = EmptyAnnotations,
        };
    }
}
#pragma warning restore IDE0305
#pragma warning restore CS1591
