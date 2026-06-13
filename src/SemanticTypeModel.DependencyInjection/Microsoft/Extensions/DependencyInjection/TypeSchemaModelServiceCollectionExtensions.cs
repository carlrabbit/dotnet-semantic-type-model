using Microsoft.Extensions.DependencyInjection.Extensions;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Abstractions.Runtime;
using RuntimeTypeSchemaModel = SemanticTypeModel.Abstractions.Canonical.TypeSchemaModel;

// This file intentionally uses the Microsoft.Extensions.DependencyInjection namespace
// so the registration methods are discoverable as standard IServiceCollection extensions.
#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers semantic type model runtime services, providers, transformations, and projections.
/// </summary>
public static class TypeSchemaModelServiceCollectionExtensions
{
    /// <summary>
    /// Registers the semantic type model runtime services without registering a provider.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModelRuntime(this IServiceCollection services, TypeSchemaRuntimeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options ?? TypeSchemaRuntimeOptions.Default);
        services.TryAddSingleton<ITypeSchemaModelService, SemanticTypeModel.DependencyInjection.TypeSchemaModelService>();
        services.TryAddSingleton(typeof(ITypeSchemaProjectionService<>), typeof(SemanticTypeModel.DependencyInjection.TypeSchemaProjectionService<>));
        return services;
    }

    /// <summary>
    /// Registers an existing runtime canonical semantic model instance.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModel(this IServiceCollection services, RuntimeTypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return services.AddSemanticTypeModel(_ => new ValueTask<TypeSchemaModelResult>(new TypeSchemaModelResult { Model = model }));
    }

    /// <summary>
    /// Registers a runtime canonical semantic model factory.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModel(this IServiceCollection services, Func<RuntimeTypeSchemaModel> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return services.AddSemanticTypeModel(_ => new ValueTask<TypeSchemaModelResult>(new TypeSchemaModelResult { Model = factory() }));
    }

    /// <summary>
    /// Registers a runtime canonical semantic model-result factory.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModel(this IServiceCollection services, Func<TypeSchemaModelResult> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return services.AddSemanticTypeModel(_ => new ValueTask<TypeSchemaModelResult>(factory()));
    }

    /// <summary>
    /// Registers an asynchronous runtime canonical semantic model-result factory.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModel(this IServiceCollection services, Func<CancellationToken, ValueTask<TypeSchemaModelResult>> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        _ = services.AddSemanticTypeModelRuntime();
        _ = services.AddSingleton<ITypeSchemaModelProvider>(_ => new SemanticTypeModel.DependencyInjection.DelegateTypeSchemaModelProvider(factory));
        return services;
    }

    /// <summary>
    /// Registers a custom runtime model provider type.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModelProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, ITypeSchemaModelProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSemanticTypeModelRuntime();
        _ = services.AddSingleton<TProvider>();
        _ = services.AddSingleton<ITypeSchemaModelProvider>(static serviceProvider => serviceProvider.GetRequiredService<TProvider>());
        return services;
    }

    /// <summary>
    /// Registers a semantic type model transformation in deterministic execution order.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModelTransformation<TTransformation>(this IServiceCollection services)
        where TTransformation : class, ISchemaTransformation
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSemanticTypeModelRuntime();
        _ = services.AddSingleton<ISchemaTransformation, TTransformation>();
        return services;
    }

    /// <summary>
    /// Registers a semantic type model projection implementation for a projection result type.
    /// </summary>
    public static IServiceCollection AddSemanticTypeModelProjection<TProjection, TProjectionImplementation>(
        this IServiceCollection services,
        ProjectionTarget target)
        where TProjectionImplementation : class, ISchemaProjection<TProjection>
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSemanticTypeModelRuntime();
        _ = services.AddSingleton<ISchemaProjection<TProjection>, TProjectionImplementation>();
        _ = services.AddSingleton(serviceProvider => new SemanticTypeModel.DependencyInjection.RegisteredTypeSchemaProjection<TProjection>(
            serviceProvider.GetRequiredService<ISchemaProjection<TProjection>>(),
            target));
        return services;
    }
}
#pragma warning restore IDE0130
