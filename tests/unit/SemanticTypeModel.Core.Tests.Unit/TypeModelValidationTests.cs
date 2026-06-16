using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Validation;

namespace SemanticTypeModel.Core.Tests.Unit;

/// <summary>
/// Verifies that <see cref="TypeSchemaModelValidator"/> emits the correct diagnostics for canonical invariant violations.
/// </summary>
#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class TypeModelValidationTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Validator_should_report_duplicate_type_id()
    {
        ScalarTypeDefinition a = Scalar("Duplicate");
        ScalarTypeDefinition b = Scalar("Duplicate");

        TypeSchemaModel model = new()
        {
            Id = new SchemaModelId("Test"),
            Types = [a, b],
            TypesById = new Dictionary<TypeId, TypeDefinition> { [a.Id] = a },
            Annotations = EmptyAnnotations,
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        await AssertDiagnostic(diagnostics, "STM0001", SchemaDiagnosticSeverity.Error, ModelPath.ForType(a.Id), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_unresolved_type_ref_on_property()
    {
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Order"),
            Name = "Order",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("OrderIdProp"),
                    Name = "id",
                    Type = new TypeRef(new TypeId("Missing")),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj));

        await AssertDiagnostic(diagnostics, "STM0002", SchemaDiagnosticSeverity.Error, ModelPath.ForProperty(obj.Id, "id"), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_not_report_errors_for_clean_model()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("CustomerNameProp"),
                    Name = "name",
                    Type = new TypeRef(stringType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, stringType));

        _ = await Assert.That(diagnostics.Where(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsEmpty();
    }

    [Test]
    public async Task Validator_should_report_duplicate_property_name()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("BadObject"),
            Name = "BadObject",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                Property("email", "Prop1", stringType.Id, true, false),
                Property("email", "Prop2", stringType.Id, false, false),
            ],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, stringType));

        await AssertDiagnostic(diagnostics, "STM0003", SchemaDiagnosticSeverity.Error, ModelPath.ForProperty(obj.Id, "email"), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_duplicate_key_name()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Keys"),
            Name = "Keys",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties = [Property("id", "IdProperty", stringType.Id, true, false)],
            Keys =
            [
                Key("PK_Keys", "IdProperty"),
                Key("PK_Keys", "IdProperty"),
            ],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, stringType));

        await AssertDiagnostic(diagnostics, "STM0004", SchemaDiagnosticSeverity.Error, ModelPath.ForKey(obj.Id, "PK_Keys"), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_missing_key_property_reference()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Keys"),
            Name = "Keys",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties = [Property("id", "IdProperty", stringType.Id, true, false)],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_Keys",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("MissingProperty"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, stringType));

        await AssertDiagnostic(diagnostics, "STM0005", SchemaDiagnosticSeverity.Error, "/types/Keys/keys/PK_Keys/MissingProperty", SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_relationship_with_missing_type()
    {
        ScalarTypeDefinition intType = Scalar("Int", ScalarKind.Integer);
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("OrderLine"),
            Name = "OrderLine",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties = [Property("orderId", "OrderIdProperty", intType.Id, true, false)],
            Keys = [],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("Order_OrderLine"),
                    PrincipalType = new TypeRef(new TypeId("MissingOrder")),
                    DependentType = new TypeRef(new TypeId("OrderLine")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("OrderIdProperty"))],
                    DependentProperties = [new PropertyRef(new PropertyId("OrderIdProperty"))],
                    Cardinality = RelationshipCardinality.OneToMany,
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, intType));

        await AssertDiagnostic(diagnostics, "STM0006", SchemaDiagnosticSeverity.Error, "/types/OrderLine/relationships/Order_OrderLine/principalType", SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_unresolved_property_ref_in_relationship()
    {
        ScalarTypeDefinition intType = Scalar("Int", ScalarKind.Integer);
        var principal = new ObjectTypeDefinition
        {
            Id = new TypeId("Order"),
            Name = "Order",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties = [Property("id", "OrderIdProp", intType.Id, true, false)],
            Keys = [],
            Relationships = [],
        };

        var dependent = new ObjectTypeDefinition
        {
            Id = new TypeId("OrderLine"),
            Name = "OrderLine",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties = [Property("orderId", "OrderLineFkProp", intType.Id, true, false)],
            Keys = [],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("Order_OrderLine"),
                    PrincipalType = new TypeRef(principal.Id),
                    DependentType = new TypeRef(new TypeId("OrderLine")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("NonExistentProp"))],
                    DependentProperties = [new PropertyRef(new PropertyId("OrderLineFkProp"))],
                    Cardinality = RelationshipCardinality.OneToMany,
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(principal, dependent, intType));

        await AssertDiagnostic(diagnostics, "STM0007", SchemaDiagnosticSeverity.Error, "/types/OrderLine/relationships/Order_OrderLine/principalProperties/NonExistentProp", SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_invalid_cardinality_when_min_exceeds_max()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Item"),
            Name = "Item",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("TagsProp"),
                    Name = "tags",
                    Type = new TypeRef(stringType.Id),
                    Cardinality = new Cardinality { IsRequired = true, MinItems = 5, MaxItems = 2 },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, stringType));

        await AssertDiagnostic(diagnostics, "STM0008", SchemaDiagnosticSeverity.Error, ModelPath.ForProperty(obj.Id, "tags"), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_invalid_numeric_constraints()
    {
        ScalarTypeDefinition decimalType = Scalar("Decimal", ScalarKind.Decimal);
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Product"),
            Name = "Product",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("PriceProp"),
                    Name = "price",
                    Type = new TypeRef(decimalType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet
                    {
                        Numeric = new NumericConstraints { Minimum = 10m, Maximum = 2m },
                    },
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, decimalType));

        await AssertDiagnostic(diagnostics, "STM0010", SchemaDiagnosticSeverity.Error, ModelPath.ForProperty(obj.Id, "price"), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_invalid_string_constraints()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Profile"),
            Name = "Profile",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("NameProp"),
                    Name = "name",
                    Type = new TypeRef(stringType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet
                    {
                        String = new StringConstraints { MinLength = -1, MaxLength = 10 },
                    },
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj, stringType));

        await AssertDiagnostic(diagnostics, "STM0009", SchemaDiagnosticSeverity.Error, ModelPath.ForProperty(obj.Id, "name"), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_annotation_key_without_namespace()
    {
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Annotated"),
            Name = "Annotated",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag
            {
                Items =
                [
                    new Annotation
                    {
                        Key = new AnnotationKey("noDot"),
                        Value = "value",
                        Scope = AnnotationScope.Type,
                        Source = AnnotationSource.Declared,
                    },
                ],
            },
            Properties = [],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj));

        await AssertDiagnostic(diagnostics, "STM0011", SchemaDiagnosticSeverity.Warning, ModelPath.ForAnnotation(ModelPath.ForType(obj.Id), new AnnotationKey("noDot")), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_report_reserved_namespace_with_incorrect_casing()
    {
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("Annotated"),
            Name = "Annotated",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag
            {
                Items =
                [
                    new Annotation
                    {
                        Key = new AnnotationKey("UI.order"),
                        Value = 1,
                        Scope = AnnotationScope.Type,
                        Source = AnnotationSource.Declared,
                    },
                ],
            },
            Properties = [],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj));

        await AssertDiagnostic(diagnostics, "STM0011", SchemaDiagnosticSeverity.Warning, ModelPath.ForAnnotation(ModelPath.ForType(obj.Id), new AnnotationKey("UI.order")), SchemaDiagnosticStage.Validation);
    }

    [Test]
    public async Task Validator_should_accept_valid_namespaced_annotation_keys()
    {
        var obj = new ObjectTypeDefinition
        {
            Id = new TypeId("GoodAnnotations"),
            Name = "GoodAnnotations",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag
            {
                Items =
                [
                    Annotation("ui.order", 1),
                    Annotation("efCore.tableName", "t_good"),
                    Annotation("jsonSchema.$defs", true),
                ],
            },
            Properties = [],
            Keys = [],
            Relationships = [],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(obj));

        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Code == "STM0011")).IsFalse();
    }

    [Test]
    public async Task Validator_should_report_duplicate_enum_names_and_values()
    {
        var enumType = new EnumTypeDefinition
        {
            Id = new TypeId("Status"),
            Name = "Status",
            Kind = TypeKind.Enum,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            StorageKind = EnumStorageKind.String,
            Values =
            [
                new EnumValueDefinition { Name = "Open", Value = "open", Annotations = EmptyAnnotations },
                new EnumValueDefinition { Name = "open", Value = "open", Annotations = EmptyAnnotations },
            ],
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(BuildModel(enumType));

        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Code == "STM0012")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Code == "STM0013")).IsTrue();
    }

    [Test]
    public async Task ModelPath_should_produce_canonical_format()
    {
        var typeId = new TypeId("Customer");
        var relId = new RelationshipId("Order_Customer");
        var annotationKey = new AnnotationKey("ui.order");

        _ = await Assert.That(ModelPath.ForType(typeId)).IsEqualTo("/types/Customer");
        _ = await Assert.That(ModelPath.ForProperty(typeId, "email")).IsEqualTo("/types/Customer/properties/email");
        _ = await Assert.That(ModelPath.ForRelationship(typeId, relId)).IsEqualTo("/types/Customer/relationships/Order_Customer");
        _ = await Assert.That(ModelPath.ForKey(typeId, "PK_Customer")).IsEqualTo("/types/Customer/keys/PK_Customer");
        _ = await Assert.That(ModelPath.ForComputedMember(typeId, "FullName")).IsEqualTo("/types/Customer/computedMembers/FullName");
        _ = await Assert.That(ModelPath.ForAnnotation("/types/Customer", annotationKey)).IsEqualTo("/types/Customer/annotations/ui.order");
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        Dictionary<TypeId, TypeDefinition> byId = [];
        foreach (TypeDefinition type in types)
        {
            if (!byId.ContainsKey(type.Id))
            {
                byId[type.Id] = type;
            }
        }

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("Test"),
            Types = types,
            TypesById = byId,
            Annotations = EmptyAnnotations,
        };
    }

    private static PropertyDefinition Property(string name, string propertyId, TypeId typeId, bool isRequired, bool allowsNull)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(propertyId),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality { IsRequired = isRequired, AllowsNull = allowsNull },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = EmptyAnnotations,
        };
    }

    private static KeyDefinition Key(string keyName, string propertyId)
    {
        return new KeyDefinition
        {
            Name = keyName,
            Kind = KeyKind.Primary,
            Properties = [new PropertyRef(new PropertyId(propertyId))],
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind scalarKind = ScalarKind.String)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(name),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = scalarKind,
        };
    }

    private static Annotation Annotation(string key, object? value)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = AnnotationScope.Type,
            Source = AnnotationSource.Declared,
        };
    }

    private static async Task AssertDiagnostic(
        IReadOnlyList<SchemaDiagnostic> diagnostics,
        string code,
        SchemaDiagnosticSeverity severity,
        string path,
        SchemaDiagnosticStage stage)
    {
        SchemaDiagnostic diagnostic = diagnostics.First(diagnostic => diagnostic.Code == code);
        _ = await Assert.That(diagnostic.Severity).IsEqualTo(severity);
        _ = await Assert.That(diagnostic.ModelPath).IsEqualTo(path);
        _ = await Assert.That(diagnostic.Stage).IsEqualTo(stage);
    }
}
#pragma warning restore CS1591
