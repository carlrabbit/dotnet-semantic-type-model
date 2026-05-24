using System.Text;
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Produces deterministic legal type names for runtime-loaded models without mutating stable ids.
/// </summary>
public sealed class NormalizeNamesTransformation(bool renameExplicitNames = false) : ISchemaTransformation
{
    /// <summary>
    /// Gets a value indicating whether explicit legal names may be rewritten.
    /// </summary>
    public bool RenameExplicitNames { get; } = renameExplicitNames;

    /// <inheritdoc />
    public ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, TypeId> usedNames = new(StringComparer.OrdinalIgnoreCase);
        List<TypeDefinition> normalizedTypes = [];

        foreach (TypeDefinition original in model.Model.Types)
        {
            TypeDefinition clone = TypeSchemaModelCloner.CloneTypeDefinition(original);
            var sourceName = string.IsNullOrWhiteSpace(clone.Name)
                ? (!string.IsNullOrWhiteSpace(clone.DisplayName) ? clone.DisplayName : clone.Id.Value)
                : clone.Name;

            var normalizedName = ShouldNormalize(clone.Name)
                ? NormalizeName(sourceName)
                : clone.Name;

            if (usedNames.TryGetValue(normalizedName, out TypeId existingTypeId))
            {
                context.Diagnostics.Report(new SchemaDiagnostic
                {
                    Severity = SchemaDiagnosticSeverity.Warning,
                    Code = "STM2001",
                    Message = $"Normalized type name '{normalizedName}' collides with type '{existingTypeId.Value}'. A deterministic suffix was applied.",
                    Stage = SchemaDiagnosticStage.Transformation,
                    PipelineStage = context.PipelineStage,
                    ModelPath = ModelPath.ForType(clone.Id),
                    RelatedModelPaths = [ModelPath.ForType(existingTypeId)],
                });

                normalizedName = Disambiguate(normalizedName, clone.Id, usedNames);
            }

            usedNames[normalizedName] = clone.Id;

            if (!string.Equals(clone.Name, normalizedName, StringComparison.Ordinal))
            {
                context.Diagnostics.Report(new SchemaDiagnostic
                {
                    Severity = SchemaDiagnosticSeverity.Info,
                    Code = "STM2002",
                    Message = $"Type name '{clone.Name}' was normalized to '{normalizedName}'.",
                    Stage = SchemaDiagnosticStage.Transformation,
                    PipelineStage = context.PipelineStage,
                    ModelPath = ModelPath.ForType(clone.Id),
                });

                clone = clone with { Name = normalizedName };
            }

            normalizedTypes.Add(clone);
        }

        model.Replace(new TypeSchemaModel
        {
            Id = model.Model.Id,
            Types = normalizedTypes,
            TypesById = TypeSchemaModelCloner.CreateTypeIndex(normalizedTypes),
            Annotations = TypeSchemaModelCloner.CloneAnnotationBag(model.Model.Annotations),
        });

        return ValueTask.CompletedTask;
    }

    private bool ShouldNormalize(string name)
    {
        return RenameExplicitNames || string.IsNullOrWhiteSpace(name) || !IsLegalName(name);
    }

    private static string Disambiguate(string candidate, TypeId typeId, Dictionary<string, TypeId> usedNames)
    {
        var typeSuffix = NormalizeName(typeId.Value);
        var disambiguated = $"{candidate}_{typeSuffix}";
        var suffix = 2;

        while (usedNames.ContainsKey(disambiguated))
        {
            disambiguated = $"{candidate}_{typeSuffix}_{suffix}";
            suffix++;
        }

        return disambiguated;
    }

    private static bool IsLegalName(string name)
    {
        return !string.IsNullOrWhiteSpace(name)
            && (char.IsLetter(name[0]) || name[0] == '_')
            && name.All(static character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static string NormalizeName(string value)
    {
        StringBuilder builder = new();
        var previousUnderscore = false;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                _ = builder.Append(character);
                previousUnderscore = false;
                continue;
            }

            if (!previousUnderscore)
            {
                _ = builder.Append('_');
                previousUnderscore = true;
            }
        }

        var normalized = builder.ToString().Trim('_');

        if (string.IsNullOrEmpty(normalized))
        {
            normalized = "Type";
        }

        if (!(char.IsLetter(normalized[0]) || normalized[0] == '_'))
        {
            normalized = $"Type_{normalized}";
        }

        return normalized;
    }
}
