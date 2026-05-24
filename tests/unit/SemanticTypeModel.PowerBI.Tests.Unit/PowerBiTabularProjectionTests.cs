using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.PowerBI.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable IDE0305
/// <summary>
/// Verifies M0007 Power BI / TOM-like projection fixture behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class PowerBiTabularProjectionTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Fixture_1_dimension_table_should_project_columns_keys_and_display_metadata()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        ScalarTypeDefinition dateType = Scalar("Date", ScalarKind.Date);
        ScalarTypeDefinition intType = Scalar("Int64", ScalarKind.Integer);
        var dimension = new ObjectTypeDefinition
        {
            Id = new TypeId("DimCustomer"),
            Name = "DimCustomer",
            DisplayName = "Customer",
            Description = "Customer dimension",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("powerBi.tableRole", "Dimension"), ("powerBi.displayFolder", "Reference")),
            Semantics = new EntitySemantics { Role = EntityRole.Dimension },
            Properties =
            [
                Property("customerKey", "CustomerKey", intType.Id, true, false),
                Property("name", "Name", stringType.Id, true, false, annotations: Annotation(("powerBi.dataCategory", "Category"))),
                Property("createdOn", "CreatedOn", dateType.Id, false, true),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_DimCustomer",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("CustomerKey"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        TabularModelDefinition projection = Project(BuildModel(dimension, stringType, dateType, intType));
        TabularTableDefinition table = projection.Tables.Single();
        TabularColumnDefinition keyColumn = table.Columns.Single(static column => column.Name == "customerKey");

        _ = await Assert.That(table.Name).IsEqualTo("Customer");
        _ = await Assert.That(table.DisplayFolder).IsEqualTo("Reference");
        _ = await Assert.That(keyColumn.IsKey).IsTrue();
        _ = await Assert.That(table.Columns.Single(static column => column.Name == "name").DataCategory).IsEqualTo("Category");
    }

    [Test]
    public async Task Fixture_2_fact_dimension_relationship_and_measure_should_project()
    {
        ScalarTypeDefinition intType = Scalar("Int64", ScalarKind.Integer);
        ScalarTypeDefinition decimalType = Scalar("Amount", ScalarKind.Decimal);

        var dimension = new ObjectTypeDefinition
        {
            Id = new TypeId("DimCustomer"),
            Name = "DimCustomer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("powerBi.tableRole", "Dimension")),
            Semantics = new EntitySemantics { Role = EntityRole.Dimension },
            Properties = [Property("customerKey", "DimCustomerKey", intType.Id, true, false)],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_DimCustomer",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("DimCustomerKey"))],
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
            Annotations = Annotation(("powerBi.tableRole", "Fact")),
            Semantics = new EntitySemantics { Role = EntityRole.Fact },
            Properties =
            [
                Property("salesKey", "FactSalesKey", intType.Id, true, false),
                Property("customerKey", "FactCustomerKey", intType.Id, true, false),
                Property("amount", "FactAmount", decimalType.Id, true, false),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_FactSales",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("FactSalesKey"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("FactSales_DimCustomer"),
                    PrincipalType = new TypeRef(dimension.Id),
                    DependentType = new TypeRef(new TypeId("FactSales")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("DimCustomerKey"))],
                    DependentProperties = [new PropertyRef(new PropertyId("FactCustomerKey"))],
                    Cardinality = RelationshipCardinality.ManyToOne,
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
                    Annotations = Annotation(("tom.measureFormatString", "$#,0.00")),
                },
            ],
        };

        TabularModelDefinition projection = Project(BuildModel(fact, dimension, intType, decimalType));

        _ = await Assert.That(projection.Tables.Count).IsEqualTo(2);
        _ = await Assert.That(projection.Relationships.Count).IsEqualTo(1);
        _ = await Assert.That(projection.Relationships[0].Cardinality).IsEqualTo(TabularRelationshipCardinality.ManyToOne);
        _ = await Assert.That(projection.Tables.Single(static table => table.Name == "FactSales").Measures.Single().Expression).IsEqualTo("SUM(FactSales[amount])");
    }

    [Test]
    public async Task Fixture_3_computed_member_should_preserve_dax_and_diagnose_unsupported_language()
    {
        ScalarTypeDefinition decimalType = Scalar("Amount", ScalarKind.Decimal);
        var fact = new ObjectTypeDefinition
        {
            Id = new TypeId("FactSales"),
            Name = "FactSales",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("powerBi.tableRole", "Fact")),
            Semantics = new EntitySemantics { Role = EntityRole.Fact },
            Properties = [Property("amount", "Amount", decimalType.Id, true, false)],
            Keys = [],
            Relationships = [],
            ComputedMembers =
            [
                new ComputedMemberDefinition
                {
                    Name = "Total Sales",
                    ResultType = new TypeRef(decimalType.Id),
                    Expression = new ExpressionDefinition { Language = "DAX", Body = "SUM(FactSales[amount])" },
                    Annotations = Annotation(("tom.measureExpression", "SUM(FactSales[amount])"), ("powerBi.displayFolder", "Financial")),
                },
                new ComputedMemberDefinition
                {
                    Name = "Total SQL",
                    ResultType = new TypeRef(decimalType.Id),
                    Expression = new ExpressionDefinition { Language = "SQL", Body = "SUM(amount)" },
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        TabularModelDefinition projection = Project(BuildModel(fact, decimalType));
        TabularMeasureDefinition measure = projection.Tables.Single().Measures.Single();

        _ = await Assert.That(measure.ExpressionLanguage).IsEqualTo("DAX");
        _ = await Assert.That(measure.DisplayFolder).IsEqualTo("Financial");
        _ = await Assert.That(projection.Diagnostics.Any(static diagnostic => diagnostic.Code == "POWERBI_UNSUPPORTED_MEASURE_EXPRESSION_LANGUAGE")).IsTrue();
    }

    [Test]
    public async Task Fixture_4_value_object_should_diagnose_flatten_and_serialize_deterministically()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var address = new ObjectTypeDefinition
        {
            Id = new TypeId("Address"),
            Name = "Address",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.ValueObject, IsValueObject = true },
            Properties =
            [
                Property("line1", "AddressLine1", stringType.Id, true, false),
                Property("city", "AddressCity", stringType.Id, true, false),
            ],
            Keys = [],
            Relationships = [],
        };

        var entity = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("powerBi.tableRole", "Entity")),
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties = [Property("address", "CustomerAddress", address.Id, false, true)],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(entity, address, stringType);

        TabularModelDefinition diagnosed = Project(model);
        TabularModelDefinition flattened = Project(model, new PowerBiProjectionOptions { ValueObjectProjectionMode = ValueObjectProjectionMode.Flatten });
        TabularModelDefinition serialized = Project(model, new PowerBiProjectionOptions { ValueObjectProjectionMode = ValueObjectProjectionMode.SerializeJson });

        _ = await Assert.That(diagnosed.Diagnostics.Any(static diagnostic => diagnostic.Code == "POWERBI_VALUE_OBJECT_UNSUPPORTED")).IsTrue();
        _ = await Assert.That(flattened.Tables.Single().Columns.Any(static column => column.Name == "address_line1")).IsTrue();
        _ = await Assert.That(serialized.Tables.Single().Columns.Any(static column => column.Name == "address")).IsTrue();
    }

    [Test]
    public async Task Fixture_5_unsupported_shapes_should_emit_diagnostics_or_serialize_when_configured()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var arrayType = new ArrayTypeDefinition
        {
            Id = new TypeId("TagArray"),
            Name = "TagArray",
            Kind = TypeKind.Array,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ItemType = new TypeRef(stringType.Id),
        };
        var dictionaryType = new DictionaryTypeDefinition
        {
            Id = new TypeId("MetricMap"),
            Name = "MetricMap",
            Kind = TypeKind.Dictionary,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            KeyType = new TypeRef(stringType.Id),
            ValueType = new TypeRef(stringType.Id),
        };
        var unionType = new UnionTypeDefinition
        {
            Id = new TypeId("UnionValue"),
            Name = "UnionValue",
            Kind = TypeKind.Union,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Options = [new TypeRef(stringType.Id)],
            Semantics = UnionSemantics.OneOf,
        };
        var entity = new ObjectTypeDefinition
        {
            Id = new TypeId("Telemetry"),
            Name = "Telemetry",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("powerBi.tableRole", "Entity")),
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("tags", "Tags", arrayType.Id, false, true),
                Property("metrics", "Metrics", dictionaryType.Id, false, true),
                Property("status", "Status", unionType.Id, false, true),
            ],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(entity, stringType, arrayType, dictionaryType, unionType);
        TabularModelDefinition diagnosed = Project(model);
        TabularModelDefinition serialized = Project(model, new PowerBiProjectionOptions { UnsupportedShapeBehavior = UnsupportedTabularShapeBehavior.SerializeJson });

        _ = await Assert.That(diagnosed.Diagnostics.Count(static diagnostic => diagnostic.Code == "POWERBI_UNSUPPORTED_SHAPE")).IsEqualTo(3);
        _ = await Assert.That(serialized.Tables.Single().Columns.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Fixture_6_name_collisions_should_be_diagnosed_or_suffixed_when_configured()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var entity = new ObjectTypeDefinition
        {
            Id = new TypeId("Order"),
            Name = "Order",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("powerBi.tableRole", "Entity")),
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("first", "First", stringType.Id, true, false, annotations: Annotation(("tom.columnName", "Duplicate"))),
                Property("second", "Second", stringType.Id, true, false, annotations: Annotation(("tom.columnName", "Duplicate"))),
            ],
            Keys = [],
            Relationships = [],
            ComputedMembers =
            [
                new ComputedMemberDefinition
                {
                    Name = "Revenue",
                    ResultType = new TypeRef(stringType.Id),
                    Expression = new ExpressionDefinition { Language = "DAX", Body = "1" },
                    Annotations = EmptyAnnotations,
                },
                new ComputedMemberDefinition
                {
                    Name = "Revenue",
                    ResultType = new TypeRef(stringType.Id),
                    Expression = new ExpressionDefinition { Language = "DAX", Body = "2" },
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        TypeSchemaModel model = BuildModel(entity, stringType);
        TabularModelDefinition diagnosed = Project(model);
        TabularModelDefinition suffixed = Project(model, new PowerBiProjectionOptions { NameCollisionBehavior = NameCollisionBehavior.Suffix });

        _ = await Assert.That(diagnosed.Diagnostics.Any(static diagnostic => diagnostic.Code == "POWERBI_DUPLICATE_PROJECTED_NAME")).IsTrue();
        _ = await Assert.That(suffixed.Tables.Single().Columns.Count(static column => column.Name.StartsWith("Duplicate", StringComparison.Ordinal))).IsEqualTo(2);
        _ = await Assert.That(suffixed.Tables.Single().Measures.Count(static measure => measure.Name.StartsWith("Revenue", StringComparison.Ordinal))).IsEqualTo(2);
    }

    private static TabularModelDefinition Project(TypeSchemaModel model, PowerBiProjectionOptions? options = null)
    {
        var projection = new PowerBiTabularProjection(options);
        var context = new SchemaProjectionContext { Target = ProjectionTarget.PowerBi };
        return projection.Project(model, context);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        Dictionary<TypeId, TypeDefinition> byId = types.ToDictionary(static type => type.Id, static type => type);
        return new TypeSchemaModel
        {
            Id = new SchemaModelId("PowerBiModel"),
            Types = types,
            TypesById = byId,
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind kind)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(name),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = kind,
        };
    }

    private static PropertyDefinition Property(
        string name,
        string propertyId,
        TypeId typeId,
        bool isRequired,
        bool allowsNull,
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
            },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = annotations ?? EmptyAnnotations,
        };
    }

    private static AnnotationBag Annotation(params (string key, object? value)[] items)
    {
        return new AnnotationBag
        {
            Items = items.Select(static item => new Annotation
            {
                Key = new AnnotationKey(item.key),
                Value = item.value,
                Scope = AnnotationScope.Projection,
                Source = AnnotationSource.Declared,
            }).ToArray(),
        };
    }
}
#pragma warning restore IDE0305
#pragma warning restore CS1591
