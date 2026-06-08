using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.Samples.CodeFirstEfCore;

// This is a normal consumer domain model; no Roslyn APIs or source strings are used.
[SemanticType]
[SemanticName("Customer")]
public sealed class Customer
{
    // The EF Core projection uses semantic key metadata when building model metadata.
    [SemanticKey]
    public int Id { get; init; }

    public required string Name { get; init; }
}

[SemanticEnvelope("management")]
[SemanticType]
public sealed class ManagedSpecificationEnvelope
{
    [SemanticKey]
    public Guid Id { get; init; }

    [SemanticEnvelopeMetadata]
    public long Revision { get; init; }

    [SemanticEnvelopeMetadata]
    public required string ModifiedBy { get; init; }

    [SemanticEnvelopeMetadata]
    public DateTimeOffset ModifiedAt { get; init; }

    [SemanticEnvelopePayload]
    public required WorkflowSpecification Specification { get; init; }

    public AuditInfo Audit { get; init; } = new();
}

public sealed class WorkflowSpecification
{
    public required string Name { get; init; }

    public IReadOnlyList<WorkflowStep> Steps { get; init; } = [];
}

public sealed class WorkflowStep
{
    public required string Name { get; init; }
}

public sealed class AuditInfo
{
    public string Source { get; init; } = "sample";
}
