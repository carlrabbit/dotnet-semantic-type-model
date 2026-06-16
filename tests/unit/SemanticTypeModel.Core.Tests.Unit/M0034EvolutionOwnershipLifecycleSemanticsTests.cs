using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Inspection;
using SemanticTypeModel.Core.Query;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0034EvolutionOwnershipLifecycleSemanticsTests
{
    [Test]
    public async Task Core_defaults_should_validate_query_and_inspect_m0034_semantics()
    {
        ScalarTypeDefinition text = Scalar("String", ScalarKind.String);
        ScalarTypeDefinition boolean = Scalar("Boolean", ScalarKind.Boolean);
        ScalarTypeDefinition instant = Scalar("DateTimeOffset", ScalarKind.DateTimeOffset);
        DictionaryTypeDefinition extensionBag = Dictionary("ExtensionBag", text.Id, text.Id);
        ObjectTypeDefinition address = Object("Address", [Property("Street", text.Id)]);
        ObjectTypeDefinition customer = Object(
            "Customer",
            [
                Property("Address", address.Id, annotations: Annotations(Annotation("schema.ownership", "true"), Annotation("schema.ownedObject", "true"))),
                Property("Version", text.Id, annotations: Annotations(Annotation("schema.version", "true"))),
                Property("IsCurrent", boolean.Id, annotations: Annotations(Annotation("schema.currentVersion", "true"))),
                Property("ValidFrom", instant.Id, annotations: Annotations(Annotation("schema.validFrom", "true"))),
                Property("ValidTo", instant.Id, annotations: Annotations(Annotation("schema.validTo", "true"))),
                Property("State", text.Id, annotations: Annotations(Annotation("schema.lifecycleState", "true"))),
                Property("Extensions", extensionBag.Id, annotations: Annotations(Annotation("schema.extensionData", "true"))),
            ],
            Annotations(Annotation("schema.versioned", "true"), Annotation("schema.temporalValidity", "true")));

        SemanticModelTransformationResult result = BuildModel(text, boolean, instant, extensionBag, address, customer).Transform(pipeline => pipeline.UseCoreDefaults());
        var transformed = (ObjectTypeDefinition)result.Model.GetType(customer.Id);
        var inspection = result.Model.ToSemanticText();

        _ = await Assert.That(result.Diagnostics).IsEmpty();
        _ = await Assert.That(transformed.OwnedMembers().Single().Name).IsEqualTo("Address");
        _ = await Assert.That(transformed.VersionMembers().Select(static property => property.Name)).Contains("Version");
        _ = await Assert.That(transformed.TemporalValidityMembers().Select(static property => property.Name)).Contains("ValidFrom");
        _ = await Assert.That(transformed.LifecycleStateMembers().Single().Name).IsEqualTo("State");
        _ = await Assert.That(transformed.ExtensionDataMembers().Single().Name).IsEqualTo("Extensions");
        _ = await Assert.That(inspection).Contains("Customer (Object) [Versioned] [TemporalValidity]");
        _ = await Assert.That(inspection).Contains("Property Address: Address optional ownedObject");
        _ = await Assert.That(inspection).Contains("Property Extensions: ExtensionBag optional extensionData");
    }

    [Test]
    public async Task Core_defaults_should_diagnose_invalid_m0034_semantics()
    {
        ScalarTypeDefinition text = Scalar("String", ScalarKind.String);
        ObjectTypeDefinition invalid = Object(
            "Invalid",
            [
                Property("Self", new TypeId("Invalid"), annotations: Annotations(Annotation("schema.ownership", "true"), Annotation("schema.ownedObject", "true"))),
                Property("ValidTo", text.Id, annotations: Annotations(Annotation("schema.validTo", "true"))),
                Property("State", new TypeId("Invalid"), annotations: Annotations(Annotation("schema.lifecycleState", "true"))),
                Property("Extensions", text.Id, annotations: Annotations(Annotation("schema.extensionData", "true"))),
            ],
            Annotations(Annotation("schema.versioned", "true"), Annotation("schema.temporalValidity", "true")));

        SemanticModelTransformationResult result = BuildModel(text, invalid).Transform(pipeline => pipeline.UseCoreDefaults(), new SchemaPipelineOptions { ContinueOnError = true });
        string[] codes = [.. result.Diagnostics.Select(static diagnostic => diagnostic.Code).Order(StringComparer.Ordinal)];

        _ = await Assert.That(codes).Contains("STM1014");
        _ = await Assert.That(codes).Contains("STM1016");
        _ = await Assert.That(codes).Contains("STM1017");
        _ = await Assert.That(codes).Contains("STM1018");
        _ = await Assert.That(codes).Contains("STM1019");
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new() { Id = new SchemaModelId("Customer"), Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = new AnnotationBag() };
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = kind };
    }

    private static DictionaryTypeDefinition Dictionary(string id, TypeId keyType, TypeId valueType)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Dictionary, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), KeyType = new TypeRef(keyType), ValueType = new TypeRef(valueType) };
    }

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties, AnnotationBag? annotations = null)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = annotations ?? new AnnotationBag(), Properties = properties, Keys = [], Relationships = [] };
    }

    private static PropertyDefinition Property(string name, TypeId typeId, bool required = false, bool nullable = false, AnnotationBag? annotations = null)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(typeId), Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = annotations ?? new AnnotationBag() };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new() { Items = annotations };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new() { Key = new AnnotationKey(key), Value = value, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared };
    }
}
#pragma warning restore CS1591
