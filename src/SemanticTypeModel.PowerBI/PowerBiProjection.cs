using SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Provides deterministic Power BI projection entry points.
/// </summary>
public static class PowerBiProjection
{
    /// <summary>
    /// Projects a canonical semantic model to a Power BI projection model.
    /// </summary>
    /// <param name="model">The source canonical model.</param>
    /// <param name="configure">Optional projection option callback.</param>
    /// <returns>The projected Power BI model.</returns>
    public static PowerBiProjectionModel Project(TypeSchemaModel model, Action<PowerBiProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        var options = new PowerBiProjectionOptions();
        configure?.Invoke(options);

        var context = new SchemaProjectionContext { Diagnostics = [] };
        return new PowerBiModelProjection(options).Project(model, context);
    }
}

/// <summary>
/// Provides extension methods for Power BI projections.
/// </summary>
public static class PowerBiProjectionExtensions
{
    /// <summary>
    /// Projects a canonical semantic model to a Power BI projection model.
    /// </summary>
    /// <param name="model">The source canonical model.</param>
    /// <param name="configure">Optional projection option callback.</param>
    /// <returns>The projected Power BI model.</returns>
    public static PowerBiProjectionModel ToPowerBiModel(this TypeSchemaModel model, Action<PowerBiProjectionOptions>? configure = null)
    {
        return PowerBiProjection.Project(model, configure);
    }
}
