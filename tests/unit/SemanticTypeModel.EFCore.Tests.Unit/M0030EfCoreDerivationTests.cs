using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable IDE0305
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0030EfCoreDerivationTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task DeriveEfCoreModel_should_return_domain_model_diagnostics_and_trace()
    {
        TypeSchemaModel model = BuildCustomerOrderModel();

        SemanticDerivationResult<EfCoreSemanticModel> result = model.DeriveEfCoreModel(options =>
        {
            _ = options.Transformations.Add(new EfCoreBoundaryTransformation());
            options.Projection = options.Projection with { DefaultInheritanceStrategy = EfCoreInheritanceStrategy.Tph };
        });

        _ = await Assert.That(result.Model.EntityTypes.Select(static entity => entity.Name)).IsEquivalentTo(["Customer", "Order"]);
        _ = await Assert.That(result.Diagnostics.All(static diagnostic => diagnostic.ProjectionTarget is null or ProjectionTarget.EfCore)).IsTrue();
        _ = await Assert.That(result.Trace.Entries.Any(static entry => entry.TransformationId == "efcore.boundary")).IsTrue();
        _ = await Assert.That(result.Model.ToEfCoreSemanticText()).Contains("EF Core Semantic Model: TestModel");
    }

    [Test]
    public async Task Projection_should_support_indexes_converter_metadata_and_explicit_inheritance()
    {
        TypeSchemaModel model = BuildInheritanceModel();

        EfModelDefinition projection = new EfCoreModelProjection(new EfCoreProjectionOptions { DefaultInheritanceStrategy = EfCoreInheritanceStrategy.Tph }).Project(model, new SchemaProjectionContext { Target = ProjectionTarget.EfCore });
        EfEntityTypeDefinition payment = projection.EntityTypes.Single(static entity => entity.Name == "CardPayment");
        EfPropertyDefinition token = payment.Properties.Single(static property => property.Name == "token");

        _ = await Assert.That(payment.Indexes.Single().PropertyNames).IsEquivalentTo(["token"]);
        _ = await Assert.That(payment.Indexes.Single().IsUnique).IsTrue();
        _ = await Assert.That(token.ConverterType).IsEqualTo(typeof(StringToBytesConverter));
        _ = await Assert.That(token.ProviderClrType).IsEqualTo(typeof(byte[]));
        _ = await Assert.That(payment.Inheritance!.Strategy).IsEqualTo(EfCoreInheritanceStrategy.Tph);
        _ = await Assert.That(payment.Inheritance!.BaseEntity).IsEqualTo("Payment");
        _ = await Assert.That(payment.Inheritance!.DiscriminatorProperty).IsEqualTo("payment_type");
    }

    [Test]
    public async Task ApplyEfCoreSemanticModel_should_apply_domain_model_without_database_provider()
    {
        TypeSchemaModel model = BuildCustomerOrderModel();
        SemanticDerivationResult<EfCoreSemanticModel> result = model.DeriveEfCoreModel(options => options.Projection = options.Projection with { ProjectUnannotatedObjectsAsEntities = false });
        var modelBuilder = new Microsoft.EntityFrameworkCore.ModelBuilder(new ConventionSet());

        modelBuilder.ApplyEfCoreSemanticModel(result.Model);

        _ = await Assert.That(modelBuilder.Model.FindEntityType("Customer")).IsNotNull();
        _ = await Assert.That(modelBuilder.Model.FindEntityType("Order")!.GetIndexes()).Count().IsEqualTo(1);
        _ = await Assert.That(modelBuilder.Model.FindEntityType("Order")!.GetForeignKeys()).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Projection_should_diagnose_ambiguous_inheritance_without_strategy()
    {
        TypeSchemaModel model = BuildInheritanceModel();

        EfModelDefinition projection = new EfCoreModelProjection().Project(model, new SchemaProjectionContext { Target = ProjectionTarget.EfCore });

        _ = await Assert.That(projection.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_INHERITANCE_STRATEGY_REQUIRED")).IsTrue();
    }

    private static TypeSchemaModel BuildCustomerOrderModel()
    {
        ScalarTypeDefinition intType = Scalar("Int64", ScalarKind.Integer);
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        ObjectTypeDefinition customer = Entity(
            "Customer",
            [
                Property("id", "CustomerId", intType.Id, true, false),
                Property("code", "CustomerCode", stringType.Id, true, false),
            ],
            [Key("PK_Customer", KeyKind.Primary, "CustomerId")]);

        ObjectTypeDefinition order = Entity(
            "Order",
            [
                Property("id", "OrderId", intType.Id, true, false),
                Property("customerId", "CustomerId", intType.Id, true, false, annotations: Annotation(("efCore.index", true))),
            ],
            [Key("PK_Order", KeyKind.Primary, "OrderId")],
            relationships:
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("Order_Customer"),
                    PrincipalType = new TypeRef(customer.Id),
                    DependentType = new TypeRef(new TypeId("Order")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("CustomerId"))],
                    DependentProperties = [new PropertyRef(new PropertyId("CustomerId"))],
                    Cardinality = RelationshipCardinality.ManyToOne,
                    DeleteBehavior = DeleteBehaviorSemantics.Restrict,
                    Annotations = EmptyAnnotations,
                },
            ]);

        return BuildModel(intType, stringType, customer, order);
    }

    private static TypeSchemaModel BuildInheritanceModel()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        ObjectTypeDefinition payment = Entity("Payment", [Property("id", "PaymentId", stringType.Id, true, false)], [Key("PK_Payment", KeyKind.Primary, "PaymentId")]);
        ObjectTypeDefinition card = Entity(
            "CardPayment",
            [
                Property("id", "PaymentId", stringType.Id, true, false),
                Property("token", "Token", stringType.Id, true, false, annotations: Annotation(("efCore.index", "IX_CardPayment_Token"), ("efCore.uniqueIndex", true), ("efCore.valueConverterType", typeof(StringToBytesConverter)), ("efCore.providerClrType", typeof(byte[])))),
            ],
            [Key("PK_CardPayment", KeyKind.Primary, "PaymentId")],
            annotations: Annotation(("efCore.discriminatorProperty", "payment_type"), ("efCore.discriminatorValue", "card"))) with
        {
            Composition = new ObjectComposition { AllOf = [new TypeRef(payment.Id)] },
        };

        return BuildModel(stringType, payment, card);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new TypeSchemaModel
        {
            Id = new SchemaModelId("TestModel"),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id, static type => type),
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind)
    {
        return new ScalarTypeDefinition { Id = new TypeId(id), Name = id, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = EmptyAnnotations, ScalarKind = kind };
    }

    private static ObjectTypeDefinition Entity(string name, IReadOnlyList<PropertyDefinition> properties, IReadOnlyList<KeyDefinition> keys, IReadOnlyList<RelationshipDefinition>? relationships = null, AnnotationBag? annotations = null)
    {
        return new ObjectTypeDefinition
        {
            Id = new TypeId(name),
            Name = name,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = annotations ?? EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties = properties,
            Keys = keys,
            Relationships = relationships ?? [],
        };
    }

    private static KeyDefinition Key(string name, KeyKind kind, params string[] propertyIds)
    {
        return new KeyDefinition { Name = name, Kind = kind, Properties = propertyIds.Select(static id => new PropertyRef(new PropertyId(id))).ToArray(), Annotations = EmptyAnnotations };
    }

    private static PropertyDefinition Property(string name, string id, TypeId typeId, bool required, bool nullable, AnnotationBag? annotations = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(id),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = annotations ?? EmptyAnnotations,
        };
    }

    private static AnnotationBag Annotation(params (string Key, object? Value)[] values)
    {
        return new AnnotationBag { Items = values.Select(static value => new Annotation { Key = new AnnotationKey(value.Key), Value = value.Value, Scope = AnnotationScope.Projection, Source = AnnotationSource.Declared }).ToArray() };
    }

    private sealed class StringToBytesConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<string, byte[]>
    {
        public StringToBytesConverter()
            : base(value => System.Text.Encoding.UTF8.GetBytes(value), value => System.Text.Encoding.UTF8.GetString(value))
        {
        }
    }
}
#pragma warning restore IDE0305
#pragma warning restore CS1591
