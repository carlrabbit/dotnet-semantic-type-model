using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Import;
using Legacy = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore.Tests.Unit;

// CS1591 and IDE0305 are disabled in this test fixture to avoid repetitive XML docs and collection-style
// churn while keeping test intent concise and consistent with existing repository test patterns.
#pragma warning disable CS1591
#pragma warning disable IDE0305
/// <summary>
/// Verifies M0008 EF Core-like projection fixture behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class EfCoreModelProjectionTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Fixture_1_simple_entity_should_project_properties_key_requiredness_and_table_annotations()
    {
        ScalarTypeDefinition intType = Scalar("Int64", ScalarKind.Integer);
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var entity = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            DisplayName = "Customer Record",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("efCore.tableName", "customers"), ("efCore.schemaName", "crm")),
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("id", "CustomerId", intType.Id, true, false, annotations: Annotation(("efCore.valueGenerated", "OnAdd"))),
                Property("name", "CustomerName", stringType.Id, true, false, stringConstraints: new StringConstraints { MaxLength = 200 }, annotations: Annotation(("efCore.columnName", "customer_name"))),
                Property("nickname", "CustomerNickname", stringType.Id, false, true),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_Customer",
                    Kind = KeyKind.Primary,
                    IsGenerated = true,
                    Properties = [new PropertyRef(new PropertyId("CustomerId"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        EfModelDefinition projection = Project(BuildModel(entity, intType, stringType));
        EfEntityTypeDefinition projectedEntity = projection.EntityTypes.Single();
        EfPropertyDefinition nameProperty = projectedEntity.Properties.Single(static property => property.Name == "customer_name");

        _ = await Assert.That(projectedEntity.TableName).IsEqualTo("customers");
        _ = await Assert.That(projectedEntity.SchemaName).IsEqualTo("crm");
        _ = await Assert.That(projectedEntity.Keys.Single().IsGenerated).IsTrue();
        _ = await Assert.That(projectedEntity.Properties.Single(static property => property.Name == "id").IsGenerated).IsTrue();
        _ = await Assert.That(nameProperty.MaxLength).IsEqualTo(200);
        _ = await Assert.That(nameProperty.ClrType).IsEqualTo(typeof(string));
        _ = await Assert.That(projectedEntity.Properties.Single(static property => property.Name == "nickname").IsNullable).IsTrue();
    }

    [Test]
    public async Task Fixture_2_alternate_key_and_relationship_should_project_deterministically()
    {
        ScalarTypeDefinition intType = Scalar("Int64", ScalarKind.Integer);
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("id", "CustomerId", intType.Id, true, false),
                Property("code", "CustomerCode", stringType.Id, true, false),
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
                new KeyDefinition
                {
                    Name = "AK_Customer_Code",
                    Kind = KeyKind.Natural,
                    Properties = [new PropertyRef(new PropertyId("CustomerCode"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        var order = new ObjectTypeDefinition
        {
            Id = new TypeId("Order"),
            Name = "Order",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("id", "OrderId", intType.Id, true, false),
                Property("customerId", "OrderCustomerId", intType.Id, true, false),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_Order",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("OrderId"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships =
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("Order_Customer"),
                    PrincipalType = new TypeRef(customer.Id),
                    DependentType = new TypeRef(new TypeId("Order")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("CustomerId"))],
                    DependentProperties = [new PropertyRef(new PropertyId("OrderCustomerId"))],
                    Cardinality = RelationshipCardinality.ManyToOne,
                    DeleteBehavior = DeleteBehaviorSemantics.Cascade,
                    Annotations = EmptyAnnotations,
                },
            ],
        };

        EfModelDefinition projection = Project(BuildModel(customer, order, intType, stringType));
        EfEntityTypeDefinition projectedCustomer = projection.EntityTypes.Single(static entity => entity.Name == "Customer");
        EfEntityTypeDefinition projectedOrder = projection.EntityTypes.Single(static entity => entity.Name == "Order");

        _ = await Assert.That(projectedCustomer.Keys.Count).IsEqualTo(2);
        _ = await Assert.That(projectedCustomer.Keys.Single(static key => key.Name == "AK_Customer_Code").Kind).IsEqualTo(EfKeyKind.Alternate);
        _ = await Assert.That(projectedOrder.Relationships.Single().PrincipalEntity).IsEqualTo("Customer");
        _ = await Assert.That(projectedOrder.Relationships.Single().Cardinality).IsEqualTo(EfRelationshipCardinality.ManyToOne);
        _ = await Assert.That(projectedOrder.Relationships.Single().DeleteBehavior).IsEqualTo(EfDeleteBehavior.Cascade);
    }

    [Test]
    public async Task Fixture_3_value_object_should_diagnose_owned_flatten_and_serialize_modes()
    {
        ScalarTypeDefinition stringType = Scalar("String", ScalarKind.String);
        var address = new ObjectTypeDefinition
        {
            Id = new TypeId("Address"),
            Name = "Address",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("efCore.owned", true)),
            Semantics = new EntitySemantics { Role = EntityRole.ValueObject, IsValueObject = true },
            Properties =
            [
                Property("line1", "AddressLine1", stringType.Id, true, false),
                Property("city", "AddressCity", stringType.Id, true, false),
            ],
            Keys = [],
            Relationships = [],
        };

        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties = [Property("address", "CustomerAddress", address.Id, true, false)],
            Keys = [],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(customer, address, stringType);

        EfModelDefinition diagnosed = Project(model);
        EfModelDefinition owned = Project(model, new EfCoreProjectionOptions { ValueObjectProjectionMode = ValueObjectEfProjectionMode.Owned });
        EfModelDefinition flattened = Project(model, new EfCoreProjectionOptions { ValueObjectProjectionMode = ValueObjectEfProjectionMode.Flatten });
        EfModelDefinition serialized = Project(model, new EfCoreProjectionOptions { ValueObjectProjectionMode = ValueObjectEfProjectionMode.SerializeJson });

        _ = await Assert.That(diagnosed.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_VALUE_OBJECT_REQUIRES_MODE")).IsTrue();
        _ = await Assert.That(owned.EntityTypes.Any(static entity => entity.IsOwned)).IsTrue();
        _ = await Assert.That(flattened.EntityTypes.Single().Properties.Any(static property => property.Name == "address_line1")).IsTrue();
        _ = await Assert.That(serialized.EntityTypes.Single().Properties.Single().Conversion).IsEqualTo("Json");
    }

    [Test]
    public async Task Fixture_4_enum_storage_should_be_deterministic_and_invalid_values_diagnosed()
    {
        var enumType = new EnumTypeDefinition
        {
            Id = new TypeId("OrderStatus"),
            Name = "OrderStatus",
            Kind = TypeKind.Enum,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            StorageKind = EnumStorageKind.String,
            Values =
            [
                new EnumValueDefinition { Name = "Open", Value = "open", Annotations = EmptyAnnotations },
                new EnumValueDefinition { Name = "Closed", Value = "closed", Annotations = EmptyAnnotations },
            ],
        };

        EnumTypeDefinition numericEnumType = enumType with { Id = new TypeId("NumericStatus"), Name = "NumericStatus", StorageKind = EnumStorageKind.Integer };
        var entity = new ObjectTypeDefinition
        {
            Id = new TypeId("Order"),
            Name = "Order",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("status", "OrderStatusProperty", enumType.Id, true, false),
                Property("numericStatus", "NumericStatusProperty", numericEnumType.Id, true, false, annotations: Annotation(("efCore.enumStorage", "Numeric"))),
                Property("badStatus", "BadStatusProperty", enumType.Id, true, false, annotations: Annotation(("efCore.enumStorage", "BadValue"))),
            ],
            Keys = [],
            Relationships = [],
        };

        EfModelDefinition defaultProjection = Project(BuildModel(entity, enumType, numericEnumType));
        EfModelDefinition numericProjection = Project(BuildModel(entity, enumType, numericEnumType), new EfCoreProjectionOptions { EnumProjectionMode = EnumEfProjectionMode.Numeric });

        _ = await Assert.That(defaultProjection.EntityTypes.Single().Properties.Single(static property => property.Name == "status").ClrType).IsEqualTo(typeof(string));
        _ = await Assert.That(numericProjection.EntityTypes.Single().Properties.Single(static property => property.Name == "numericStatus").ClrType).IsEqualTo(typeof(long));
        _ = await Assert.That(defaultProjection.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_INVALID_ANNOTATION_VALUE")).IsTrue();
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
            Annotations = EmptyAnnotations,
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
        EfModelDefinition diagnosed = Project(model);
        EfModelDefinition serialized = Project(model, new EfCoreProjectionOptions { UnsupportedShapeBehavior = UnsupportedEfShapeBehavior.SerializeJson });

        _ = await Assert.That(diagnosed.Diagnostics.Count(static diagnostic => diagnostic.Code.EndsWith("_UNSUPPORTED", StringComparison.Ordinal))).IsEqualTo(3);
        _ = await Assert.That(serialized.EntityTypes.Single().Properties.Count).IsEqualTo(3);
        _ = await Assert.That(serialized.EntityTypes.Single().Properties.All(static property => property.Conversion == "Json")).IsTrue();
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
            Annotations = EmptyAnnotations,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("first", "First", stringType.Id, true, false, annotations: Annotation(("efCore.columnName", "Duplicate"))),
                Property("second", "Second", stringType.Id, true, false, annotations: Annotation(("efCore.columnName", "Duplicate"))),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "DuplicateKey",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("First"))],
                    Annotations = EmptyAnnotations,
                },
                new KeyDefinition
                {
                    Name = "DuplicateKey",
                    Kind = KeyKind.Alternate,
                    Properties = [new PropertyRef(new PropertyId("Second"))],
                    Annotations = EmptyAnnotations,
                },
            ],
            Relationships = [],
        };

        TypeSchemaModel model = BuildModel(entity, stringType);
        EfModelDefinition diagnosed = Project(model);
        EfModelDefinition suffixed = Project(model, new EfCoreProjectionOptions { NameCollisionBehavior = NameCollisionBehavior.Suffix });

        _ = await Assert.That(diagnosed.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_DUPLICATE_PROJECTED_NAME")).IsTrue();
        _ = await Assert.That(suffixed.EntityTypes.Single().Properties.Count(static property => property.Name.StartsWith("Duplicate", StringComparison.Ordinal))).IsEqualTo(2);
        _ = await Assert.That(suffixed.EntityTypes.Single().Keys.Count(static key => key.Name.StartsWith("DuplicateKey", StringComparison.Ordinal))).IsEqualTo(2);
    }

    [Test]
    public async Task Fixture_7_optional_vs_nullable_json_schema_properties_should_survive_projection_with_annotations_and_diagnostics()
    {
        TypeSchemaModel model = ImportHardeningModel(/*lang=json,strict*/ """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$id": "Contact",
              "type": "object",
              "required": ["requiredNullable"],
              "properties": {
                "requiredNullable": { "type": ["string", "null"] },
                "optionalNonNullable": { "type": "string" },
                "optionalNullable": { "type": ["string", "null"] }
              }
            }
            """);

        EfModelDefinition projection = Project(model, new EfCoreProjectionOptions { ProjectUnannotatedObjectsAsEntities = true });
        EfEntityTypeDefinition entity = projection.EntityTypes.Single();
        EfPropertyDefinition requiredNullable = entity.Properties.Single(static property => property.Name == "requiredNullable");
        EfPropertyDefinition optionalNonNullable = entity.Properties.Single(static property => property.Name == "optionalNonNullable");
        EfPropertyDefinition optionalNullable = entity.Properties.Single(static property => property.Name == "optionalNullable");

        _ = await Assert.That(requiredNullable.IsRequired).IsTrue();
        _ = await Assert.That(requiredNullable.IsNullable).IsTrue();
        _ = await Assert.That(optionalNonNullable.IsRequired).IsFalse();
        _ = await Assert.That(optionalNonNullable.IsNullable).IsFalse();
        _ = await Assert.That(optionalNullable.Annotations.Items.Any(static annotation => annotation.Key.Value == "schema.isOptional")).IsTrue();
        _ = await Assert.That(projection.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_OPTIONALITY_PRESERVED_AS_ANNOTATION")).IsTrue();
    }

    private static EfModelDefinition Project(TypeSchemaModel model, EfCoreProjectionOptions? options = null)
    {
        var projection = new EfCoreModelProjection(options);
        var context = new SchemaProjectionContext { Target = ProjectionTarget.EfCore };
        return projection.Project(model, context);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        Dictionary<TypeId, TypeDefinition> byId = types.ToDictionary(static type => type.Id, static type => type);
        return new TypeSchemaModel
        {
            Id = new SchemaModelId("EfModel"),
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
        StringConstraints? stringConstraints = null,
        NumericConstraints? numericConstraints = null,
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
            Constraints = new ConstraintSet
            {
                String = stringConstraints,
                Numeric = numericConstraints,
            },
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

    private static TypeSchemaModel ImportHardeningModel(string json)
    {
        JsonSchemaImportResult imported = JsonSchemaImporter.Import(json);
        Legacy.ObjectShape root = imported.Model.Root as Legacy.ObjectShape
            ?? throw new InvalidOperationException("The EF projection test bridge expects an object root.");
        var rootId = imported.Model.RootIdentifier ?? "Root";
        List<TypeDefinition> types = [];

        foreach (Legacy.PropertyShape property in root.Properties)
        {
            if (property.Type?.Resolve(imported.Model) is not Legacy.ScalarShape scalarShape)
            {
                throw new InvalidOperationException("The EF projection test bridge expects scalar properties.");
            }

            var scalarId = new TypeId($"{rootId}_{property.Name}_Scalar");
            types.Add(new ScalarTypeDefinition
            {
                Id = scalarId,
                Name = scalarId.Value,
                Kind = TypeKind.Scalar,
                Nullability = scalarShape.IsNullable ? Nullability.Nullable : Nullability.NonNullable,
                Annotations = new AnnotationBag(),
                ScalarKind = ToHardeningScalarKind(scalarShape.Kind),
            });
        }

        types.Add(new ObjectTypeDefinition
        {
            Id = new TypeId(rootId),
            Name = rootId,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = ToHardeningAnnotations(root.Annotations, AnnotationScope.Type),
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                .. root.Properties.Select(property => new PropertyDefinition
                {
                    Id = new PropertyId($"{rootId}_{property.Name}"),
                    Name = property.Name,
                    Type = new TypeRef(new TypeId($"{rootId}_{property.Name}_Scalar")),
                    Cardinality = new Cardinality
                    {
                        IsRequired = property.IsRequired,
                        AllowsNull = property.IsNullable,
                    },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = ToHardeningAnnotations(property.Annotations, AnnotationScope.Member),
                }),
            ],
            Keys = [],
            Relationships = [],
        });

        return new TypeSchemaModel
        {
            Id = new SchemaModelId(rootId),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id, static type => type),
            Annotations = new AnnotationBag(),
        };
    }

    private static AnnotationBag ToHardeningAnnotations(IReadOnlyList<Legacy.SchemaAnnotation> annotations, AnnotationScope scope)
    {
        return new AnnotationBag
        {
            Items =
            [
                .. annotations.Select(annotation => new Annotation
                {
                    Key = new AnnotationKey(annotation.Key.Replace(':', '.')),
                    Value = TryParseJsonLiteral(annotation.Value),
                    Scope = scope,
                    Source = AnnotationSource.Imported,
                }),
            ],
        };
    }

    private static object? TryParseJsonLiteral(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.String => document.RootElement.GetString(),
                JsonValueKind.Number when document.RootElement.TryGetInt64(out var number) => number,
                JsonValueKind.Number => document.RootElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => value,
                JsonValueKind.Array => value,
                JsonValueKind.Undefined => value,
                _ => value,
            };
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static ScalarKind ToHardeningScalarKind(Legacy.ScalarKind scalarKind)
    {
        return scalarKind switch
        {
            Legacy.ScalarKind.Boolean => ScalarKind.Boolean,
            Legacy.ScalarKind.Integer => ScalarKind.Integer,
            Legacy.ScalarKind.Number => ScalarKind.Number,
            Legacy.ScalarKind.String => ScalarKind.String,
            Legacy.ScalarKind.Null => ScalarKind.String,
            _ => ScalarKind.String,
        };
    }
}
#pragma warning restore IDE0305
#pragma warning restore CS1591
