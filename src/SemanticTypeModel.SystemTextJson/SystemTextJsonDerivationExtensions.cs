using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Provides System.Text.Json domain semantic model derivation entry points.
/// </summary>
public static class SystemTextJsonDerivationExtensions
{
    private const string DotNetMemberNameAnnotation = "dotnet.memberName";

    /// <summary>
    /// Derives a System.Text.Json domain semantic model from the runtime canonical semantic model.
    /// </summary>
    public static SemanticDerivationResult<SystemTextJsonSemanticModel> DeriveSystemTextJsonModel(
        this TypeSchemaModel model,
        Action<SystemTextJsonProjectionOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var options = new SystemTextJsonProjectionOptions();
        configure?.Invoke(options);

        SchemaTransformationPipeline pipeline = options.Transformations ?? SchemaTransformationPipeline.Create().UseCoreDefaults();
        SemanticModelTransformationResult transformed = pipeline.Run(model, options.PipelineOptions, cancellationToken);
        List<SchemaDiagnostic> diagnostics = [.. transformed.Diagnostics];
        Dictionary<TypeId, SystemTextJsonTypeDefinition> types = [];

        foreach (TypeDefinition type in transformed.Model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
        {
            if (type is not ObjectTypeDefinition obj)
            {
                continue;
            }

            List<SystemTextJsonPropertyDefinition> properties = [];
            Dictionary<string, PropertyId> projectedNames = new(StringComparer.Ordinal);
            foreach (PropertyDefinition property in obj.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                var jsonName = GetAnnotationString(property.Annotations, SystemTextJsonAnnotationNames.PropertyName);
                var projectedName = ResolveProjectedName(property.Name, jsonName, options.PropertyNameSource);
                var projected = new SystemTextJsonPropertyDefinition
                {
                    Id = property.Id,
                    SemanticName = property.Name,
                    DotNetMemberName = GetAnnotationString(property.Annotations, DotNetMemberNameAnnotation),
                    SystemTextJsonPropertyName = jsonName,
                    IsExtensionData = GetAnnotationBool(property.Annotations, SystemTextJsonAnnotationNames.ExtensionData),
                    ProjectedJsonName = projectedName,
                };
                properties.Add(projected);

                if (!string.IsNullOrWhiteSpace(projectedName) && !projected.IsExtensionData)
                {
                    if (projectedNames.TryGetValue(projectedName, out PropertyId existing))
                    {
                        diagnostics.Add(CreateDiagnostic(
                            "STJ101",
                            $"System.Text.Json projection produced duplicate JSON property name '{projectedName}' for '{existing.Value}' and '{property.Id.Value}'.",
                            $"/types/{Escape(type.Id.Value)}/properties/{Escape(property.Id.Value)}"));
                    }
                    else
                    {
                        projectedNames.Add(projectedName, property.Id);
                    }
                }

                if (GetAnnotationString(property.Annotations, SystemTextJsonAnnotationNames.Converter) is string converter)
                {
                    diagnostics.Add(CreateDiagnostic(
                        "STJ102",
                        $"System.Text.Json converter metadata '{converter}' is preserved as projection metadata but custom converter behavior is not inferred.",
                        $"/types/{Escape(type.Id.Value)}/properties/{Escape(property.Id.Value)}"));
                }
            }

            types[obj.Id] = new SystemTextJsonTypeDefinition
            {
                Id = obj.Id,
                Name = obj.Name,
                Properties = properties,
            };
        }

        var domainModel = new SystemTextJsonSemanticModel
        {
            TypesById = types,
            Diagnostics = diagnostics,
            PropertyNameSource = options.PropertyNameSource,
            Trace = transformed.Trace,
        };

        return new SemanticDerivationResult<SystemTextJsonSemanticModel>
        {
            Model = domainModel,
            Diagnostics = diagnostics,
            Trace = transformed.Trace,
        };
    }

    private static string? ResolveProjectedName(string semanticName, string? jsonName, SemanticJsonPropertyNameSource source)
    {
        return source switch
        {
            SemanticJsonPropertyNameSource.ExistingJsonContract => null,
            SemanticJsonPropertyNameSource.SystemTextJsonPropertyNameAnnotation => string.IsNullOrWhiteSpace(jsonName) ? null : jsonName,
            SemanticJsonPropertyNameSource.SemanticPropertyName => semanticName,
            _ => throw new InvalidOperationException($"Semantic JSON property name source '{source}' is not supported."),
        };
    }

    private static string? GetAnnotationString(AnnotationBag annotations, string key)
    {
        Annotation? annotation = annotations.Items.FirstOrDefault(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal));
        return annotation?.Value switch
        {
            string value when !string.IsNullOrWhiteSpace(value) => value,
            Type type => type.FullName,
            { } value => value.ToString(),
            _ => null,
        };
    }

    private static bool GetAnnotationBool(AnnotationBag annotations, string key)
    {
        Annotation? annotation = annotations.Items.FirstOrDefault(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal));
        return annotation?.Value is bool value
            ? value
            : annotation?.Value is string text && bool.TryParse(text, out var parsed) && parsed;
    }

    private static SchemaDiagnostic CreateDiagnostic(string code, string message, string modelPath)
    {
        return new SchemaDiagnostic
        {
            Severity = SchemaDiagnosticSeverity.Warning,
            Code = code,
            Message = message,
            Stage = SchemaDiagnosticStage.Projection,
            ModelPath = modelPath,
            ProjectionTarget = ProjectionTarget.SystemTextJson,
        };
    }

    private static string Escape(string value)
    {
        return value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }
}
