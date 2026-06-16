using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class TypeModelContractsTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Contracts_should_represent_form_editor_example()
    {
        ScalarTypeDefinition emailType = Scalar("Email", ScalarKind.String, "email");
        ScalarTypeDefinition uriType = Scalar("Website", ScalarKind.String, "uri");
        ScalarTypeDefinition dateType = Scalar("BirthDate", ScalarKind.Date, "date");
        var statusType = new EnumTypeDefinition
        {
            Id = new TypeId("ContactStatus"),
            Name = "ContactStatus",
            Kind = TypeKind.Enum,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            StorageKind = EnumStorageKind.String,
            Values =
            [
                new EnumValueDefinition { Name = "Active", Value = "active", DisplayName = "Active Contact", Annotations = EmptyAnnotations },
                new EnumValueDefinition { Name = "Dormant", Value = "dormant", DisplayName = "Dormant Contact", Annotations = EmptyAnnotations },
            ],
        };

        var form = new ObjectTypeDefinition
        {
            Id = new TypeId("CustomerForm"),
            Name = "CustomerForm",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("ui.category", "Customer"),
            Properties =
            [
                Property("email", "EmailProperty", emailType.Id, true, false, 1, 1, Annotation("ui.order", 1)),
                Property("website", "WebsiteProperty", uriType.Id, false, true, 1, 1, Annotation("ui.order", 2)),
                Property("birthDate", "BirthDateProperty", dateType.Id, false, true, 1, 1, Annotation("jsonEditor.format", "date")),
                Property("status", "StatusProperty", statusType.Id, true, false, 1, 1, Annotation("ui.order", 3)),
            ],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(form, emailType, uriType, dateType, statusType);

        var resolved = (ObjectTypeDefinition)model.GetType(form.Id);
        _ = await Assert.That(resolved.Properties.Count).IsEqualTo(4);
        _ = await Assert.That(resolved.Properties[1].Cardinality.IsRequired).IsFalse();
        _ = await Assert.That(resolved.Properties[1].Cardinality.AllowsNull).IsTrue();
        _ = await Assert.That(statusType.Values[0].DisplayName).IsEqualTo("Active Contact");
    }

    [Test]
    public async Task Contracts_should_represent_ef_style_entity_example()
    {
        ScalarTypeDefinition intType = Scalar("Int32", ScalarKind.Integer);
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var addressType = new ObjectTypeDefinition
        {
            Id = new TypeId("Address"),
            Name = "Address",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("efCore.owned", true),
            Semantics = new EntitySemantics { Role = EntityRole.ValueObject, IsValueObject = true },
            Properties =
            [
                Property("line1", "AddressLine1Property", stringType.Id, true, false),
                Property("city", "AddressCityProperty", stringType.Id, true, false),
            ],
            Keys = [],
            Relationships = [],
        };

        var orderType = new ObjectTypeDefinition
        {
            Id = new TypeId("Order"),
            Name = "Order",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity, IsAggregateRoot = true },
            Properties =
            [
                Property("id", "OrderIdProperty", intType.Id, true, false, annotations: Annotation("efCore.valueGenerated", "OnAdd")),
                Property("orderNumber", "OrderNumberProperty", stringType.Id, true, false),
                Property("address", "OrderAddressProperty", addressType.Id, true, false),
                Property("status", "OrderStatusProperty", stringType.Id, false, true),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_Order",
                    Kind = KeyKind.Primary,
                    IsGenerated = true,
                    Properties = [new PropertyRef(new PropertyId("OrderIdProperty"))],
                    Annotations = EmptyAnnotations,
                },
                new KeyDefinition
                {
                    Name = "AK_Order_OrderNumber",
                    Kind = KeyKind.Alternate,
                    Properties = [new PropertyRef(new PropertyId("OrderNumberProperty"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        var orderLineType = new ObjectTypeDefinition
        {
            Id = new TypeId("OrderLine"),
            Name = "OrderLine",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                Property("id", "OrderLineIdProperty", intType.Id, true, false),
                Property("orderId", "OrderLineOrderIdProperty", intType.Id, true, false),
                Property("quantity", "OrderLineQuantityProperty", intType.Id, true, false),
            ],
            Keys = [],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("Order_OrderLine"),
                    PrincipalType = new TypeRef(orderType.Id),
                    DependentType = new TypeRef(new TypeId("OrderLine")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("OrderIdProperty"))],
                    DependentProperties = [new PropertyRef(new PropertyId("OrderLineOrderIdProperty"))],
                    Cardinality = RelationshipCardinality.OneToMany,
                    DeleteBehavior = DeleteBehaviorSemantics.Cascade,
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        TypeSchemaModel model = BuildModel(orderType, orderLineType, addressType, intType, stringType);

        var resolvedOrder = (ObjectTypeDefinition)model.GetType(orderType.Id);
        _ = await Assert.That(resolvedOrder.Keys.Count).IsEqualTo(2);
        _ = await Assert.That(resolvedOrder.Semantics.Role).IsEqualTo(EntityRole.Entity);
        _ = await Assert.That(((ObjectTypeDefinition)model.GetType(orderLineType.Id)).Relationships.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Contracts_should_represent_power_bi_star_schema_example()
    {
        ScalarTypeDefinition intType = Scalar("Int32", ScalarKind.Integer);
        ScalarTypeDefinition decimalType = Scalar("Decimal", ScalarKind.Decimal);
        ScalarTypeDefinition dateType = Scalar("Date", ScalarKind.Date);
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);

        var dimension = new ObjectTypeDefinition
        {
            Id = new TypeId("DimCustomer"),
            Name = "DimCustomer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("powerBi.tableRole", "Dimension"),
            Semantics = new EntitySemantics { Role = EntityRole.Dimension },
            Properties =
            [
                Property("customerKey", "DimCustomerKeyProperty", intType.Id, true, false),
                Property("name", "DimCustomerNameProperty", stringType.Id, true, false),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_DimCustomer",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("DimCustomerKeyProperty"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        var fact = new ObjectTypeDefinition
        {
            Id = new TypeId("FactSales"),
            Name = "FactSales",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("powerBi.tableRole", "Fact"),
            Semantics = new EntitySemantics { Role = EntityRole.Fact },
            Properties =
            [
                Property("salesKey", "FactSalesKeyProperty", intType.Id, true, false),
                Property("customerKey", "FactSalesCustomerKeyProperty", intType.Id, true, false),
                Property("saleDate", "FactSalesDateProperty", dateType.Id, true, false),
                Property("amount", "FactSalesAmountProperty", decimalType.Id, true, false),
            ],
            Keys = [],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("FactSales_DimCustomer"),
                    PrincipalType = new TypeRef(dimension.Id),
                    DependentType = new TypeRef(new TypeId("FactSales")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("DimCustomerKeyProperty"))],
                    DependentProperties = [new PropertyRef(new PropertyId("FactSalesCustomerKeyProperty"))],
                    Cardinality = RelationshipCardinality.OneToMany,
                    Annotations = EmptyAnnotations,
                },
            ],
            ComputedMembers =
            [
                new ComputedMemberDefinition
                {
                    Name = "Total Sales",
                    ResultType = new TypeRef(decimalType.Id),
                    Expression = new ExpressionDefinition { Language = "DAX", Body = "SUM(FactSales[amount])" },
                    Annotations = Annotation("tom.measureExpression", "SUM(FactSales[amount])"),
                },
            ],
        };

        TypeSchemaModel model = BuildModel(fact, dimension, intType, decimalType, dateType, stringType);

        var resolvedFact = (ObjectTypeDefinition)model.GetType(fact.Id);
        _ = await Assert.That(resolvedFact.ComputedMembers.Count).IsEqualTo(1);
        _ = await Assert.That(resolvedFact.ComputedMembers[0].Expression.Language).IsEqualTo("DAX");
        _ = await Assert.That(resolvedFact.Relationships[0].Cardinality).IsEqualTo(RelationshipCardinality.OneToMany);
    }

    [Test]
    public async Task Contracts_should_represent_json_schema_composition_example()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var nullType = new ReferenceTypeDefinition
        {
            Id = new TypeId("NullRef"),
            Name = "NullRef",
            Kind = TypeKind.Reference,
            Nullability = Nullability.Nullable,
            Annotations = EmptyAnnotations,
            Target = new TypeRef(new TypeId("NeverType")),
        };
        var neverType = new TypeDefinitionLeaf
        {
            Id = new TypeId("NeverType"),
            Name = "NeverType",
            Kind = TypeKind.Never,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
        };

        var contactCore = new ObjectTypeDefinition
        {
            Id = new TypeId("ContactCore"),
            Name = "ContactCore",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("jsonSchema.$defs", true),
            Properties = [Property("name", "ContactCoreNameProperty", stringType.Id, true, false)],
            Keys = [],
            Relationships = [],
        };

        var composed = new ObjectTypeDefinition
        {
            Id = new TypeId("ComposedContact"),
            Name = "ComposedContact",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("jsonSchema.unevaluatedProperties", false),
            Composition = new ObjectComposition { AllOf = [new TypeRef(contactCore.Id)] },
            Properties =
            [
                Property("email", "ComposedContactEmailProperty", stringType.Id, false, true, annotations: Annotation("jsonSchema.default", "unknown@example.com")),
            ],
            Keys = [],
            Relationships = [],
        };

        var union = new UnionTypeDefinition
        {
            Id = new TypeId("NullableName"),
            Name = "NullableName",
            Kind = TypeKind.Union,
            Nullability = Nullability.Nullable,
            Annotations = EmptyAnnotations,
            Semantics = UnionSemantics.OneOf,
            Options = [new TypeRef(stringType.Id), new TypeRef(nullType.Id)],
            Discriminator = null,
        };

        TypeSchemaModel model = BuildModel(composed, contactCore, union, stringType, nullType, neverType);

        var resolvedComposed = (ObjectTypeDefinition)model.GetType(composed.Id);
        var resolvedUnion = (UnionTypeDefinition)model.GetType(union.Id);
        _ = await Assert.That(resolvedComposed.Composition.AllOf.Count).IsEqualTo(1);
        _ = await Assert.That(resolvedUnion.Options.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Contracts_should_distinguish_requiredness_nullability_and_cardinality()
    {
        PropertyDefinition optionalNullable = Property("nickname", "NicknameProperty", new TypeId("String"), false, true, 0, 1);
        PropertyDefinition requiredCollection = Property("tags", "TagsProperty", new TypeId("StringArray"), true, false, 1, 10);

        _ = await Assert.That(optionalNullable.Cardinality.IsRequired).IsFalse();
        _ = await Assert.That(optionalNullable.Cardinality.AllowsNull).IsTrue();
        _ = await Assert.That(requiredCollection.Cardinality.MinItems).IsEqualTo(1);
        _ = await Assert.That(requiredCollection.Cardinality.MaxItems).IsEqualTo(10);
    }

    [Test]
    public async Task Contracts_should_preserve_annotation_namespace_and_value()
    {
        Annotation annotation = new()
        {
            Key = new AnnotationKey("ui.order"),
            Value = 10,
            Scope = AnnotationScope.Member,
            Source = AnnotationSource.Imported,
        };

        _ = await Assert.That(annotation.Key.Value).IsEqualTo("ui.order");
        _ = await Assert.That(annotation.Value).IsEqualTo(10);
    }

    [Test]
    public async Task Contracts_should_resolve_type_references_by_stable_id()
    {
        var parent = new ObjectTypeDefinition
        {
            Id = new TypeId("Node"),
            Name = "Node",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                Property("next", "NodeNextProperty", new TypeId("Node"), false, true),
            ],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(parent);
        var resolved = (ObjectTypeDefinition)model.GetType(parent.Id);
        TypeRef nextRef = resolved.Properties[0].Type;

        _ = await Assert.That(nextRef.Id).IsEqualTo(parent.Id);
        _ = await Assert.That(model.GetType(nextRef.Id).Name).IsEqualTo("Node");
    }

    [Test]
    public async Task Contracts_should_allow_projection_diagnostics_for_unsupported_cases()
    {
        SchemaDiagnostic[] diagnostics =
        [
            new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "EFCORE_UNION_UNSUPPORTED",
                Message = "Union types require transformation before EF Core projection.",
                Stage = SchemaDiagnosticStage.Projection,
                ModelPath = "/types/OrderStatus",
                ProjectionTarget = ProjectionTarget.EfCore,
            },
            new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "POWERBI_DICTIONARY_UNSUPPORTED",
                Message = "Dictionary types require flattening before Power BI projection.",
                Stage = SchemaDiagnosticStage.Projection,
                ModelPath = "/types/Telemetry",
                ProjectionTarget = ProjectionTarget.PowerBi,
            },
        ];

        _ = await Assert.That(diagnostics[0].ProjectionTarget).IsEqualTo(ProjectionTarget.EfCore);
        _ = await Assert.That(diagnostics[1].ProjectionTarget).IsEqualTo(ProjectionTarget.PowerBi);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        Dictionary<TypeId, TypeDefinition> byId = types.ToDictionary(t => t.Id, t => t);
        return new TypeSchemaModel
        {
            Id = new SchemaModelId("Model"),
            Types = types,
            TypesById = byId,
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind kind, string? format = null)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(name),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = kind,
            Format = format,
        };
    }

    private static PropertyDefinition Property(
        string name,
        string propertyId,
        TypeId typeId,
        bool isRequired,
        bool allowsNull,
        int? minItems = null,
        int? maxItems = null,
        AnnotationBag? annotations = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(propertyId),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality
            {
                IsRequired = isRequired,
                AllowsNull = allowsNull,
                MinItems = minItems,
                MaxItems = maxItems,
            },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = annotations ?? EmptyAnnotations,
        };
    }

    private static AnnotationBag Annotation(string key, object? value)
    {
        return new AnnotationBag
        {
            Items =
            [
                new Annotation
                {
                    Key = new AnnotationKey(key),
                    Value = value,
                    Scope = AnnotationScope.Member,
                    Source = AnnotationSource.Declared,
                },
            ],
        };
    }

    private sealed record TypeDefinitionLeaf : TypeDefinition;
}
#pragma warning restore CS1591
