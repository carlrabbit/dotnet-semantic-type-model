using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.PowerBI;

/// <summary>Configures Power BI domain semantic model derivation.</summary>
public sealed class PowerBiDerivationOptions
{
    /// <summary>Gets the configurable canonical transformation sequence executed before Power BI domain mapping.</summary>
    public SchemaTransformationPipeline Transformations { get; } = SchemaTransformationPipeline.Create();

    /// <summary>Gets projection options used by the Power BI domain mapper.</summary>
    public PowerBiProjectionOptions Projection { get; } = new();

    /// <summary>Gets Power BI envelope analytical projection policy configuration.</summary>
    public PowerBiEnvelopeProjectionOptions Envelopes { get; } = new();

    /// <summary>Gets explicit measures added after canonical metadata derivation.</summary>
    public PowerBiMeasureBuilder Measures { get; } = new();

    /// <summary>Gets explicit calculated tables added after canonical metadata derivation.</summary>
    public PowerBiCalculatedTableBuilder CalculatedTables { get; } = new();

    /// <summary>Gets or sets canonical transformation pipeline options.</summary>
    public SchemaPipelineOptions PipelineOptions { get; set; } = SchemaPipelineOptions.Default;

    internal IList<Action<PowerBiSemanticModel>> ConfigureModelCallbacks { get; } = [];

    /// <summary>Adds the default core and Power BI derivation transformations.</summary>
    public PowerBiDerivationOptions UseDefaultTransformations()
    {
        _ = Transformations.UseCoreDefaults().Add(new DerivePowerBiTablesTransformation());
        return this;
    }

    /// <summary>Adds a post-derive model configuration hook.</summary>
    /// <param name="configure">The deterministic model configuration callback.</param>
    public PowerBiDerivationOptions ConfigureModel(Action<PowerBiSemanticModel> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ConfigureModelCallbacks.Add(configure);
        return this;
    }
}

/// <summary>Configures explicit Power BI measures owned by the caller.</summary>
public sealed class PowerBiMeasureBuilder
{
    private readonly List<PowerBiExplicitMeasure> _items = [];

    internal IReadOnlyList<PowerBiExplicitMeasure> Items => _items;

    /// <summary>Adds an explicit DAX measure for the table represented by <typeparamref name="TTable" />.</summary>
    public void Add<TTable>(string name, string dax, Action<PowerBiMeasureDefinition>? configure = null)
    {
        Add(typeof(TTable).Name, name, dax, configure);
    }

    /// <summary>Adds an explicit DAX measure for the named table.</summary>
    public void Add(string tableName, string name, string dax, Action<PowerBiMeasureDefinition>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dax);
        _items.Add(new PowerBiExplicitMeasure(tableName, name, dax, configure));
    }
}

/// <summary>Configures explicit calculated tables owned by the caller.</summary>
public sealed class PowerBiCalculatedTableBuilder
{
    private readonly List<PowerBiCalculatedTableDefinition> _items = [];

    internal IReadOnlyList<PowerBiCalculatedTableDefinition> Items => _items;

    /// <summary>Adds an explicit DAX calculated table.</summary>
    public void Add(string name, string dax, Action<PowerBiCalculatedTableDefinition>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dax);
        var table = new PowerBiCalculatedTableDefinition { Name = name, Expression = dax };
        configure?.Invoke(table);
        _items.Add(table);
    }
}

internal sealed record PowerBiExplicitMeasure(string TableName, string Name, string Expression, Action<PowerBiMeasureDefinition>? Configure);
