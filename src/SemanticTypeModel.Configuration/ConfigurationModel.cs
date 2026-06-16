#pragma warning disable CS1591
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.Configuration;

public static class ConfigurationAnnotationKeys
{
    public const string Options = "configuration.options";
    public const string Section = "configuration.section";
    public const string SectionName = "configuration.section.name";
    public const string Bind = "configuration.bind";
    public const string BindPolicy = "configuration.bind.policy";
    public const string NamedOptions = "configuration.namedOptions";
    public const string NamedOptionsName = "configuration.namedOptions.name";
    public const string ValidateDataAnnotations = "configuration.validateDataAnnotations";
    public const string ValidateOnStart = "configuration.validateOnStart";
    public const string RegistrationGenerateExtensionMethod = "configuration.registration.generateExtensionMethod";
    public const string RegistrationExtensionMethodName = "configuration.registration.extensionMethodName";
}

public sealed record RequiredWhenConstraint(string TargetProperty, string SourceProperty, string Operator, string Value, string? Message);
public sealed record ConfigurationSemanticModel(IReadOnlyList<ConfigurationType> ConfigurationTypes, IReadOnlyList<SchemaDiagnostic> Diagnostics);
public sealed record ConfigurationType(string OptionsClrType, string? Section, string BindPolicy, string? NamedOptionsName, bool ValidateDataAnnotations, bool ValidateOnStart, bool GenerateExtensionMethod, string? ExtensionMethodName, IReadOnlyList<ConfigurationProperty> Properties, IReadOnlyList<RequiredWhenConstraint> RequiredWhenConstraints, IReadOnlyList<SchemaDiagnostic> Diagnostics);
public sealed record ConfigurationProperty(string Name, TypeRef Type, bool IsRequired, bool AllowsNull, IReadOnlyDictionary<string, string> Annotations);

public static class ConfigurationDerivationExtensions
{
    public static ConfigurationSemanticModel DeriveConfigurationModel(this TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var types = new List<ConfigurationType>();
        var diagnostics = new List<SchemaDiagnostic>();
        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>().OrderBy(static t => t.Id.Value, StringComparer.Ordinal))
        {
            if (!IsConfigurationType(objectType))
            {
                continue;
            }
            var typeDiagnostics = new List<SchemaDiagnostic>();
            var section = Get(objectType.Annotations, ConfigurationAnnotationKeys.SectionName);
            if (string.IsNullOrWhiteSpace(section))
            {
                typeDiagnostics.Add(Diagnostic("STM1025", $"Configuration type '{objectType.Name}' does not declare a configuration section name.", $"/types/{objectType.Id.Value}", ConfigurationAnnotationKeys.SectionName));
            }
            var rules = new List<RequiredWhenConstraint>();
            foreach (PropertyDefinition property in objectType.Properties.OrderBy(static p => p.Name, StringComparer.Ordinal))
            {
                if (TryReadRequiredWhen(property, out RequiredWhenConstraint? rule))
                {
                    if (!objectType.Properties.Any(p => string.Equals(p.Name, rule!.SourceProperty, StringComparison.Ordinal) || string.Equals(Get(p.Annotations, "dotnet.memberName"), rule!.SourceProperty, StringComparison.Ordinal)))
                    {
                        typeDiagnostics.Add(Diagnostic("STM1020", $"RequiredWhen source property '{rule!.SourceProperty}' could not be resolved on '{objectType.Name}'.", $"/types/{objectType.Id.Value}/properties/{property.Name}", CoreSemanticAnnotationKeys.RequiredWhenSource));
                    }
                    rules.Add(rule!);
                }
            }
            var configurationType = new ConfigurationType(
                objectType.Id.Value,
                section,
                Get(objectType.Annotations, ConfigurationAnnotationKeys.BindPolicy) ?? "Section",
                Get(objectType.Annotations, ConfigurationAnnotationKeys.NamedOptionsName),
                Has(objectType.Annotations, ConfigurationAnnotationKeys.ValidateDataAnnotations),
                Has(objectType.Annotations, ConfigurationAnnotationKeys.ValidateOnStart),
                Has(objectType.Annotations, ConfigurationAnnotationKeys.RegistrationGenerateExtensionMethod),
                Get(objectType.Annotations, ConfigurationAnnotationKeys.RegistrationExtensionMethodName),
                [.. objectType.Properties.OrderBy(static p => p.Name, StringComparer.Ordinal).Select(static p => new ConfigurationProperty(p.Name, p.Type, p.Cardinality.IsRequired, p.Cardinality.AllowsNull, ToStringDictionary(p.Annotations)))],
                rules,
                typeDiagnostics);
            types.Add(configurationType);
            diagnostics.AddRange(typeDiagnostics);
        }
        return new ConfigurationSemanticModel(types, diagnostics);
    }

    public static string Inspect(this ConfigurationSemanticModel model)
    {
        StringBuilder b = new();
        _ = b.AppendLine("configurationModel");
        foreach (ConfigurationType type in model.ConfigurationTypes.OrderBy(static t => t.OptionsClrType, StringComparer.Ordinal))
        {
            _ = b.AppendLine(CultureInfo.InvariantCulture, $"  options {type.OptionsClrType} section={type.Section ?? "<missing>"} bind={type.BindPolicy} validateDataAnnotations={type.ValidateDataAnnotations} validateOnStart={type.ValidateOnStart}");
            foreach (RequiredWhenConstraint rule in type.RequiredWhenConstraints.OrderBy(static r => r.TargetProperty, StringComparer.Ordinal))
            {
                _ = b.AppendLine(CultureInfo.InvariantCulture, $"    requiredWhen target={rule.TargetProperty} source={rule.SourceProperty} operator={rule.Operator} value={rule.Value} message={rule.Message ?? "<none>"}");
            }
        }
        return b.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static bool IsConfigurationType(ObjectTypeDefinition type)
    {
        return string.Equals(Get(type.Annotations, "schema.role"), "Configuration", StringComparison.Ordinal) || Has(type.Annotations, ConfigurationAnnotationKeys.Options) || Has(type.Annotations, ConfigurationAnnotationKeys.Section) || Get(type.Annotations, ConfigurationAnnotationKeys.SectionName) is not null;
    }

    private static bool TryReadRequiredWhen(PropertyDefinition property, out RequiredWhenConstraint? rule)
    {
        var source = Get(property.Annotations, CoreSemanticAnnotationKeys.RequiredWhenSource);
        var value = Get(property.Annotations, CoreSemanticAnnotationKeys.RequiredWhenValue);
        if (source is null || value is null)
        {
            rule = null;
            return false;
        }
        rule = new RequiredWhenConstraint(property.Name, source, Get(property.Annotations, CoreSemanticAnnotationKeys.RequiredWhenOperator) ?? "equals", value, Get(property.Annotations, CoreSemanticAnnotationKeys.RequiredWhenMessage));
        return true;
    }
    private static SchemaDiagnostic Diagnostic(string code, string message, string path, string source)
    {
        return new() { Severity = SchemaDiagnosticSeverity.Warning, Code = code, Message = message, Stage = SchemaDiagnosticStage.Transformation, ModelPath = path, Source = source };
    }

    private static bool Has(AnnotationBag bag, string key)
    {
        return string.Equals(Get(bag, key), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Get(AnnotationBag bag, string key)
    {
        return bag.Items.FirstOrDefault(a => string.Equals(a.Key.Value, key, StringComparison.Ordinal))?.Value?.ToString();
    }

    private static Dictionary<string, string> ToStringDictionary(AnnotationBag bag)
    {
        return bag.Items.OrderBy(static a => a.Key.Value, StringComparer.Ordinal).ToDictionary(static a => a.Key.Value, static a => a.Value?.ToString() ?? string.Empty, StringComparer.Ordinal);
    }
}

public static class OptionsRegistrationProjection
{
    public static IServiceCollection AddSemanticConfigurationOptions(this IServiceCollection services, ConfigurationSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(model);
        return services;
    }
}
