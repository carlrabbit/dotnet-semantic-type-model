using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.Samples.CodeFirstJsonSchema;

// Consumers mark normal application types with SemanticTypeModel.DotNet attributes.
[SemanticType(SemanticTypeRole.Entity)]
[SemanticName("Customer")]
[SemanticDescription("A customer account exported by the code-first JSON Schema sample.")]
public sealed class Customer
{
    // SemanticKey marks the identity member in the generated semantic model.
    [SemanticKey]
    public required string Id { get; init; }

    // SemanticName changes the semantic property name that projections see.
    [SemanticName("emailAddress")]
    [SemanticDescription("Primary contact email address.")]
    public required string Email { get; init; }

    public Address BillingAddress { get; init; } = new();
}

public sealed class Address
{
    public string Street { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;
}

[SemanticEnvelope("management")]
[SemanticType(SemanticTypeRole.Entity)]
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
