using SemanticTypeModel.DotNet;

#pragma warning disable CS1591

namespace SemanticTypeModel.RealWorldFixtures.OrderFulfillmentRunStateModel;

public readonly record struct FulfillmentRunId(Guid Value);
public readonly record struct OrderSourceId(Guid Value);
public readonly record struct SourceExecutionId(Guid Value);
public readonly record struct ProcessingFailureId(Guid Value);
public readonly record struct ControlOperationId(Guid Value);
public readonly record struct ComponentSnapshotId(Guid Value);

[SemanticType(SemanticTypeRole.ValueObject)] public sealed record OrderSourceStatistics(int Accepted, int Rejected);
[SemanticType(SemanticTypeRole.ValueObject)]
public sealed record ComponentStateEnvelope
{
    public required ComponentSnapshotId Id { get; init; }
    public required ReadOnlyMemory<byte> Payload { get; init; }
}
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
    public IReadOnlyList<SourceExecutionRecord> Executions { get; init; } = [];
    public IReadOnlyList<ProcessingFailureRecord> Failures { get; init; } = [];
    public IReadOnlyList<ControlOperationRecord> ControlOperations { get; init; } = [];
    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>();
}

public sealed record SaveFulfillmentRunRequest(OrderFulfillmentRunSnapshot Snapshot);
public sealed record FulfillmentRunOverview(FulfillmentRunId RunId, int FailureCount);
public interface IFulfillmentRunStateRepository
{
    Task SaveAsync(SaveFulfillmentRunRequest request, CancellationToken cancellationToken);
    Task<FulfillmentRunOverview?> FindAsync(FulfillmentRunId runId, CancellationToken cancellationToken);
}
