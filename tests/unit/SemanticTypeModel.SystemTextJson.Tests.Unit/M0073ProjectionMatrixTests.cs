using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.TestModels.ModelA;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

public sealed class M0073ProjectionMatrixTests
{
    [Test]
    public async Task Strong_scalar_matrix_round_trips_each_supported_backing_kind()
    {
        var options = new JsonSerializerOptions();
        TypeSchemaModel model = ModelAGenerated.ModelASemanticTypeModel.Create();
        _ = await Assert.That(model.Types.OfType<StrongScalarTypeDefinition>().Count(type => ProjectionMatrix.Cases.Any(matrixCase => matrixCase.StrongScalarClrType.Name == type.Name))).IsEqualTo(12);
        _ = options.AddSemanticTypeModelJson(model);

        foreach (ProjectionMatrixCase matrixCase in ProjectionMatrix.Cases)
        {
            var value = CreateValue(matrixCase.ScalarKind, matrixCase.StrongScalarClrType);
            var json = JsonSerializer.Serialize(value, matrixCase.StrongScalarClrType, options);
            var roundTripped = JsonSerializer.Deserialize(json, matrixCase.StrongScalarClrType, options)!;
            var expectedUnderlying = matrixCase.StrongScalarClrType.GetProperty("Value")!.GetValue(value);
            var actualUnderlying = matrixCase.StrongScalarClrType.GetProperty("Value")!.GetValue(roundTripped);

            _ = await Assert.That(json).DoesNotContain("Value");
            _ = await Assert.That(AreUnderlyingValuesEqual(expectedUnderlying, actualUnderlying)).IsTrue();
        }
    }

    private static object CreateValue(ScalarKind kind, Type wrapperType)
    {
        object underlying = kind switch
        {
            ScalarKind.Boolean => true,
            ScalarKind.String => "strong",
            ScalarKind.Integer => 42L,
            ScalarKind.Number => 4.25D,
            ScalarKind.Decimal => 12.50M,
            ScalarKind.Date => new DateOnly(2026, 8, 31),
            ScalarKind.Time => new TimeOnly(12, 34, 56),
            ScalarKind.DateTime => new DateTime(2026, 8, 31, 12, 34, 56, DateTimeKind.Utc),
            ScalarKind.DateTimeOffset => new DateTimeOffset(2026, 8, 31, 12, 34, 56, TimeSpan.Zero),
            ScalarKind.Duration => TimeSpan.FromMinutes(42),
            ScalarKind.Guid => Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ScalarKind.Binary => new byte[] { 1, 2, 3, 4 },
            ScalarKind.Json => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            ScalarKind.Unknown => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        return Activator.CreateInstance(wrapperType, underlying) ?? throw new InvalidOperationException();
    }

    private static bool AreUnderlyingValuesEqual(object? expected, object? actual)
    {
        return expected switch
        {
            byte[] expectedBytes when actual is byte[] actualBytes => actualBytes.SequenceEqual(expectedBytes),
            _ => Equals(actual, expected),
        };
    }
}
