# SemanticTypeModel.DotNet

`SemanticTypeModel.DotNet` exposes .NET extraction contracts and semantic attributes.

```sh
dotnet add package SemanticTypeModel.DotNet --version 1.1.0
```

This package is part of the stable package set. Public APIs follow the compatibility policy.

Common attribute surface:

- `[SemanticType]`, `[SemanticIgnore]`, `[SemanticName]`, `[SemanticDisplayName]`, `[SemanticDescription]`
- `[SemanticRole]`, `[SemanticKey]`, `[SemanticRelationship]`
- `[SemanticCategory]`, `[SemanticOrder]`, `[SemanticFormat]`
- `[SemanticStringConstraints]`, `[SemanticNumericConstraints]`, `[SemanticCollectionConstraints]`
- `[SemanticEnumValue]`, `[SemanticAnnotation]`

Example:

```csharp
using SemanticTypeModel.DotNet;

[SemanticType(SemanticTypeRole.Entity)]
[SemanticDisplayName("Customer account")]
public sealed class Customer
{
    [SemanticKey]
    public required string Id { get; init; }

    [SemanticFormat(SemanticScalarFormat.Email)]
    [SemanticStringConstraints(MinLength = 5, MaxLength = 200)]
    [SemanticAnnotation("ui.placeholder", "name@example.com")]
    public required string Email { get; init; }
}
```
