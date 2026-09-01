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
    public async Task Generated_model_should_expose_complete_scalar_and_strong_scalar_matrix()
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

            PropertyDefinition strongProperty = entity.Properties.Single(property => property.Name == matrixCase.StrongScalarPropertyName);
            StrongScalarTypeDefinition strong = model.GetType(strongProperty.Type.Id) as StrongScalarTypeDefinition
                ?? throw new InvalidOperationException(matrixCase.StrongScalarPropertyName);
            _ = await Assert.That(((ScalarTypeDefinition)model.GetType(strong.ValueType.Id)).ScalarKind).IsEqualTo(matrixCase.ScalarKind);
        }
    }

    [Test]
    public async Task Exported_schema_should_map_each_strong_scalar_to_its_underlying_json_shape()
    {
        TypeSchemaModel model = ModelAGenerated.ModelASemanticTypeModel.Create();
        JsonElement root = JsonSchemaExporter.Export(model).Document.RootElement;
        JsonElement definitions = root.GetProperty("$defs");

        foreach (ProjectionMatrixCase matrixCase in ProjectionMatrix.Cases)
        {
            StrongScalarTypeDefinition strong = model.Types.OfType<StrongScalarTypeDefinition>().Single(type => type.Name == matrixCase.StrongScalarClrType.Name);
            JsonElement schema = definitions.GetProperty(strong.Id.Value);
            var expectedType = matrixCase.ScalarKind is ScalarKind.Boolean ? "boolean"
                : matrixCase.ScalarKind is ScalarKind.Integer ? "integer"
                : matrixCase.ScalarKind is ScalarKind.Number or ScalarKind.Decimal ? "number"
                : "string";

            _ = await Assert.That(schema.GetProperty("type").GetString()).IsEqualTo(expectedType);
        }
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
