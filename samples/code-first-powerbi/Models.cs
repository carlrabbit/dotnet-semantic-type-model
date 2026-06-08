using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.Samples.CodeFirstPowerBi;

// A fact-like domain object is enough to demonstrate local Power BI metadata projection.
[SemanticType]
[SemanticName("SalesRecord")]
public sealed class SalesRecord
{
    [SemanticKey]
    public int SalesKey { get; init; }

    public decimal Amount { get; init; }
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
}

public sealed class WorkflowSpecification
{
    public required string Name { get; init; }
}
