using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Validation;

namespace SemanticTypeModel.Core.Tests.Unit;

/// <summary>
/// Verifies that <see cref="TypeSchemaModelValidator"/> emits the correct diagnostics for each
/// class of model invariant violation.
/// </summary>
#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class TypeModelValidationTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    // -------------------------------------------------------------------------
    // 1. Duplicate TypeId
    // -------------------------------------------------------------------------

    [Test]
    public async Task Validator_should_report_duplicate_type_id()
    {
        ScalarTypeDefinition a = Scalar("Duplicate");
        ScalarTypeDefinition b = Scalar("Duplicate");

        // Manually construct a model that bypasses the dictionary de-duplication.
        TypeSchemaModel model = new()
        {
            Id = new SchemaModelId("Test"),
            Types = [a, b],
            TypesById = new Dictionary<TypeId, TypeDefinition> { [a.Id] = a },
            Annotations = EmptyAnnotations,
        };

        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_DUPLICATE_TYPE_ID")).IsTrue();
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_DUPLICATE_TYPE_ID").Severity)
            .IsEqualTo(SchemaDiagnosticSeverity.Error);
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_DUPLICATE_TYPE_ID").ModelPath)
            .IsEqualTo(ModelPath.ForType(a.Id));
    }

    // -------------------------------------------------------------------------
    // 2. Unresolved TypeRef
    // -------------------------------------------------------------------------

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

        TypeSchemaModel model = BuildModel(obj);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_UNRESOLVED_TYPE_REF")).IsTrue();
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_UNRESOLVED_TYPE_REF").ModelPath)
            .IsEqualTo(ModelPath.ForProperty(obj.Id, "id"));
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

        TypeSchemaModel model = BuildModel(obj, stringType);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Where(d => d.Severity == SchemaDiagnosticSeverity.Error)).IsEmpty();
    }

    // -------------------------------------------------------------------------
    // 3. Duplicate property names
    // -------------------------------------------------------------------------

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
                new PropertyDefinition
                {
                    Id = new PropertyId("Prop1"),
                    Name = "email",
                    Type = new TypeRef(stringType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
                new PropertyDefinition
                {
                    Id = new PropertyId("Prop2"),
                    Name = "email",
                    Type = new TypeRef(stringType.Id),
                    Cardinality = new Cardinality { IsRequired = false },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(obj, stringType);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_DUPLICATE_PROPERTY_NAME")).IsTrue();
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_DUPLICATE_PROPERTY_NAME").Severity)
            .IsEqualTo(SchemaDiagnosticSeverity.Error);
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_DUPLICATE_PROPERTY_NAME").ModelPath)
            .IsEqualTo(ModelPath.ForProperty(obj.Id, "email"));
    }

    // -------------------------------------------------------------------------
    // 4. Relationship references to missing properties
    // -------------------------------------------------------------------------

    [Test]
    public async Task Validator_should_report_unresolved_property_ref_in_relationship()
    {
        ScalarTypeDefinition intType = Scalar("Int");
        var principal = new ObjectTypeDefinition
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
                    Type = new TypeRef(intType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
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
            Properties =
            [
                new PropertyDefinition
                {
                    Id = new PropertyId("OrderLineFkProp"),
                    Name = "orderId",
                    Type = new TypeRef(intType.Id),
                    Cardinality = new Cardinality { IsRequired = true },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = EmptyAnnotations,
                },
            ],
            Keys = [],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("Order_OrderLine"),
                    PrincipalType = new TypeRef(principal.Id),
                    DependentType = new TypeRef(new TypeId("OrderLine")),
                    // References a property that does NOT exist in principal.
                    PrincipalProperties = [new PropertyRef(new PropertyId("NonExistentProp"))],
                    DependentProperties = [new PropertyRef(new PropertyId("OrderLineFkProp"))],
                    Cardinality = RelationshipCardinality.OneToMany,
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        TypeSchemaModel model = BuildModel(principal, dependent, intType);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_UNRESOLVED_PROPERTY_REF")).IsTrue();
    }

    // -------------------------------------------------------------------------
    // 5. Invalid cardinality
    // -------------------------------------------------------------------------

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

        TypeSchemaModel model = BuildModel(obj, stringType);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_INVALID_CARDINALITY")).IsTrue();
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_INVALID_CARDINALITY").Severity)
            .IsEqualTo(SchemaDiagnosticSeverity.Error);
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_INVALID_CARDINALITY").ModelPath)
            .IsEqualTo(ModelPath.ForProperty(obj.Id, "tags"));
    }

    [Test]
    public async Task Validator_should_report_invalid_cardinality_on_array_type()
    {
        ScalarTypeDefinition stringType = Scalar("String");
        var array = new ArrayTypeDefinition
        {
            Id = new TypeId("BadArray"),
            Name = "BadArray",
            Kind = TypeKind.Array,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ItemType = new TypeRef(stringType.Id),
            MinItems = 10,
            MaxItems = 3,
        };

        TypeSchemaModel model = BuildModel(array, stringType);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_INVALID_CARDINALITY")).IsTrue();
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_INVALID_CARDINALITY").ModelPath)
            .IsEqualTo(ModelPath.ForType(array.Id));
    }

    // -------------------------------------------------------------------------
    // 6. Invalid annotation key format
    // -------------------------------------------------------------------------

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

        TypeSchemaModel model = BuildModel(obj);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_INVALID_ANNOTATION_KEY")).IsTrue();
        _ = await Assert.That(diagnostics.First(d => d.Code == "MODEL_INVALID_ANNOTATION_KEY").Severity)
            .IsEqualTo(SchemaDiagnosticSeverity.Warning);
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
                    new Annotation { Key = new AnnotationKey("ui.order"), Value = 1, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared },
                    new Annotation { Key = new AnnotationKey("efCore.tableName"), Value = "t_good", Scope = AnnotationScope.Type, Source = AnnotationSource.Declared },
                    new Annotation { Key = new AnnotationKey("jsonSchema.$defs"), Value = true, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared },
                ],
            },
            Properties = [],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(obj);
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(model);

        _ = await Assert.That(diagnostics.Any(d => d.Code == "MODEL_INVALID_ANNOTATION_KEY")).IsFalse();
    }

    // -------------------------------------------------------------------------
    // ModelPath helper tests
    // -------------------------------------------------------------------------

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
        _ = await Assert.That(ModelPath.ForComputedMember(typeId, "FullName")).IsEqualTo("/types/Customer/computed/FullName");
        _ = await Assert.That(ModelPath.ForAnnotation("/types/Customer", annotationKey)).IsEqualTo("/types/Customer/annotations/ui.order");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        var byId = types
            .GroupBy(t => t.Id)
            .ToDictionary(g => g.Key, g => g.First());

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("Test"),
            Types = types,
            TypesById = byId,
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string name)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(name),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = ScalarKind.String,
        };
    }
}
#pragma warning restore CS1591
