#pragma warning disable IDE0058
using System.Globalization;
using System.Text;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Query;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.Core.Inspection;

/// <summary>
/// Provides deterministic human-readable semantic model and diagnostic text inspection helpers.
/// </summary>
public static class SemanticTextExtensions
{
    /// <summary>
    /// Produces deterministic human-readable text for a canonical semantic model.
    /// </summary>
    public static string ToSemanticText(this TypeSchemaModel model)
    {
        return ToSemanticText(model, new SemanticTextOptions());
    }

    /// <summary>
    /// Produces deterministic human-readable text for a canonical semantic model.
    /// </summary>
    public static string ToSemanticText(this TypeSchemaModel model, SemanticTextOptions options, IEnumerable<SchemaDiagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Model {model.Id.Value}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Types: {model.Types.Count}");

        if (options.Detail == SemanticTextDetail.Summary)
        {
            return Normalize(builder);
        }

        builder.AppendLine();
        builder.AppendLine("Types:");
        foreach (TypeDefinition type in model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
        {
            AppendType(builder, type, options);
        }

        if (options.IncludeDiagnostics && diagnostics is not null)
        {
            builder.AppendLine();
            builder.AppendLine("Diagnostics:");
            builder.Append(diagnostics.ToDiagnosticText(new DiagnosticTextOptions { Detail = options.Detail }));
        }

        return Normalize(builder);
    }

    /// <summary>
    /// Produces deterministic human-readable diagnostic text.
    /// </summary>
    public static string ToDiagnosticText(this IEnumerable<SchemaDiagnostic> diagnostics)
    {
        return ToDiagnosticText(diagnostics, new DiagnosticTextOptions());
    }

    /// <summary>
    /// Produces deterministic human-readable diagnostic text.
    /// </summary>
    public static string ToDiagnosticText(this IEnumerable<SchemaDiagnostic> diagnostics, DiagnosticTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder();
        foreach (SchemaDiagnostic diagnostic in SemanticDiagnosticQueryExtensions.Ordered(diagnostics))
        {
            builder.Append(diagnostic.Severity.ToString().ToLowerInvariant());
            builder.Append(' ');
            builder.Append(diagnostic.Code);
            if (!string.IsNullOrWhiteSpace(diagnostic.ModelPath))
            {
                builder.Append(' ');
                builder.Append(diagnostic.ModelPath);
            }

            builder.Append(' ');
            builder.AppendLine(diagnostic.Message);

            if (options.IncludeRelatedPaths && diagnostic.RelatedModelPaths.Count > 0)
            {
                foreach (var path in diagnostic.RelatedModelPaths.OrderBy(static path => path, StringComparer.Ordinal))
                {
                    builder.Append("  related ");
                    builder.AppendLine(path);
                }
            }

            if (options.Detail == SemanticTextDetail.Detailed)
            {
                builder.Append("  stage ");
                builder.AppendLine(diagnostic.Stage.ToString());
                if (!string.IsNullOrWhiteSpace(diagnostic.PipelineStage))
                {
                    builder.Append("  pipelineStage ");
                    builder.AppendLine(diagnostic.PipelineStage);
                }

                if (diagnostic.ProjectionTarget is not null)
                {
                    builder.Append("  projectionTarget ");
                    builder.AppendLine(diagnostic.ProjectionTarget.Value.ToString());
                }
            }

            if (options.IncludeSource && !string.IsNullOrWhiteSpace(diagnostic.Source))
            {
                builder.Append("  source ");
                builder.AppendLine(diagnostic.Source);
            }
        }

        return Normalize(builder);
    }

    private static void AppendType(StringBuilder builder, TypeDefinition type, SemanticTextOptions options)
    {
        builder.Append("  ");
        builder.Append(type.Id.Value);
        builder.Append(" (");
        builder.Append(type.Kind);
        builder.Append(')');
        if (type is ObjectTypeDefinition objectType && objectType.Semantics.Role != EntityRole.Unspecified)
        {
            builder.Append(" [");
            builder.Append(objectType.Semantics.Role);
            builder.Append(']');
        }

        if (type is ObjectTypeDefinition envelopeType && HasBooleanAnnotation(envelopeType.Annotations, CoreSemanticAnnotationKeys.Envelope))
        {
            builder.Append(" [Envelope]");
        }

        if (type is ObjectTypeDefinition versionedType && HasBooleanAnnotation(versionedType.Annotations, CoreSemanticAnnotationKeys.Versioned))
        {
            builder.Append(" [Versioned]");
        }

        if (type is ObjectTypeDefinition temporalType && HasBooleanAnnotation(temporalType.Annotations, CoreSemanticAnnotationKeys.TemporalValidity))
        {
            builder.Append(" [TemporalValidity]");
        }

        builder.AppendLine();

        if (options.Detail == SemanticTextDetail.Detailed)
        {
            AppendOptional(builder, "    Name", type.Name);
            AppendOptional(builder, "    DisplayName", type.DisplayName);
            AppendOptional(builder, "    Description", type.Description);
        }

        if (type is ObjectTypeDefinition obj)
        {
            foreach (KeyDefinition key in obj.Keys.OrderBy(static key => key.Name, StringComparer.Ordinal))
            {
                builder.Append("    Key ");
                builder.Append(key.Name);
                builder.Append(": ");
                builder.Append(key.Kind);
                builder.Append(" (");
                builder.Append(string.Join(", ", key.Properties.Select(static property => property.Id.Value).OrderBy(static id => id, StringComparer.Ordinal)));
                builder.AppendLine(")");
            }

            foreach (PropertyDefinition property in obj.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                builder.Append("    Property ");
                builder.Append(property.Name);
                builder.Append(": ");
                builder.Append(property.Type.Id.Value);
                builder.Append(property.Cardinality.IsRequired ? " required" : " optional");
                if (property.Cardinality.AllowsNull)
                {
                    builder.Append(" nullable");
                }

                if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.EnvelopePayload))
                {
                    builder.Append(" envelopePayload");
                }

                if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.EnvelopeMetadata))
                {
                    builder.Append(" envelopeMetadata");
                }

                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.OwnedObject, "ownedObject");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.OwnedCollection, "ownedCollection");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.Version, "version");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.Revision, "revision");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.CurrentVersion, "currentVersion");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.ValidFrom, "validFrom");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.ValidTo, "validTo");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.LifecycleState, "lifecycleState");
                AppendSemanticFlag(builder, property.Annotations, CoreSemanticAnnotationKeys.ExtensionData, "extensionData");

                builder.AppendLine();

                if (options.IncludeConstraints || options.Detail == SemanticTextDetail.Detailed)
                {
                    AppendConstraints(builder, property.Constraints, "      ");
                }

                if (options.IncludeAnnotations || options.Detail == SemanticTextDetail.Detailed)
                {
                    AppendAnnotations(builder, property.Annotations, "      ");
                }
            }

            foreach (RelationshipDefinition relationship in obj.Relationships.OrderBy(static relationship => relationship.Id.Value, StringComparer.Ordinal))
            {
                builder.Append("    Relationship ");
                builder.Append(relationship.Id.Value);
                builder.Append(": ");
                builder.Append(relationship.DependentType.Id.Value);
                builder.Append(" -> ");
                builder.Append(relationship.PrincipalType.Id.Value);
                builder.Append(' ');
                builder.AppendLine(relationship.Cardinality.ToString());
            }
        }

        if (options.IncludeAnnotations || options.Detail == SemanticTextDetail.Detailed)
        {
            AppendAnnotations(builder, type.Annotations, "    ");
        }
    }

    private static bool HasBooleanAnnotation(AnnotationBag bag, string key)
    {
        return bag.Items
            .Where(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal))
            .Select(static annotation => annotation.Value?.ToString())
            .LastOrDefault(static value => !string.IsNullOrWhiteSpace(value))
            is string value
            && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendSemanticFlag(StringBuilder builder, AnnotationBag annotations, string key, string label)
    {
        if (HasBooleanAnnotation(annotations, key))
        {
            builder.Append(' ');
            builder.Append(label);
        }
    }

    private static void AppendAnnotations(StringBuilder builder, AnnotationBag annotations, string indent)
    {
        foreach (Annotation annotation in annotations.Items.OrderBy(static annotation => annotation.Key.Value, StringComparer.Ordinal).ThenBy(static annotation => Convert.ToString(annotation.Value, CultureInfo.InvariantCulture), StringComparer.Ordinal))
        {
            builder.Append(indent);
            builder.Append("Annotation ");
            builder.Append(annotation.Key.Value);
            builder.Append('=');
            builder.AppendLine(Convert.ToString(annotation.Value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static void AppendConstraints(StringBuilder builder, ConstraintSet constraints, string indent)
    {
        AppendConstraint(builder, indent, "string.minLength", constraints.String?.MinLength);
        AppendConstraint(builder, indent, "string.maxLength", constraints.String?.MaxLength);
        AppendConstraint(builder, indent, "string.pattern", constraints.String?.Pattern);
        AppendConstraint(builder, indent, "numeric.minimum", constraints.Numeric?.Minimum);
        AppendConstraint(builder, indent, "numeric.maximum", constraints.Numeric?.Maximum);
        AppendConstraint(builder, indent, "numeric.multipleOf", constraints.Numeric?.MultipleOf);
        AppendConstraint(builder, indent, "array.minItems", constraints.Array?.MinItems);
        AppendConstraint(builder, indent, "array.maxItems", constraints.Array?.MaxItems);
        if (constraints.Array?.UniqueItems == true)
        {
            AppendConstraint(builder, indent, "array.uniqueItems", true);
        }

        foreach (CustomConstraint custom in constraints.Custom.OrderBy(static custom => custom.Name, StringComparer.Ordinal))
        {
            AppendConstraint(builder, indent, "custom." + custom.Name, custom.Value);
        }
    }

    private static void AppendConstraint(StringBuilder builder, string indent, string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        builder.Append(indent);
        builder.Append("Constraint ");
        builder.Append(name);
        builder.Append('=');
        builder.AppendLine(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    private static void AppendOptional(StringBuilder builder, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(label);
        builder.Append(' ');
        builder.AppendLine(value);
    }

    private static string Normalize(StringBuilder builder)
    {
        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

#pragma warning restore IDE0058
