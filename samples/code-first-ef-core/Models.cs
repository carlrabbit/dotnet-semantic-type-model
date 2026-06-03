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
