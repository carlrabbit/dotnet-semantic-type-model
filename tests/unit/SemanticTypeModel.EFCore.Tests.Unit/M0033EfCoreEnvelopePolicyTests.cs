using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable IDE0305
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0033EfCoreEnvelopePolicyTests
{
    [Test]
    public async Task DeriveEfCoreModel_should_default_envelope_payload_to_serialized_json()
    {
        EfModelDefinition model = BuildModel().DeriveEfCoreModel().Model.ToDefinition();
        EfEntityTypeDefinition envelope = model.EntityTypes.Single(static entity => entity.Name == "ManagedSpecificationEnvelope");
        EfPropertyDefinition payload = envelope.Properties.Single(static property => property.Name == "Specification");

        _ = await Assert.That(payload.ClrType).IsEqualTo(typeof(string));
        _ = await Assert.That(payload.Conversion).IsEqualTo("Json");
        _ = await Assert.That(model.Diagnostics).DoesNotContain(static diagnostic => diagnostic.Code == "EFCORE_VALUE_OBJECT_REQUIRES_MODE");
    }

    [Test]
    public async Task DeriveEfCoreModel_should_support_serialized_owned_same_table_owned_json_separate_and_ignored_payload_policies()
    {
        EfModelDefinition serialized = BuildModel().DeriveEfCoreModel(options => options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsEntity().Payload(x => x.Specification).StoreAsSerializedJson("SpecificationJson")).Model.ToDefinition();
        _ = await Assert.That(serialized.EntityTypes.Single(static entity => entity.Name == "ManagedSpecificationEnvelope").Properties.Single(static property => property.Name == "Specification").ColumnName).IsEqualTo("SpecificationJson");

        EfModelDefinition sameTable = BuildModel().DeriveEfCoreModel(options => options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsEntity().Payload(x => x.Specification).StoreAsOwnedColumns("Specification")).Model.ToDefinition();
        string[] sameTableColumns = [.. sameTable.EntityTypes.Single(static entity => entity.Name == "ManagedSpecificationEnvelope").Properties.Select(static property => property.Name).Order(StringComparer.Ordinal)];
        _ = await Assert.That(sameTableColumns).Contains("Specification_Name");

        EfModelDefinition ownedJson = BuildModel().DeriveEfCoreModel(options => options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsEntity().Payload(x => x.Specification).StoreAsOwnedJson("Specification")).Model.ToDefinition();
        _ = await Assert.That(ownedJson.EntityTypes.Single(static entity => entity.Name == "ManagedSpecificationEnvelope").Properties.Single(static property => property.Name == "Specification").Conversion).IsEqualTo("OwnedJson");

        EfModelDefinition separate = BuildModel().DeriveEfCoreModel(options => options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsEntity().Payload(x => x.Specification).StoreAsOwnedSeparateTable()).Model.ToDefinition();
        _ = await Assert.That(separate.EntityTypes.Any(static entity => entity.IsOwned && entity.Name == "ManagedSpecificationEnvelope_Specification")).IsTrue();

        EfModelDefinition ignored = BuildModel().DeriveEfCoreModel(options => options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsEntity().Payload(x => x.Specification).IgnorePayload()).Model.ToDefinition();
        _ = await Assert.That(ignored.EntityTypes.Single(static entity => entity.Name == "ManagedSpecificationEnvelope").Properties.Any(static property => property.Name == "Specification")).IsFalse();
    }

    [Test]
    public async Task DeriveEfCoreModel_should_default_value_object_references_to_owned_same_table_and_diagnose_collections()
    {
        EfModelDefinition model = BuildModel(includeValueObjectReference: true, includeCollection: true).DeriveEfCoreModel().Model.ToDefinition();
        EfEntityTypeDefinition envelope = model.EntityTypes.Single(static entity => entity.Name == "ManagedSpecificationEnvelope");

        _ = await Assert.That(envelope.Properties.Any(static property => property.Name == "Audit_Source")).IsTrue();
        _ = await Assert.That(model.Diagnostics.Any(static diagnostic => diagnostic.Code == "EFCORE_ARRAY_UNSUPPORTED")).IsTrue();
    }

    private static TypeSchemaModel BuildModel(bool includeValueObjectReference = false, bool includeCollection = false)
    {
        ScalarTypeDefinition guid = Scalar("Guid", ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar("String", ScalarKind.String);
        ScalarTypeDefinition integer = Scalar("Int64", ScalarKind.Integer);
        ObjectTypeDefinition payload = Object("WorkflowSpecification", [Property("Name", text.Id, true, false)]);
        ObjectTypeDefinition audit = Object("AuditInfo", [Property("Source", text.Id, true, false)]);
        ArrayTypeDefinition tags = new() { Id = new TypeId("TagList"), Name = "TagList", Kind = TypeKind.Array, Nullability = Nullability.NonNullable, ItemType = new TypeRef(audit.Id), Annotations = EmptyAnnotations };
        List<PropertyDefinition> properties =
        [
            Property("Id", guid.Id, true, false, Annotation(("schema.key", true))),
            Property("Revision", integer.Id, true, false, Annotation(("schema.envelope.metadata", true))),
            Property("ModifiedBy", text.Id, true, false, Annotation(("schema.envelope.metadata", true))),
            Property("Specification", payload.Id, true, false, Annotation(("schema.envelope.payload", true))),
        ];
        if (includeValueObjectReference)
        {
            properties.Add(Property("Audit", audit.Id, false, true));
        }
        if (includeCollection)
        {
            properties.Add(Property("Audits", tags.Id, false, true));
        }
        ObjectTypeDefinition envelope = Object("ManagedSpecificationEnvelope", properties, Annotation(("schema.envelope", true)), new EntitySemantics { Role = EntityRole.Entity }, [Key("PK_ManagedSpecificationEnvelope", KeyKind.Primary, "Id")]);
        return Model(includeCollection ? [guid, text, integer, payload, audit, tags, envelope] : [guid, text, integer, payload, audit, envelope]);
    }

    private static TypeSchemaModel Model(params TypeDefinition[] types)
    {
        return new() { Id = new SchemaModelId("ManagedSpecificationEnvelope"), Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = EmptyAnnotations };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind kind)
    {
        return new() { Id = new TypeId(name), Name = name, Kind = TypeKind.Scalar, ScalarKind = kind, Nullability = Nullability.NonNullable, Annotations = EmptyAnnotations };
    }

    private static ObjectTypeDefinition Object(string name, IReadOnlyList<PropertyDefinition> properties, AnnotationBag? annotations = null, EntitySemantics? semantics = null, IReadOnlyList<KeyDefinition>? keys = null)
    {
        return new() { Id = new TypeId(name), Name = name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Properties = properties, Keys = keys ?? [], Relationships = [], Annotations = annotations ?? EmptyAnnotations, Semantics = semantics ?? new EntitySemantics { Role = EntityRole.ValueObject, IsValueObject = true } };
    }

    private static PropertyDefinition Property(string name, TypeId type, bool required, bool nullable, AnnotationBag? annotations = null)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(type), Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = annotations ?? EmptyAnnotations };
    }

    private static KeyDefinition Key(string name, KeyKind kind, params string[] properties)
    {
        return new() { Name = name, Kind = kind, Properties = properties.Select(static property => new PropertyRef(new PropertyId(property))).ToArray(), Annotations = EmptyAnnotations };
    }

    private static AnnotationBag Annotation(params (string Key, object? Value)[] values)
    {
        return new() { Items = values.Select(static value => new Annotation { Key = new AnnotationKey(value.Key), Value = value.Value, Scope = AnnotationScope.Projection, Source = AnnotationSource.Declared }).ToArray() };
    }

    private static readonly AnnotationBag EmptyAnnotations = new();

    private sealed class ManagedSpecificationEnvelope { public WorkflowSpecification Specification { get; init; } = new(); }
    private sealed class WorkflowSpecification { }
}
#pragma warning restore IDE0305
#pragma warning restore CS1591
