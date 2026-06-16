using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.PowerBI.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable IDE0305
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0033PowerBiEnvelopePolicyTests
{
    [Test]
    public async Task DerivePowerBiModel_should_project_envelope_metadata_and_ignore_payload_by_default()
    {
        PowerBiSemanticModel model = BuildModel().DerivePowerBiModel().Model;
        PowerBiTableDefinition table = model.Tables.Single(static table => table.Name == "ManagedSpecificationEnvelope");
        string[] columns = [.. table.Columns.Select(static column => column.Name).Order(StringComparer.Ordinal)];

        _ = await Assert.That(columns).Contains("Revision");
        _ = await Assert.That(columns).DoesNotContain("Specification");
    }

    [Test]
    public async Task DerivePowerBiModel_should_support_payload_summary_policy()
    {
        PowerBiSemanticModel model = BuildModel().DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Envelopes.For<ManagedSpecificationEnvelope>().UseEnvelopeMetadataTable().SummarizePayload(x => x.Specification, "SpecificationSummary");
        }).Model;

        _ = await Assert.That(model.Tables.Single(static table => table.Name == "ManagedSpecificationEnvelope").Columns.Any(static column => column.Name == "SpecificationSummary")).IsTrue();
    }

    private static TypeSchemaModel BuildModel()
    {
        ScalarTypeDefinition guid = Scalar("Guid", ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar("String", ScalarKind.String);
        ScalarTypeDefinition integer = Scalar("Int64", ScalarKind.Integer);
        ObjectTypeDefinition payload = Object("WorkflowSpecification", [Property("Name", text.Id, true, false)]);
        ObjectTypeDefinition envelope = Object("ManagedSpecificationEnvelope", [Property("Id", guid.Id, true, false), Property("Revision", integer.Id, true, false, Annotation(("schema.envelope.metadata", true))), Property("ModifiedBy", text.Id, true, false, Annotation(("schema.envelope.metadata", true))), Property("Specification", payload.Id, true, false, Annotation(("schema.envelope.payload", true)))], Annotation(("schema.envelope", true)), new EntitySemantics { Role = EntityRole.Entity });
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
