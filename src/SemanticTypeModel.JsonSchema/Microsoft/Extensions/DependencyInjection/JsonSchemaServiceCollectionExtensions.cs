using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema;

// This file intentionally uses the Microsoft.Extensions.DependencyInjection namespace
// so the registration method is discoverable as a standard IServiceCollection extension.
#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the JSON Schema runtime projection over the semantic type model runtime API.
/// </summary>
public static class JsonSchemaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the JSON Schema runtime projection.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModelJsonSchema(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSemanticTypeModelProjection<JsonSchemaExportResult, SemanticTypeModel.JsonSchema.Runtime.JsonSchemaRuntimeProjection>(ProjectionTarget.JsonSchema);
        return services;
    }
}
#pragma warning restore IDE0130
