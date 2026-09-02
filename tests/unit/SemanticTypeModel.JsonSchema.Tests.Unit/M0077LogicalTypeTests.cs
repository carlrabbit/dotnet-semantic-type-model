using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591, CA1707
public sealed class M0077LogicalTypeTests
{
    [Test]
    public async Task Logical_type_is_optional_metadata_without_changing_scalar_schema()
    {
        var scalar = new ScalarTypeDefinition { Id = new("Guid"), Name = "Guid", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.Guid };
        var property = new PropertyDefinition { Id = new("id"), Name = "Id", Type = new(scalar.Id), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(), Annotations = new AnnotationBag { Items = [new Annotation { Key = new(CoreSemanticAnnotationKeys.LogicalType), Value = "CustomerId", Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }] } };
        var owner = new ObjectTypeDefinition { Id = new("Customer"), Name = "Customer", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = [property], Keys = [] };
        var model = new TypeSchemaModel { Id = new("M0077"), Types = [owner, scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [owner.Id] = owner, [scalar.Id] = scalar }, Annotations = new() };

        JsonElement root = JsonSchemaExporter.Export(model).Document.RootElement;
        JsonElement schema = root.GetProperty("properties").GetProperty("Id");
        _ = await Assert.That(schema.GetProperty("$ref").GetString()).IsEqualTo("#/$defs/Guid");
        _ = await Assert.That(root.GetProperty("$defs").GetProperty("Guid").GetProperty("format").GetString()).IsEqualTo("uuid");
        _ = await Assert.That(schema.GetProperty("x-stm").GetProperty("logicalType").GetString()).IsEqualTo("CustomerId");
    }
}
