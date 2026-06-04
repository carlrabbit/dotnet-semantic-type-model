#pragma warning disable IDE0058
using System.Globalization;
using System.Text;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Query;
using LegacyModel = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Inspection;

/// <summary>
/// Provides deterministic human-readable semantic model and diagnostic text inspection helpers.
/// </summary>
public static class SemanticTextExtensions
{
    /// <summary>
    /// Produces deterministic human-readable text for a hardened semantic type model.
    /// </summary>
    public static string ToSemanticText(this TypeSchemaModel model)
    {
        return ToSemanticText(model, new SemanticTextOptions());
    }

    /// <summary>
    /// Produces deterministic human-readable text for a hardened semantic type model.
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
    /// Produces deterministic human-readable text for a legacy semantic type model.
    /// </summary>
    public static string ToSemanticText(this LegacyModel.TypeSchemaModel model)
    {
        return ToSemanticText(model, new SemanticTextOptions());
    }

    /// <summary>
    /// Produces deterministic human-readable text for a legacy semantic type model.
    /// </summary>
    public static string ToSemanticText(this LegacyModel.TypeSchemaModel model, SemanticTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder();
        builder.AppendLine("Model TypeSchemaModel");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Root: {model.RootIdentifier ?? "<none>"}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Types: {model.Shapes.Count}");

        if (options.Detail == SemanticTextDetail.Summary)
        {
            return Normalize(builder);
        }

        builder.AppendLine();
        builder.AppendLine("Types:");
        foreach ((var id, LegacyModel.TypeShape shape) in model.Shapes.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            AppendLegacyShape(builder, id, shape, options);
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

    private static void AppendLegacyShape(StringBuilder builder, string id, LegacyModel.TypeShape shape, SemanticTextOptions options)
    {
        builder.Append("  ");
        builder.Append(id);
        builder.Append(" (");
        builder.Append(shape.GetType().Name.Replace("Shape", string.Empty, StringComparison.Ordinal));
        builder.AppendLine(")");

        if (shape is LegacyModel.ObjectShape obj)
        {
            foreach (LegacyModel.PropertyShape property in obj.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                builder.Append("    Property ");
                builder.Append(property.Name);
                builder.Append(": ");
                builder.Append(property.Type?.Identifier ?? property.Type?.Inline?.Identifier ?? "<inline>");
                builder.Append(property.IsRequired ? " required" : " optional");
                if (property.IsNullable)
                {
                    builder.Append(" nullable");
                }

                builder.AppendLine();

                if (options.IncludeAnnotations || options.Detail == SemanticTextDetail.Detailed)
                {
                    AppendLegacyAnnotations(builder, property.Annotations, "      ");
                }
            }
        }

        if (options.IncludeConstraints || options.Detail == SemanticTextDetail.Detailed)
        {
            foreach (LegacyModel.ConstraintEntry constraint in shape.Constraints.Entries.OrderBy(static constraint => constraint.Key, StringComparer.Ordinal))
            {
                builder.Append("    Constraint ");
                builder.Append(constraint.Key);
                builder.Append('=');
                builder.AppendLine(constraint.Value);
            }
        }

        if (options.IncludeAnnotations || options.Detail == SemanticTextDetail.Detailed)
        {
            AppendLegacyAnnotations(builder, shape.Annotations, "    ");
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

    private static void AppendLegacyAnnotations(StringBuilder builder, IReadOnlyList<LegacyModel.SchemaAnnotation> annotations, string indent)
    {
        foreach (LegacyModel.SchemaAnnotation annotation in annotations.OrderBy(static annotation => annotation.Key, StringComparer.Ordinal).ThenBy(static annotation => annotation.Value, StringComparer.Ordinal))
        {
            builder.Append(indent);
            builder.Append("Annotation ");
            builder.Append(annotation.Key);
            builder.Append('=');
            builder.AppendLine(annotation.Value);
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
