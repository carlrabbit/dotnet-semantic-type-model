using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestModels.ModelA;

public sealed record ProjectionMatrixCase(
    string PropertyName,
    ScalarKind ScalarKind,
    Type ClrType,
    string OptionalPropertyName);

public static class ProjectionMatrix
{
    public static IReadOnlyList<ProjectionMatrixCase> Cases { get; } =
    [
        new(nameof(ProjectionMatrixEntity.BooleanValue), ScalarKind.Boolean, typeof(bool), nameof(ProjectionMatrixEntity.OptionalBooleanValue)),
        new(nameof(ProjectionMatrixEntity.StringValue), ScalarKind.String, typeof(string), nameof(ProjectionMatrixEntity.OptionalStringValue)),
        new(nameof(ProjectionMatrixEntity.IntegerValue), ScalarKind.Integer, typeof(long), nameof(ProjectionMatrixEntity.OptionalIntegerValue)),
        new(nameof(ProjectionMatrixEntity.NumberValue), ScalarKind.Number, typeof(double), nameof(ProjectionMatrixEntity.OptionalNumberValue)),
        new(nameof(ProjectionMatrixEntity.DecimalValue), ScalarKind.Decimal, typeof(decimal), nameof(ProjectionMatrixEntity.OptionalDecimalValue)),
        new(nameof(ProjectionMatrixEntity.DateValue), ScalarKind.Date, typeof(DateOnly), nameof(ProjectionMatrixEntity.OptionalDateValue)),
        new(nameof(ProjectionMatrixEntity.TimeValue), ScalarKind.Time, typeof(TimeOnly), nameof(ProjectionMatrixEntity.OptionalTimeValue)),
        new(nameof(ProjectionMatrixEntity.DateTimeValue), ScalarKind.DateTime, typeof(DateTime), nameof(ProjectionMatrixEntity.OptionalDateTimeValue)),
        new(nameof(ProjectionMatrixEntity.DateTimeOffsetValue), ScalarKind.DateTimeOffset, typeof(DateTimeOffset), nameof(ProjectionMatrixEntity.OptionalDateTimeOffsetValue)),
        new(nameof(ProjectionMatrixEntity.DurationValue), ScalarKind.Duration, typeof(TimeSpan), nameof(ProjectionMatrixEntity.OptionalDurationValue)),
        new(nameof(ProjectionMatrixEntity.GuidValue), ScalarKind.Guid, typeof(Guid), nameof(ProjectionMatrixEntity.OptionalGuidValue)),
        new(nameof(ProjectionMatrixEntity.BinaryValue), ScalarKind.Binary, typeof(byte[]), nameof(ProjectionMatrixEntity.OptionalBinaryValue)),
    ];
}
