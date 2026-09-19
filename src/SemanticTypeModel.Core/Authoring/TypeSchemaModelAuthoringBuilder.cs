using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Validation;

namespace SemanticTypeModel.Core.Authoring;

/// <summary>
/// Builds a validated canonical model from explicitly declared canonical type definitions.
/// </summary>
/// <remarks>Initializes an authoring builder for the supplied stable model identity.</remarks>
public sealed class TypeSchemaModelAuthoringBuilder(SchemaModelId id, AnnotationBag? annotations = null)
{
    private readonly SchemaModelId _id = id;
    private readonly AnnotationBag _annotations = annotations ?? new AnnotationBag();
    private readonly List<TypeDefinition> _types = [];

    /// <summary>Adds one existing canonical type definition in declaration order.</summary>
    public TypeSchemaModelAuthoringBuilder AddType(TypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _types.Add(definition);
        return this;
    }

    /// <summary>Finalizes a fresh canonical snapshot and validates its invariants.</summary>
    public TypeSchemaModelAuthoringResult Build()
    {
        // Copy the root collection and index on every build. The builder's mutable working
        // state is never exposed through a previously returned model.
        IReadOnlyList<TypeDefinition> types = [.. _types];
        var typesById = new Dictionary<TypeId, TypeDefinition>();
        var diagnostics = new List<SchemaDiagnostic>();

        foreach (TypeDefinition type in types)
        {
            if (!typesById.TryAdd(type.Id, type))
            {
                diagnostics.Add(new SchemaDiagnostic
                {
                    Severity = SchemaDiagnosticSeverity.Error,
                    Code = "STM0001",
                    Message = $"Duplicate TypeId '{type.Id.Value}' found in the model.",
                    Stage = SchemaDiagnosticStage.Validation,
                    ModelPath = ModelPath.ForType(type.Id),
                });
            }
        }

        var model = new TypeSchemaModel
        {
            Id = _id,
            Types = types,
            TypesById = new Dictionary<TypeId, TypeDefinition>(typesById),
            Annotations = new AnnotationBag { Items = [.. _annotations.Items] },
        };

        IReadOnlyList<SchemaDiagnostic> canonicalDiagnostics = TypeSchemaModelValidator.Validate(model);
        diagnostics.AddRange(canonicalDiagnostics.Where(diagnostic => diagnostic.Code != "STM0001" || !diagnostics.Any(existing => existing.ModelPath == diagnostic.ModelPath)));

        return new TypeSchemaModelAuthoringResult
        {
            Model = diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error) ? null : model,
            Diagnostics = diagnostics,
        };
    }
}

/// <summary>
/// Result of finalizing programmatic canonical model declarations.
/// </summary>
public sealed record TypeSchemaModelAuthoringResult
{
    /// <summary>The finalized model, or <see langword="null"/> when errors prevent success.</summary>
    public TypeSchemaModel? Model { get; init; }
    /// <summary>Structured authoring and canonical validation diagnostics.</summary>
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; } = [];
    /// <summary>Gets whether a usable model was finalized without error diagnostics.</summary>
    public bool Succeeded => Model is not null && !HasErrors;
    /// <summary>Gets whether any error diagnostic was produced.</summary>
    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
}
