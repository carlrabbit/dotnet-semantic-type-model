using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.TestModels.ModelA;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0073ProjectionMatrixTests
{
    [Test]
    public async Task Generated_model_should_expose_complete_scalar_matrix()
    {
        TypeSchemaModel model = ModelAGenerated.ModelASemanticTypeModel.Create();
        ObjectTypeDefinition entity = model.Types.OfType<ObjectTypeDefinition>().Single(type => type.Id.Value.EndsWith(nameof(ProjectionMatrixEntity), StringComparison.Ordinal));

        _ = await Assert.That(ProjectionMatrix.Cases.Count).IsEqualTo(12);
        foreach (ProjectionMatrixCase matrixCase in ProjectionMatrix.Cases)
        {
            PropertyDefinition scalarProperty = entity.Properties.Single(property => property.Name == matrixCase.PropertyName);
            ScalarTypeDefinition scalar = model.GetType(scalarProperty.Type.Id) as ScalarTypeDefinition
                ?? throw new InvalidOperationException(matrixCase.PropertyName);
            _ = await Assert.That(scalar.ScalarKind).IsEqualTo(matrixCase.ScalarKind);

        }
    }

    [Test]
    public async Task Exported_schema_should_preserve_native_representation_fidelity_metadata()
    {
        JsonElement definitions = JsonSchemaExporter.Export(ModelAGenerated.ModelASemanticTypeModel.Create()).Document.RootElement.GetProperty("$defs");
        JsonElement binary = definitions.EnumerateObject().First(item => item.Value.TryGetProperty("contentEncoding", out _)).Value;
        _ = await Assert.That(binary.GetProperty("type").GetString()).IsEqualTo("string");
        _ = await Assert.That(binary.GetProperty("contentEncoding").GetString()).IsEqualTo("base64");

        JsonProperty uriEntry = definitions.EnumerateObject().FirstOrDefault(item => item.Value.TryGetProperty("format", out JsonElement format) && format.GetString() == "uri-reference");
        if (uriEntry.Name is null)
        {
            throw new InvalidOperationException(string.Join(", ", definitions.EnumerateObject().Select(item => $"{item.Name}:{(item.Value.TryGetProperty("format", out JsonElement format) ? format.GetString() : "<none>")}")));
        }
        JsonElement uri = uriEntry.Value;
        _ = await Assert.That(uri.GetProperty("type").GetString()).IsEqualTo("string");
    }

    [Test]
    public async Task Json_scalar_should_export_as_unconstrained_json_with_semantic_kind_metadata()
    {
        var json = new ScalarTypeDefinition
        {
            Id = new TypeId("JsonValue"),
            Name = "JsonValue",
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            ScalarKind = ScalarKind.Json,
            Annotations = new AnnotationBag(),
        };
        var model = new TypeSchemaModel
        {
            Id = new SchemaModelId("JsonValue"),
            Types = [json],
            TypesById = new Dictionary<TypeId, TypeDefinition> { [json.Id] = json },
            Annotations = new AnnotationBag(),
        };

        JsonElement document = JsonSchemaExporter.Export(model).Document.RootElement;
        _ = await Assert.That(document.TryGetProperty("type", out _)).IsFalse();
        _ = await Assert.That(document.GetProperty("x-stm").GetProperty("scalarKind").GetString()).IsEqualTo("Json");
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
