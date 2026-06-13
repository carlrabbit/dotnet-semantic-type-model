using SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Executes deterministic schema transformations over immutable canonical model snapshots.
/// </summary>
public sealed class SchemaTransformationPipeline
{
    private readonly List<PipelineTransformationEntry> _transformations = [];

    /// <summary>
    /// Creates an empty transformation pipeline.
    /// </summary>
    public static SchemaTransformationPipeline Create()
    {
        return new SchemaTransformationPipeline();
    }

    /// <summary>
    /// Adds the minimal core default transformation sequence.
    /// </summary>
    public SchemaTransformationPipeline UseCoreDefaults()
    {
        return Use(new NormalizeAnnotationsTransformation())
            .Add(new NormalizeSemanticAliasesTransformation())
            .Add(new DeriveSemanticKeysTransformation())
            .Add(new ValidateEnvelopeSemanticsTransformation())
            .Add(new ValidateEvolutionOwnershipLifecycleSemanticsTransformation())
            .Add(new NormalizeDisplayMetadataTransformation())
            .Use(new ValidateModelTransformation());
    }

    /// <summary>
    /// Removes all configured transformations.
    /// </summary>
    public SchemaTransformationPipeline Clear()
    {
        _transformations.Clear();
        return this;
    }

    /// <summary>
    /// Adds a legacy transformation to the pipeline in execution order.
    /// </summary>
    public SchemaTransformationPipeline Use(ISchemaTransformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        return Add(ToSemanticTransformation(transformation, _transformations.Count + 1), transformation.GetType());
    }

    /// <summary>
    /// Adds a transformation to the pipeline in execution order.
    /// </summary>
    public SchemaTransformationPipeline Add(ISemanticModelTransformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        return Add(transformation, transformation.GetType());
    }

    /// <summary>
    /// Removes a configured transformation by implementation type.
    /// </summary>
    public SchemaTransformationPipeline Remove<TTransformation>()
    {
        var index = FindIndexByType(typeof(TTransformation));
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation type '{typeof(TTransformation).FullName}' was not found in the pipeline.");
        }

        _transformations.RemoveAt(index);
        return this;
    }

    /// <summary>
    /// Removes a configured transformation by stable identifier.
    /// </summary>
    public SchemaTransformationPipeline Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var index = FindIndexById(id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation id '{id}' was not found in the pipeline.");
        }

        _transformations.RemoveAt(index);
        return this;
    }

    /// <summary>
    /// Replaces a configured transformation by implementation type.
    /// </summary>
    public SchemaTransformationPipeline Replace<TTransformation>(ISemanticModelTransformation replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        var index = FindIndexByType(typeof(TTransformation));
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation type '{typeof(TTransformation).FullName}' was not found in the pipeline.");
        }

        ReplaceAt(index, replacement, replacement.GetType());
        return this;
    }

    /// <summary>
    /// Replaces a configured transformation by stable identifier.
    /// </summary>
    public SchemaTransformationPipeline Replace(string id, ISemanticModelTransformation replacement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(replacement);
        var index = FindIndexById(id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation id '{id}' was not found in the pipeline.");
        }

        ReplaceAt(index, replacement, replacement.GetType());
        return this;
    }

    /// <summary>
    /// Inserts a transformation before a configured transformation type.
    /// </summary>
    public SchemaTransformationPipeline AddBefore<TTransformation>(ISemanticModelTransformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        var index = FindIndexByType(typeof(TTransformation));
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation type '{typeof(TTransformation).FullName}' was not found in the pipeline.");
        }

        InsertAt(index, transformation, transformation.GetType());
        return this;
    }

    /// <summary>
    /// Inserts a transformation after a configured transformation type.
    /// </summary>
    public SchemaTransformationPipeline AddAfter<TTransformation>(ISemanticModelTransformation transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        var index = FindIndexByType(typeof(TTransformation));
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation type '{typeof(TTransformation).FullName}' was not found in the pipeline.");
        }

        InsertAt(index + 1, transformation, transformation.GetType());
        return this;
    }

    /// <summary>
    /// Inserts a transformation before a configured transformation identifier.
    /// </summary>
    public SchemaTransformationPipeline AddBefore(string id, ISemanticModelTransformation transformation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(transformation);
        var index = FindIndexById(id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation id '{id}' was not found in the pipeline.");
        }

        InsertAt(index, transformation, transformation.GetType());
        return this;
    }

    /// <summary>
    /// Inserts a transformation after a configured transformation identifier.
    /// </summary>
    public SchemaTransformationPipeline AddAfter(string id, ISemanticModelTransformation transformation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(transformation);
        var index = FindIndexById(id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Transformation id '{id}' was not found in the pipeline.");
        }

        InsertAt(index + 1, transformation, transformation.GetType());
        return this;
    }

    /// <summary>
    /// Gets the deterministic configured transformation order.
    /// </summary>
    public IReadOnlyList<string> GetTransformationOrder()
    {
        return [.. _transformations.Select(static entry => entry.Transformation.Id)];
    }

    /// <summary>
    /// Runs the configured transformations sequentially.
    /// </summary>
    public SemanticModelTransformationResult Run(
        TypeSchemaModel model,
        SchemaPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= SchemaPipelineOptions.Default;

        TypeSchemaModel current = TypeSchemaModelCloner.Clone(model);
        SchemaDiagnosticSink diagnostics = new(options.InitialDiagnostics, options.PromoteWarningsToErrors);
        List<SemanticTransformationTraceEntry> traceEntries = [];

        for (var index = 0; index < _transformations.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PipelineTransformationEntry entry = _transformations[index];
            var beforeCount = diagnostics.Diagnostics.Count;

            SemanticModelTransformationContext context = new()
            {
                TransformationId = entry.Transformation.Id,
                Diagnostics = diagnostics,
                Options = options,
                CancellationToken = cancellationToken,
            };

            SemanticModelTransformationStepResult stepResult = entry.Transformation.Transform(current, context);
            current = TypeSchemaModelCloner.Clone(stepResult.Model);

            IReadOnlyList<SchemaDiagnostic> stepDiagnostics = [.. diagnostics.Diagnostics.Skip(beforeCount)];
            traceEntries.Add(new SemanticTransformationTraceEntry
            {
                Sequence = index + 1,
                TransformationId = entry.Transformation.Id,
                DisplayName = entry.Transformation.DisplayName,
                DiagnosticCount = stepDiagnostics.Count,
                DiagnosticCodes = [.. stepDiagnostics.Select(static diagnostic => diagnostic.Code)],
                ChangeSummary = stepResult.ChangeSummary,
            });

            if (!stepResult.Continue)
            {
                break;
            }

            if (diagnostics.HasErrors && !options.ContinueOnError)
            {
                break;
            }
        }

        return new SemanticModelTransformationResult
        {
            Model = TypeSchemaModelCloner.Clone(current),
            Diagnostics = [.. diagnostics.Diagnostics],
            Trace = new SemanticTransformationTrace { Entries = traceEntries },
        };
    }

    /// <summary>
    /// Runs the configured transformations sequentially against the model.
    /// </summary>
    public ValueTask<SchemaPipelineResult> RunAsync(
        TypeSchemaModel model,
        SchemaPipelineOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        SemanticModelTransformationResult result = Run(model, options, cancellationToken);
        return ValueTask.FromResult(new SchemaPipelineResult
        {
            Model = result.Model,
            Diagnostics = result.Diagnostics,
        });
    }

    private SchemaTransformationPipeline Add(ISemanticModelTransformation transformation, Type matchType)
    {
        EnsureUniqueId(transformation.Id, ignoreIndex: null);
        _transformations.Add(new PipelineTransformationEntry(transformation, matchType));
        return this;
    }

    private void InsertAt(int index, ISemanticModelTransformation transformation, Type matchType)
    {
        EnsureUniqueId(transformation.Id, ignoreIndex: null);
        _transformations.Insert(index, new PipelineTransformationEntry(transformation, matchType));
    }

    private void ReplaceAt(int index, ISemanticModelTransformation replacement, Type matchType)
    {
        EnsureUniqueId(replacement.Id, index);
        _transformations[index] = new PipelineTransformationEntry(replacement, matchType);
    }

    private void EnsureUniqueId(string id, int? ignoreIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        for (var index = 0; index < _transformations.Count; index++)
        {
            if (ignoreIndex == index)
            {
                continue;
            }

            if (string.Equals(_transformations[index].Transformation.Id, id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Transformation id '{id}' is already configured. Use Replace when replacing an existing transformation.");
            }
        }
    }

    private int FindIndexByType(Type type)
    {
        return _transformations.FindIndex(entry => type.IsAssignableFrom(entry.MatchType) || type.IsInstanceOfType(entry.Transformation));
    }

    private int FindIndexById(string id)
    {
        return _transformations.FindIndex(entry => string.Equals(entry.Transformation.Id, id, StringComparison.Ordinal));
    }

    private static ISemanticModelTransformation ToSemanticTransformation(ISchemaTransformation transformation, int sequence)
    {
        return transformation as ISemanticModelTransformation ?? new LegacySchemaTransformationAdapter(transformation, sequence);
    }

    private sealed record PipelineTransformationEntry(ISemanticModelTransformation Transformation, Type MatchType);

    private sealed class LegacySchemaTransformationAdapter(ISchemaTransformation transformation, int sequence) : ISemanticModelTransformation
    {
        public string Id => $"{transformation.GetType().FullName ?? transformation.GetType().Name}#{sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        public string DisplayName => transformation.GetType().Name;

        public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
        {
            TypeSchemaModelBuilder builder = new(TypeSchemaModelCloner.Clone(model));
            SchemaTransformContext legacyContext = new()
            {
                Diagnostics = context.Diagnostics,
                AnnotationPolicy = context.AnnotationPolicy,
                PipelineStage = context.TransformationId,
                Services = context.Services,
            };

            transformation.TransformAsync(builder, legacyContext, context.CancellationToken).AsTask().GetAwaiter().GetResult();
            return new SemanticModelTransformationStepResult { Model = builder.Build() };
        }
    }
}
