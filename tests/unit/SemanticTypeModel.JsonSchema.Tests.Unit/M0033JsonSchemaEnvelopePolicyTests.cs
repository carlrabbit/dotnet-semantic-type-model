using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Domain;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable IDE0305
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0033JsonSchemaEnvelopePolicyTests
{
    [Test]
    public async Task DeriveJsonSchemaModel_should_default_envelope_payload_to_structured_ref()
    {
        SemanticDerivationResult<JsonSchemaSemanticModel> result = BuildModel().DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
        var root = (JsonSchemaObjectNode)result.Model.Root;
        JsonSchemaProperty payload = root.Properties.Single(static property => property.Name == "Specification");

        _ = await Assert.That(payload.Schema.Reference).IsEqualTo("WorkflowSpecification");
        _ = await Assert.That(result.Model.Definitions.ContainsKey("WorkflowSpecification")).IsTrue();
    }

    [Test]
    public async Task DeriveJsonSchemaModel_should_support_payload_as_root_inline_document_serialized_and_ambiguity_policies()
    {
        SemanticDerivationResult<JsonSchemaSemanticModel> payloadRoot = BuildModel().DeriveJsonSchemaModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Envelopes.For<ManagedSpecificationEnvelope>().UsePayloadAsRoot(x => x.Specification);
        });
        _ = await Assert.That(payloadRoot.Model.Root.Name).IsEqualTo("WorkflowSpecification");

        SemanticDerivationResult<JsonSchemaSemanticModel> inline = BuildModel().DeriveJsonSchemaModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsRoot().Payload(x => x.Specification).RepresentInline();
        });
        JsonSchemaProperty inlinePayload = ((JsonSchemaObjectNode)inline.Model.Root).Properties.Single(static property => property.Name == "Specification");
        _ = await Assert.That(inlinePayload.Schema.Inline).IsNotNull();

        SemanticDerivationResult<JsonSchemaSemanticModel> serialized = BuildModel().DeriveJsonSchemaModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsRoot().Payload(x => x.Specification).RepresentAsSerializedJsonString();
        });
        JsonSchemaProperty serializedPayload = ((JsonSchemaObjectNode)serialized.Model.Root).Properties.Single(static property => property.Name == "Specification");
        _ = await Assert.That(((JsonSchemaScalarNode)serializedPayload.Schema.Inline!).Type).IsEqualTo("string");

        SemanticDerivationResult<JsonSchemaSemanticModel> document = BuildModel().DeriveJsonSchemaModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsRoot().Payload(x => x.Specification).RepresentAsJsonDocument();
        });
        JsonSchemaProperty documentPayload = ((JsonSchemaObjectNode)document.Model.Root).Properties.Single(static property => property.Name == "Specification");
        _ = await Assert.That(((JsonSchemaScalarNode)documentPayload.Schema.Inline!).Type).IsEqualTo("object");

        SemanticDerivationResult<JsonSchemaSemanticModel> ambiguous = BuildModel().DeriveJsonSchemaModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeAsRoot().UsePayloadAsRoot(x => x.Specification);
        });
        _ = await Assert.That(ambiguous.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_ENVELOPE_ROOT_AMBIGUOUS")).IsTrue();
    }

    private static TypeSchemaModel BuildModel()
    {
        ScalarTypeDefinition guid = Scalar("Guid", ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar("String", ScalarKind.String);
        ScalarTypeDefinition integer = Scalar("Int64", ScalarKind.Integer);
        ObjectTypeDefinition payload = Object("WorkflowSpecification", [Property("Name", text.Id, true, false)]);
        ObjectTypeDefinition envelope = Object(
            "ManagedSpecificationEnvelope",
            [
                Property("Id", guid.Id, true, false, Annotation(("schema.key", true))),
                Property("Revision", integer.Id, true, false, Annotation(("schema.envelope.metadata", true))),
                Property("ModifiedBy", text.Id, true, false, Annotation(("schema.envelope.metadata", true))),
                Property("Specification", payload.Id, true, false, Annotation(("schema.envelope.payload", true))),
            ],
            Annotation(("schema.envelope", true)),
            new EntitySemantics { Role = EntityRole.Entity });
        return Model(guid, text, integer, payload, envelope);
    }

    private static TypeSchemaModel Model(params TypeDefinition[] types)
    {
        return new() { Id = new SchemaModelId("ManagedSpecificationEnvelope"), Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = EmptyAnnotations };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind kind)
    {
        return new() { Id = new TypeId(name), Name = name, Kind = TypeKind.Scalar, ScalarKind = kind, Nullability = Nullability.NonNullable, Annotations = EmptyAnnotations };
    }

    private static ObjectTypeDefinition Object(string name, IReadOnlyList<PropertyDefinition> properties, AnnotationBag? annotations = null, EntitySemantics? semantics = null)
    {
        return new() { Id = new TypeId(name), Name = name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Properties = properties, Keys = [], Relationships = [], Annotations = annotations ?? EmptyAnnotations, Semantics = semantics ?? new EntitySemantics { Role = EntityRole.ValueObject, IsValueObject = true } };
    }

    private static PropertyDefinition Property(string name, TypeId type, bool required, bool nullable, AnnotationBag? annotations = null)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(type), Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = annotations ?? EmptyAnnotations };
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
