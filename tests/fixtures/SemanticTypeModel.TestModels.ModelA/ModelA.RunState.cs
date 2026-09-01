using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.TestModels.ModelA.RunState;

[SemanticType(SemanticTypeRole.ValueObject)] public sealed record OrderSourceStatistics(int Accepted, int Rejected);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ComponentStateEnvelope(Guid Id, ReadOnlyMemory<byte> Payload);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record SourceExecutionRecord(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ProcessingFailureRecord(Guid Id, string Code, string Message);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ControlOperationRecord(Guid Id, string Operation, DateTimeOffset RequestedAt);

[SemanticType(SemanticTypeRole.Entity)]
public sealed record OrderFulfillmentRunSnapshot
{
    [SemanticKey] public required Guid Id { get; init; }
    public required Guid RunId { get; init; }
    public required Guid SourceId { get; init; }
    [SemanticOwned] public required OrderSourceStatistics Statistics { get; init; }
    [SemanticOwned] public required ComponentStateEnvelope ComponentState { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<SourceExecutionRecord> Executions { get; init; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<ProcessingFailureRecord> Failures { get; init; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<ControlOperationRecord> ControlOperations { get; init; } = [];
    public required ReadOnlyMemory<byte> RawPayload { get; init; }
    [SemanticIgnore] public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}
