using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.TestModels.ModelA;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;
using ModelB = SemanticTypeModel.TestModels.ModelB;
using ModelBGenerated = SemanticTypeModel.TestModels.ModelB.Generated;

namespace SemanticTypeModel.PowerBI.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0073ProjectionMatrixTests
{
    [Test]
    public async Task Generated_model_should_project_complete_scalar_and_strong_scalar_matrix()
    {
        SemanticDerivationResult<PowerBiSemanticModel> result = ModelAGenerated.ModelASemanticTypeModel.Create().DerivePowerBiModel();
        PowerBiTableDefinition table = result.Model.Tables.Single(value => value.Name == nameof(ProjectionMatrixEntity));

        foreach (ProjectionMatrixCase matrixCase in ProjectionMatrix.Cases)
        {
            PowerBiDataType expected = ExpectedDataType(matrixCase.ScalarKind);
            PowerBiColumnDefinition scalar = table.Columns.Single(column => column.Name == matrixCase.PropertyName);
            PowerBiColumnDefinition strong = table.Columns.Single(column => column.Name == matrixCase.StrongScalarPropertyName);
            _ = await Assert.That(scalar.DataType).IsEqualTo(expected);
            _ = await Assert.That(strong.DataType).IsEqualTo(expected);
            _ = await Assert.That(scalar.IsKey).IsFalse();
            _ = await Assert.That(strong.IsKey).IsFalse();
        }
    }

    [Test]
    public async Task Independent_generated_models_should_remain_model_local_in_one_process()
    {
        SemanticDerivationResult<PowerBiSemanticModel> modelA = ModelAGenerated.ModelASemanticTypeModel.Create().DerivePowerBiModel();
        SemanticDerivationResult<PowerBiSemanticModel> modelB = ModelBGenerated.ModelBSemanticTypeModel.Create().DerivePowerBiModel();

        _ = await Assert.That(modelA.Model.Tables.Any(table => table.Name == nameof(ProjectionMatrixEntity))).IsTrue();
        _ = await Assert.That(modelB.Model.Tables.Any(table => table.Name == nameof(ModelB.BillingRecord))).IsTrue();
        _ = await Assert.That(modelA.Model.Tables.Any(table => table.Name == nameof(ModelB.BillingRecord))).IsFalse();
        _ = await Assert.That(modelB.Model.Tables.Any(table => table.Name == nameof(ProjectionMatrixEntity))).IsFalse();
    }

    private static PowerBiDataType ExpectedDataType(ScalarKind kind)
    {
        return kind switch
        {
            ScalarKind.Boolean => PowerBiDataType.Boolean,
            ScalarKind.String => PowerBiDataType.String,
            ScalarKind.Integer => PowerBiDataType.Int64,
            ScalarKind.Number => PowerBiDataType.Double,
            ScalarKind.Decimal => PowerBiDataType.Decimal,
            ScalarKind.Date => PowerBiDataType.Date,
            ScalarKind.Time => PowerBiDataType.Time,
            ScalarKind.DateTime or ScalarKind.DateTimeOffset => PowerBiDataType.DateTime,
            ScalarKind.Duration => PowerBiDataType.String,
            ScalarKind.Guid => PowerBiDataType.String,
            ScalarKind.Binary => PowerBiDataType.Binary,
            ScalarKind.Json => PowerBiDataType.String,
            ScalarKind.Unknown => PowerBiDataType.String,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
