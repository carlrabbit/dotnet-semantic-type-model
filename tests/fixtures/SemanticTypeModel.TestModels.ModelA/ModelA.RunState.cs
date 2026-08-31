using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.TestModels.ModelA.RunState;

[SemanticStrongScalar] public readonly record struct FulfillmentRunId(Guid Value);
[SemanticStrongScalar] public readonly record struct OrderSourceId(Guid Value);
[SemanticStrongScalar] public readonly record struct SourceExecutionId(Guid Value);
[SemanticStrongScalar] public readonly record struct ProcessingFailureId(Guid Value);
[SemanticStrongScalar] public readonly record struct ControlOperationId(Guid Value);
[SemanticStrongScalar] public readonly record struct ComponentSnapshotId(Guid Value);

[SemanticType(SemanticTypeRole.ValueObject)] public sealed record OrderSourceStatistics(int Accepted, int Rejected);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ComponentStateEnvelope(ComponentSnapshotId Id, ReadOnlyMemory<byte> Payload);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record SourceExecutionRecord(SourceExecutionId Id, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ProcessingFailureRecord(ProcessingFailureId Id, string Code, string Message);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ControlOperationRecord(ControlOperationId Id, string Operation, DateTimeOffset RequestedAt);

[SemanticType(SemanticTypeRole.Entity)]
public sealed record OrderFulfillmentRunSnapshot
{
    [SemanticKey] public required Guid Id { get; init; }
    public required FulfillmentRunId RunId { get; init; }
    public required OrderSourceId SourceId { get; init; }
    [SemanticOwned] public required OrderSourceStatistics Statistics { get; init; }
    [SemanticOwned] public required ComponentStateEnvelope ComponentState { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<SourceExecutionRecord> Executions { get; init; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<ProcessingFailureRecord> Failures { get; init; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<ControlOperationRecord> ControlOperations { get; init; } = [];
    public required ReadOnlyMemory<byte> RawPayload { get; init; }
    [SemanticIgnore] public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}
