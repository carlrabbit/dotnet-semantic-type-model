using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestModels.ModelA;

public sealed record ProjectionMatrixCase(
    string PropertyName,
    ScalarKind ScalarKind,
    Type ClrType,
    string OptionalPropertyName,
    string StrongScalarPropertyName,
    string OptionalStrongScalarPropertyName,
    Type StrongScalarClrType);

public static class ProjectionMatrix
{
    public static IReadOnlyList<ProjectionMatrixCase> Cases { get; } =
    [
        new(nameof(ProjectionMatrixEntity.BooleanValue), ScalarKind.Boolean, typeof(bool), nameof(ProjectionMatrixEntity.OptionalBooleanValue), nameof(ProjectionMatrixEntity.BooleanId), nameof(ProjectionMatrixEntity.OptionalBooleanId), typeof(BooleanId)),
        new(nameof(ProjectionMatrixEntity.StringValue), ScalarKind.String, typeof(string), nameof(ProjectionMatrixEntity.OptionalStringValue), nameof(ProjectionMatrixEntity.StringId), nameof(ProjectionMatrixEntity.OptionalStringId), typeof(StringId)),
        new(nameof(ProjectionMatrixEntity.IntegerValue), ScalarKind.Integer, typeof(long), nameof(ProjectionMatrixEntity.OptionalIntegerValue), nameof(ProjectionMatrixEntity.IntegerId), nameof(ProjectionMatrixEntity.OptionalIntegerId), typeof(IntegerId)),
        new(nameof(ProjectionMatrixEntity.NumberValue), ScalarKind.Number, typeof(double), nameof(ProjectionMatrixEntity.OptionalNumberValue), nameof(ProjectionMatrixEntity.NumberId), nameof(ProjectionMatrixEntity.OptionalNumberId), typeof(NumberId)),
        new(nameof(ProjectionMatrixEntity.DecimalValue), ScalarKind.Decimal, typeof(decimal), nameof(ProjectionMatrixEntity.OptionalDecimalValue), nameof(ProjectionMatrixEntity.DecimalId), nameof(ProjectionMatrixEntity.OptionalDecimalId), typeof(DecimalId)),
        new(nameof(ProjectionMatrixEntity.DateValue), ScalarKind.Date, typeof(DateOnly), nameof(ProjectionMatrixEntity.OptionalDateValue), nameof(ProjectionMatrixEntity.DateId), nameof(ProjectionMatrixEntity.OptionalDateId), typeof(DateId)),
        new(nameof(ProjectionMatrixEntity.TimeValue), ScalarKind.Time, typeof(TimeOnly), nameof(ProjectionMatrixEntity.OptionalTimeValue), nameof(ProjectionMatrixEntity.TimeId), nameof(ProjectionMatrixEntity.OptionalTimeId), typeof(TimeId)),
        new(nameof(ProjectionMatrixEntity.DateTimeValue), ScalarKind.DateTime, typeof(DateTime), nameof(ProjectionMatrixEntity.OptionalDateTimeValue), nameof(ProjectionMatrixEntity.DateTimeId), nameof(ProjectionMatrixEntity.OptionalDateTimeId), typeof(DateTimeId)),
        new(nameof(ProjectionMatrixEntity.DateTimeOffsetValue), ScalarKind.DateTimeOffset, typeof(DateTimeOffset), nameof(ProjectionMatrixEntity.OptionalDateTimeOffsetValue), nameof(ProjectionMatrixEntity.DateTimeOffsetId), nameof(ProjectionMatrixEntity.OptionalDateTimeOffsetId), typeof(DateTimeOffsetId)),
        new(nameof(ProjectionMatrixEntity.DurationValue), ScalarKind.Duration, typeof(TimeSpan), nameof(ProjectionMatrixEntity.OptionalDurationValue), nameof(ProjectionMatrixEntity.DurationId), nameof(ProjectionMatrixEntity.OptionalDurationId), typeof(DurationId)),
        new(nameof(ProjectionMatrixEntity.GuidValue), ScalarKind.Guid, typeof(Guid), nameof(ProjectionMatrixEntity.OptionalGuidValue), nameof(ProjectionMatrixEntity.GuidId), nameof(ProjectionMatrixEntity.OptionalGuidId), typeof(GuidId)),
        new(nameof(ProjectionMatrixEntity.BinaryValue), ScalarKind.Binary, typeof(byte[]), nameof(ProjectionMatrixEntity.OptionalBinaryValue), nameof(ProjectionMatrixEntity.BinaryId), nameof(ProjectionMatrixEntity.OptionalBinaryId), typeof(BinaryId)),
    ];
}
