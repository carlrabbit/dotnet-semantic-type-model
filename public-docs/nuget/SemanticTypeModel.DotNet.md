# SemanticTypeModel.DotNet

`SemanticTypeModel.DotNet` provides the .NET attribute model and Roslyn-based extraction support for code-first SemanticTypeModel authoring.

```sh
dotnet add package SemanticTypeModel.DotNet --version 2.0.0
```

## Attribute vocabulary

Use semantic attributes to declare projection-neutral domain meaning in code. 2.0.0 includes envelope attributes:

```csharp
[SemanticEnvelope("management")]
public sealed class ManagedSpecificationEnvelope<TSpecification>
{
    [SemanticEnvelopePayload]
    public required TSpecification Specification { get; init; }

    [SemanticEnvelopeMetadata]
    public required long Revision { get; init; }
}
```

Extraction preserves attribute intent; transformations derive canonical semantics and diagnostics.

More details: `public-docs/guides/core-semantics.md`.
