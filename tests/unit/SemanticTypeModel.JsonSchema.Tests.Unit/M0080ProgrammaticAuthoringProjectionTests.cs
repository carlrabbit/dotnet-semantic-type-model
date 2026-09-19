using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Authoring;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591, CA1707
public sealed class M0080ProgrammaticAuthoringProjectionTests
{
    [Test]
    public async Task Programmatic_model_uses_the_existing_json_schema_entry_point()
    {
        ScalarTypeDefinition text = new() { Id = new("Text"), Name = "Text", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = ScalarKind.String };
        ObjectTypeDefinition root = new() { Id = new("Root"), Name = "Root", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Keys = [], Properties = [new PropertyDefinition { Id = new("Name"), Name = "Name", Type = new(text.Id), Cardinality = new() { IsRequired = true }, Constraints = new(), Annotations = new() }] };
        TypeSchemaModel model = new TypeSchemaModelAuthoringBuilder(new SchemaModelId("m0080")).AddType(root).AddType(text).Build().Model!;

        JsonElement document = JsonSchemaExporter.Export(model).Document.RootElement;
        _ = await Assert.That(document.GetProperty("properties").GetProperty("Name").GetProperty("$ref").GetString()).IsEqualTo("#/$defs/Text");
    }
}
