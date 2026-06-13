#pragma warning disable IDE0046
using System.Linq.Expressions;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Inspection;

namespace SemanticTypeModel.Core.Query;

/// <summary>
/// Provides deterministic query and assertion helpers for semantic model diagnostics.
/// </summary>
public static class SemanticDiagnosticQueryExtensions
{
    /// <summary>
    /// Returns true when any diagnostic has error severity.
    /// </summary>
    public static bool HasErrors(this IEnumerable<SchemaDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
    }

    /// <summary>
    /// Returns diagnostics with error severity in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> Errors(this IEnumerable<SchemaDiagnostic> diagnostics)
    {
        return Ordered(diagnostics).Where(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
    }

    /// <summary>
    /// Returns diagnostics with warning severity in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> Warnings(this IEnumerable<SchemaDiagnostic> diagnostics)
    {
        return Ordered(diagnostics).Where(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Returns diagnostics with the supplied severity in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> WithSeverity(this IEnumerable<SchemaDiagnostic> diagnostics, SchemaDiagnosticSeverity severity)
    {
        return Ordered(diagnostics).Where(diagnostic => diagnostic.Severity == severity);
    }

    /// <summary>
    /// Returns diagnostics with the supplied diagnostic code in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> WithCode(this IEnumerable<SchemaDiagnostic> diagnostics, string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        return Ordered(diagnostics).Where(diagnostic => string.Equals(diagnostic.Code, code, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns diagnostics emitted by the supplied stage in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> WithStage(this IEnumerable<SchemaDiagnostic> diagnostics, SchemaDiagnosticStage stage)
    {
        return Ordered(diagnostics).Where(diagnostic => diagnostic.Stage == stage);
    }

    /// <summary>
    /// Returns diagnostics for the supplied canonical model path in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> ForPath(this IEnumerable<SchemaDiagnostic> diagnostics, string modelPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelPath);
        return Ordered(diagnostics).Where(diagnostic => string.Equals(diagnostic.ModelPath, modelPath, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns diagnostics for the supplied CLR type in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> ForType<T>(this IEnumerable<SchemaDiagnostic> diagnostics)
    {
        return ForPath(diagnostics, ModelPath.ForType(new TypeId(GetClrTypeIdentifier(typeof(T)))));
    }

    /// <summary>
    /// Returns diagnostics for the supplied CLR property in deterministic order.
    /// </summary>
    public static IEnumerable<SchemaDiagnostic> ForProperty<T>(this IEnumerable<SchemaDiagnostic> diagnostics, Expression<Func<T, object?>> propertyExpression)
    {
        var propertyName = GetSimplePropertyName(propertyExpression);
        return ForPath(diagnostics, ModelPath.ForProperty(new TypeId(GetClrTypeIdentifier(typeof(T))), propertyName));
    }

    /// <summary>
    /// Throws an exception containing deterministic diagnostic text when any error diagnostic is present.
    /// </summary>
    public static void ThrowIfErrors(this IEnumerable<SchemaDiagnostic> diagnostics)
    {
        ThrowIfErrors(diagnostics, new DiagnosticTextOptions());
    }

    /// <summary>
    /// Throws an exception containing deterministic diagnostic text when any error diagnostic is present.
    /// </summary>
    public static void ThrowIfErrors(this IEnumerable<SchemaDiagnostic> diagnostics, DiagnosticTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(options);
        SchemaDiagnostic[] errors = [.. diagnostics.Errors()];
        if (errors.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException("Semantic type model diagnostics contain errors." + Environment.NewLine + errors.ToDiagnosticText(options));
    }

    internal static IEnumerable<SchemaDiagnostic> Ordered(IEnumerable<SchemaDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return diagnostics
            .OrderBy(static diagnostic => diagnostic.ModelPath ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Severity)
            .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal);
    }

    private static string GetSimplePropertyName<T>(Expression<Func<T, object?>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        Expression body = propertyExpression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Expression: ParameterExpression, Member.MemberType: System.Reflection.MemberTypes.Property } member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException($"Property expression '{propertyExpression}' is unsupported. Use a simple property access expression such as x => x.Email; method calls, anonymous objects, and nested property paths are not supported.", nameof(propertyExpression));
    }

    private static string GetClrTypeIdentifier(Type type)
    {
        if (!string.IsNullOrEmpty(type.FullName))
        {
            return "global::" + type.FullName.Replace('+', '.');
        }

        return "global::" + type.Name;
    }
}

#pragma warning restore IDE0046
