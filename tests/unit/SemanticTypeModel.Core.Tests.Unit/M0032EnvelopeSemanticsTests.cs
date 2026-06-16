using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Inspection;
using SemanticTypeModel.Core.Query;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0032EnvelopeSemanticsTests
{
    [Test]
    public async Task Core_defaults_should_validate_and_inspect_envelope_semantics()
    {
        ScalarTypeDefinition text = Scalar("String");
        ObjectTypeDefinition payload = Object("OrderSubmitted", []);
        ObjectTypeDefinition envelope = Object(
            "MessageEnvelope",
            [
                Property("CorrelationId", text.Id, Annotations(Annotation("schema.envelope.metadata", "true"))),
                Property("Payload", payload.Id, Annotations(Annotation("schema.envelope.payload", "true"))),
            ],
            Annotations(Annotation("schema.envelope", "true"), Annotation("schema.envelope.purpose", "transport")));

        SemanticModelTransformationResult result = BuildModel(text, payload, envelope).Transform(pipeline => pipeline.UseCoreDefaults());
        ObjectTypeDefinition transformed = result.Model.Envelopes().Single();

        _ = await Assert.That(result.Diagnostics).IsEmpty();
        _ = await Assert.That(transformed.EnvelopePayloads().Single().Name).IsEqualTo("Payload");
        _ = await Assert.That(transformed.EnvelopeMetadata().Single().Name).IsEqualTo("CorrelationId");
        _ = await Assert.That(result.Model.ToSemanticText()).Contains("MessageEnvelope (Object) [Envelope]");
        _ = await Assert.That(result.Model.ToSemanticText()).Contains("Property Payload: OrderSubmitted optional envelopePayload");
        _ = await Assert.That(result.Model.ToSemanticText()).Contains("Property CorrelationId: String optional envelopeMetadata");
    }

    [Test]
    public async Task Core_defaults_should_diagnose_missing_duplicate_and_misplaced_envelope_members()
    {
        ScalarTypeDefinition text = Scalar("String");
        ObjectTypeDefinition missingPayload = Object("MissingPayload", [], Annotations(Annotation("schema.envelope", "true")));
        ObjectTypeDefinition duplicatePayload = Object(
            "DuplicatePayload",
            [
                Property("First", text.Id, Annotations(Annotation("schema.envelope.payload", "true"))),
                Property("Second", text.Id, Annotations(Annotation("schema.envelope.payload", "true"))),
            ],
            Annotations(Annotation("schema.envelope", "true")));
        ObjectTypeDefinition misplaced = Object(
            "Misplaced",
            [
                Property("Payload", text.Id, Annotations(Annotation("schema.envelope.payload", "true"))),
                Property("CorrelationId", text.Id, Annotations(Annotation("schema.envelope.metadata", "true"))),
            ]);
        ObjectTypeDefinition missingType = Object(
            "MissingPayloadType",
            [Property("Payload", new TypeId("NotInModel"), Annotations(Annotation("schema.envelope.payload", "true")))],
            Annotations(Annotation("schema.envelope", "true")));

        SemanticModelTransformationResult result = BuildModel(text, missingPayload, duplicatePayload, misplaced, missingType)
            .Transform(pipeline => pipeline.UseCoreDefaults(), new SchemaPipelineOptions { ContinueOnError = true });

        string[] codes = [.. result.Diagnostics.Select(static diagnostic => diagnostic.Code).OrderBy(static code => code, StringComparer.Ordinal)];
        _ = await Assert.That(codes).Contains("STM1008");
        _ = await Assert.That(codes).Contains("STM1009");
        _ = await Assert.That(codes).Contains("STM1010");
        _ = await Assert.That(codes).Contains("STM1011");
        _ = await Assert.That(codes).Contains("STM1012");
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new TypeSchemaModel
        {
            Id = new SchemaModelId("TestModel"),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id, static type => type),
            Annotations = new AnnotationBag(),
        };
    }

    private static ScalarTypeDefinition Scalar(string id)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(id),
            Name = id,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            ScalarKind = ScalarKind.String,
        };
    }

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties, AnnotationBag? annotations = null)
    {
        return new ObjectTypeDefinition
        {
            Id = new TypeId(id),
            Name = id,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = annotations ?? new AnnotationBag(),
            Properties = properties,
            Keys = [],
            Relationships = [],
        };
    }

    private static PropertyDefinition Property(string name, TypeId typeId, AnnotationBag? annotations = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(name),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality(),
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = annotations ?? new AnnotationBag(),
        };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new AnnotationBag { Items = annotations };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = AnnotationScope.Type,
            Source = AnnotationSource.Declared,
        };
    }
}
#pragma warning restore CS1591
