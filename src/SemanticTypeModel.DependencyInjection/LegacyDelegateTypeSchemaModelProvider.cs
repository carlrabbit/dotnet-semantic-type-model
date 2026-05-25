using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Runtime;
using Legacy = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.DependencyInjection;

internal sealed class LegacyDelegateTypeSchemaModelProvider(Func<CancellationToken, ValueTask<Legacy.TypeSchemaModel>> factory) : ITypeSchemaModelProvider
{
    public async ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default)
    {
        Legacy.TypeSchemaModel model = await factory(cancellationToken).ConfigureAwait(false);
        return LegacyTypeSchemaModelAdapter.Adapt(model);
    }
}
