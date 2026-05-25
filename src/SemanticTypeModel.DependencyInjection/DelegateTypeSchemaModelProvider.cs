using SemanticTypeModel.Abstractions.Runtime;

namespace SemanticTypeModel.DependencyInjection;

internal sealed class DelegateTypeSchemaModelProvider(Func<CancellationToken, ValueTask<TypeSchemaModelResult>> factory) : ITypeSchemaModelProvider
{
    public ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default)
    {
        return factory(cancellationToken);
    }
}
