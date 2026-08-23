using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0068StrongScalarSystemTextJsonTests
{
    [Test]
    public async Task Options_configuration_round_trips_a_Strong_Scalar_as_its_underlying_scalar()
    {
        TypeSchemaModel model = BuildModel();
        var options = new JsonSerializerOptions();
        _ = options.AddSemanticTypeModelJson(model);
        var expected = Guid.NewGuid();

        var json = JsonSerializer.Serialize(new StrongScalarContainer { SpecificationVersionId = new SpecificationVersionId(expected) }, options);
        StrongScalarContainer roundTripped = JsonSerializer.Deserialize<StrongScalarContainer>(json, options)!;

        _ = await Assert.That(json).Contains(expected.ToString("D"));
        _ = await Assert.That(json).DoesNotContain("Value");
        _ = await Assert.That(roundTripped.SpecificationVersionId.Value).IsEqualTo(expected);
    }

    private static TypeSchemaModel BuildModel()
    {
        var scalar = new ScalarTypeDefinition { Id = new TypeId("global::System.Guid"), Name = "Guid", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = ScalarKind.Guid, Annotations = new AnnotationBag() };
        var strong = new StrongScalarTypeDefinition { Id = new TypeId("global::SemanticTypeModel.SystemTextJson.Tests.Unit.SpecificationVersionId"), Name = "SpecificationVersionId", Kind = TypeKind.StrongScalar, Nullability = Nullability.NonNullable, ValueType = new TypeRef(scalar.Id), Annotations = new AnnotationBag() };
        var container = new ObjectTypeDefinition
        {
            Id = new TypeId("global::SemanticTypeModel.SystemTextJson.Tests.Unit.StrongScalarContainer"),
            Name = "StrongScalarContainer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Keys = [],
            Properties = [new PropertyDefinition
            {
                Id = new PropertyId("container.SpecificationVersionId"), Name = "SpecificationVersionId", Type = new TypeRef(strong.Id),
                Cardinality = new Cardinality { IsRequired = true, AllowsNull = false }, Constraints = new ConstraintSet(), Annotations = new AnnotationBag(),
            }],
        };
        TypeDefinition[] types = [scalar, strong, container];
        return new TypeSchemaModel { Id = new SchemaModelId(container.Id.Value), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new AnnotationBag() };
    }
}

public sealed class StrongScalarContainer
{
    public SpecificationVersionId SpecificationVersionId { get; set; }
}

public readonly record struct SpecificationVersionId(Guid Value);
#pragma warning restore CA1707
#pragma warning restore CS1591
