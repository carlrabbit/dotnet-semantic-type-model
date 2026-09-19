using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Authoring;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.PowerBI.Tests.Unit;

#pragma warning disable CS1591, CA1707, IDE0055, CA1861, IDE0300
public sealed class M0080ProgrammaticAuthoringProjectionTests
{
    [Test]
    public async Task Programmatic_dimension_and_fact_use_the_existing_power_bi_entry_point()
    {
        ScalarTypeDefinition integer = Scalar("Integer", ScalarKind.Integer);
        ObjectTypeDefinition dimension = Entity("DimCustomer", EntityRole.Dimension, integer, "CustomerKey");
        ObjectTypeDefinition fact = Entity("FactSales", EntityRole.Fact, integer, "SalesKey");
        var builder = new TypeSchemaModelAuthoringBuilder(new SchemaModelId("m0080"));
        TypeSchemaModel model = builder.AddType(fact).AddType(dimension).AddType(integer).Build().Model!;

        SemanticDerivationResult<PowerBiSemanticModel> result = model.DerivePowerBiModel(options => options.UseDefaultTransformations());
        _ = await Assert.That(result.Model.Tables.Select(table => table.Name)).IsEquivalentTo(new[] { "DimCustomer", "FactSales" });
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind)
    {
        return new() { Id = new(id), Name = id, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = kind };
}
    private static ObjectTypeDefinition Entity(string id, EntityRole role, ScalarTypeDefinition scalar, string propertyId)
    {
        return new()
    {
        Id = new(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Semantics = new EntitySemantics { Role = role }, Properties = [new PropertyDefinition { Id = new(propertyId), Name = propertyId, Type = new(scalar.Id), Cardinality = new() { IsRequired = true }, Constraints = new(), Annotations = new() }], Keys = [new KeyDefinition { Name = "Primary", Kind = KeyKind.Primary, Properties = [new PropertyRef(new PropertyId(propertyId))], Annotations = new() }],
    };
}}
