using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Authoring;
#pragma warning disable CS1591, CA1707, IDE0055, IDE0058

namespace SemanticTypeModel.Core.Tests.Unit;

public sealed class M0080ProgrammaticAuthoringTests
{
    [Test]
    public async Task Builds_forward_reference_and_preserves_declaration_order()
    {
        ScalarTypeDefinition text = Scalar("Text", ScalarKind.String);
        ObjectTypeDefinition customer = Object("Customer", [new PropertyDefinition
        {
            Id = new("Name"), Name = "Name", Type = new TypeRef(text.Id), Cardinality = new() { IsRequired = true }, Constraints = new(), Annotations = Empty,
        }]);

        TypeSchemaModelAuthoringResult result = new TypeSchemaModelAuthoringBuilder(new SchemaModelId("m0080"))
            .AddType(customer)
            .AddType(text)
            .Build();

        _ = await Assert.That(result.Succeeded).IsTrue();
        _ = await Assert.That(result.Model!.Types.Select(type => type.Id)).IsEquivalentTo([customer.Id, text.Id]);
        _ = await Assert.That(result.Model.TypesById[text.Id]).IsEqualTo(text);
    }

    [Test]
    public async Task Duplicate_type_ids_fail_without_exposing_a_model()
    {
        TypeSchemaModelAuthoringResult result = new TypeSchemaModelAuthoringBuilder(new SchemaModelId("m0080"))
            .AddType(Scalar("Text", ScalarKind.String))
            .AddType(Scalar("Text", ScalarKind.String))
            .Build();

        _ = await Assert.That(result.Succeeded).IsFalse();
        _ = await Assert.That(result.Model).IsNull();
        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "STM0001")).IsTrue();
    }

    [Test]
    public async Task Earlier_snapshot_isolated_from_later_builder_activity()
    {
        var builder = new TypeSchemaModelAuthoringBuilder(new SchemaModelId("m0080"));
        builder.AddType(Scalar("Text", ScalarKind.String));
        TypeSchemaModel first = builder.Build().Model!;
        builder.AddType(Scalar("Number", ScalarKind.Integer));

        _ = await Assert.That(first.Types).Count().IsEqualTo(1);
        _ = await Assert.That(builder.Build().Model!.Types).Count().IsEqualTo(2);
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind)
    {
        return new()
        {
            Id = new(id),
            Name = id,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = Empty,
            ScalarKind = kind,
        };
    }

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties)
    {
        return new()
        {
            Id = new(id),
            Name = id,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Empty,
            Properties = properties,
            Keys = [],
        };
    }

    private static AnnotationBag Empty => new();
}
