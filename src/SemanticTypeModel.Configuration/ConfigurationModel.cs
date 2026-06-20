#pragma warning disable CS1591
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.Configuration;

public static class ConfigurationAnnotationKeys
{
    public const string Options = "configuration.options";
    public const string Section = "configuration.section";
    public const string SectionName = "configuration.section.name";
    public const string SectionPresence = "configuration.section.presence";
    public const string Bind = "configuration.bind";
    public const string BindPolicy = "configuration.bind.policy";
    public const string NamedOptions = "configuration.namedOptions";
    public const string NamedOptionsName = "configuration.namedOptions.name";
    public const string ValidateDataAnnotations = "configuration.validateDataAnnotations";
    public const string ValidateOnStart = "configuration.validateOnStart";
    public const string RegistrationGenerateExtensionMethod = "configuration.registration.generateExtensionMethod";
    public const string RegistrationExtensionMethodName = "configuration.registration.extensionMethodName";
}

public enum ConfigurationSectionPresence
{
    Optional,
    Required,
}

public sealed class SemanticOptionsRegistration
{
    public string? Name { get; set; }
    public string? SectionName { get; set; }
    public bool? ValidateOnStart { get; set; }
    public ConfigurationSectionPresence? SectionPresence { get; set; }
}

public sealed record ConfigurationTypeResult(ConfigurationType? Type, IReadOnlyList<SchemaDiagnostic> Diagnostics);

public sealed record RequiredWhenConstraint(string TargetProperty, string SourceProperty, string Operator, string Value, string? Message);
public sealed record ConfigurationSemanticModel(IReadOnlyList<ConfigurationType> ConfigurationTypes, IReadOnlyList<SchemaDiagnostic> Diagnostics);
public sealed record ConfigurationType(string OptionsClrType, string? Section, ConfigurationSectionPresence SectionPresence, string BindPolicy, string? NamedOptionsName, bool ValidateDataAnnotations, bool ValidateOnStart, bool GenerateExtensionMethod, string? ExtensionMethodName, IReadOnlyList<ConfigurationProperty> Properties, IReadOnlyList<RequiredWhenConstraint> RequiredWhenConstraints, IReadOnlyList<SchemaDiagnostic> Diagnostics);
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
            var bindPolicy = Get(objectType.Annotations, ConfigurationAnnotationKeys.BindPolicy) ?? "Section";
            ConfigurationSectionPresence sectionPresence = ReadSectionPresence(objectType, typeDiagnostics);
            if (string.IsNullOrWhiteSpace(section))
            {
                typeDiagnostics.Add(Diagnostic("STM1025", $"Configuration type '{objectType.Name}' does not declare a configuration section name.", $"/types/{objectType.Id.Value}", ConfigurationAnnotationKeys.SectionName));
            }
            if (sectionPresence == ConfigurationSectionPresence.Required && string.IsNullOrWhiteSpace(section))
            {
                typeDiagnostics.Add(Error("STM1030", $"Configuration type '{objectType.Name}' declares required section presence without a section name.", $"/types/{objectType.Id.Value}", ConfigurationAnnotationKeys.SectionPresence));
            }
            if (sectionPresence == ConfigurationSectionPresence.Required && string.Equals(section, ":", StringComparison.Ordinal))
            {
                typeDiagnostics.Add(Error("STM1031", $"Configuration type '{objectType.Name}' declares required section presence for root binding.", $"/types/{objectType.Id.Value}", ConfigurationAnnotationKeys.SectionPresence));
            }
            if (sectionPresence == ConfigurationSectionPresence.Required && string.Equals(bindPolicy, "None", StringComparison.OrdinalIgnoreCase))
            {
                typeDiagnostics.Add(Error("STM1032", $"Configuration type '{objectType.Name}' declares required section presence while binding is disabled.", $"/types/{objectType.Id.Value}", ConfigurationAnnotationKeys.SectionPresence));
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
                sectionPresence,
                bindPolicy,
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

    public static ConfigurationTypeResult DeriveConfigurationType<TOptions>(this TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var fullName = typeof(TOptions).FullName;
        var name = typeof(TOptions).Name;
        List<ObjectTypeDefinition> matches = [.. model.Types.OfType<ObjectTypeDefinition>().Where(t =>
            string.Equals(t.Id.Value, fullName, StringComparison.Ordinal) ||
            string.Equals(t.Id.Value, name, StringComparison.Ordinal) ||
            string.Equals(t.Name, fullName, StringComparison.Ordinal) ||
            string.Equals(t.Name, name, StringComparison.Ordinal) ||
            string.Equals(Get(t.Annotations, "dotnet.clrType"), fullName, StringComparison.Ordinal) ||
            string.Equals(Get(t.Annotations, "dotnet.metadataName"), fullName, StringComparison.Ordinal))];
        if (matches.Count == 0)
        {
            return new ConfigurationTypeResult(null, [Error("STM1034", $"Options type '{typeof(TOptions).FullName}' was not found in the semantic model.", "/types", "clrType")]);
        }
        if (matches.Count > 1)
        {
            return new ConfigurationTypeResult(null, [Error("STM1035", $"Options type '{typeof(TOptions).FullName}' matched multiple semantic model types.", "/types", "clrType")]);
        }
        if (!IsConfigurationType(matches[0]))
        {
            return new ConfigurationTypeResult(null, [Error("STM1036", $"Options type '{typeof(TOptions).FullName}' is not a Configuration type.", $"/types/{matches[0].Id.Value}", ConfigurationAnnotationKeys.Options)]);
        }
        ConfigurationSemanticModel derived = new TypeSchemaModel { Id = model.Id, Types = [matches[0]], TypesById = new Dictionary<TypeId, TypeDefinition> { [matches[0].Id] = matches[0] }, Annotations = model.Annotations }.DeriveConfigurationModel();
        return new ConfigurationTypeResult(derived.ConfigurationTypes.SingleOrDefault(), derived.Diagnostics);
    }

    public static string Inspect(this ConfigurationSemanticModel model)
    {
        StringBuilder b = new();
        _ = b.AppendLine("configurationModel");
        foreach (ConfigurationType type in model.ConfigurationTypes.OrderBy(static t => t.OptionsClrType, StringComparer.Ordinal))
        {
            _ = b.AppendLine(CultureInfo.InvariantCulture, $"  options {type.OptionsClrType} section={type.Section ?? "<missing>"} presence={type.SectionPresence} bind={type.BindPolicy} validateDataAnnotations={type.ValidateDataAnnotations} validateOnStart={type.ValidateOnStart}");
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
    private static ConfigurationSectionPresence ReadSectionPresence(ObjectTypeDefinition type, List<SchemaDiagnostic> diagnostics)
    {
        var value = Get(type.Annotations, ConfigurationAnnotationKeys.SectionPresence);
        if (string.IsNullOrWhiteSpace(value))
        {
            return ConfigurationSectionPresence.Optional;
        }
        if (Enum.TryParse(value, ignoreCase: true, out ConfigurationSectionPresence presence) && Enum.IsDefined(presence))
        {
            return presence;
        }
        diagnostics.Add(Error("STM1033", $"Configuration type '{type.Name}' declares unsupported section presence '{value}'.", $"/types/{type.Id.Value}", ConfigurationAnnotationKeys.SectionPresence));
        return ConfigurationSectionPresence.Optional;
    }

    private static SchemaDiagnostic Diagnostic(string code, string message, string path, string source)
    {
        return new() { Severity = SchemaDiagnosticSeverity.Warning, Code = code, Message = message, Stage = SchemaDiagnosticStage.Transformation, ModelPath = path, Source = source };
    }

    private static SchemaDiagnostic Error(string code, string message, string path, string source)
    {
        return new() { Severity = SchemaDiagnosticSeverity.Error, Code = code, Message = message, Stage = SchemaDiagnosticStage.Transformation, ModelPath = path, Source = source };
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
    public static OptionsBuilder<TOptions> AddSemanticOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, TypeSchemaModel model, Action<SemanticOptionsRegistration>? configure = null)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(model);

        ConfigurationTypeResult result = model.DeriveConfigurationType<TOptions>();
        if (result.Type is null || result.Diagnostics.Any(static d => d.Severity == SchemaDiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("Semantic options registration failed: " + string.Join("; ", result.Diagnostics.Select(static d => $"{d.Code}: {d.Message}")));
        }

        SemanticOptionsRegistration registration = new();
        configure?.Invoke(registration);
        ConfigurationType type = result.Type;
        var name = registration.Name ?? type.NamedOptionsName;
        var sectionName = registration.SectionName ?? type.Section;
        ConfigurationSectionPresence presence = Strengthen(type.SectionPresence, registration.SectionPresence);
        var validateOnStart = registration.ValidateOnStart ?? type.ValidateOnStart;
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new InvalidOperationException($"Configuration type '{type.OptionsClrType}' does not declare a section name.");
        }
        if (presence == ConfigurationSectionPresence.Required && string.Equals(sectionName, ":", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Configuration type '{type.OptionsClrType}' cannot require root configuration section presence.");
        }
        if (!string.Equals(type.BindPolicy, "Section", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Configuration type '{type.OptionsClrType}' declares unsupported bind policy '{type.BindPolicy}'.");
        }

        IConfigurationSection section = configuration.GetSection(sectionName);
        OptionsBuilder<TOptions> builder = name is null ? services.AddOptions<TOptions>() : services.AddOptions<TOptions>(name);
        _ = builder.Bind(section);
        if (presence == ConfigurationSectionPresence.Required)
        {
            _ = builder.Validate(_ => SectionHasEffectiveData(section), $"Configuration section '{sectionName}' is required.");
        }
        if (type.ValidateDataAnnotations)
        {
            _ = builder.ValidateDataAnnotations();
        }
        foreach (RequiredWhenConstraint rule in type.RequiredWhenConstraints)
        {
            _ = builder.Validate(options => ValidateRequiredWhen(options, rule), rule.Message ?? $"Configuration value '{rule.TargetProperty}' is required when '{rule.SourceProperty}' equals '{rule.Value}'.");
        }
        if (validateOnStart)
        {
            _ = builder.ValidateOnStart();
        }
        return builder;
    }

    [Obsolete("Register selected options types with AddSemanticOptions<TOptions>(IServiceCollection, IConfiguration, TypeSchemaModel, Action<SemanticOptionsRegistration>?) instead.")]
    public static IServiceCollection AddSemanticConfigurationOptions(this IServiceCollection services, ConfigurationSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(model);
        return services;
    }

    private static ConfigurationSectionPresence Strengthen(ConfigurationSectionPresence modelPresence, ConfigurationSectionPresence? overridePresence)
    {
        return overridePresence switch
        {
            null => modelPresence,
            _ when overridePresence == modelPresence => modelPresence,
            ConfigurationSectionPresence.Required when modelPresence == ConfigurationSectionPresence.Optional => ConfigurationSectionPresence.Required,
            ConfigurationSectionPresence.Optional => throw new InvalidOperationException("Call-site section-presence overrides may only strengthen Optional to Required."),
            ConfigurationSectionPresence.Required => throw new InvalidOperationException("Call-site section-presence overrides may only strengthen Optional to Required."),
            _ => throw new InvalidOperationException("Call-site section-presence overrides may only strengthen Optional to Required."),
        };
    }

    private static bool SectionHasEffectiveData(IConfigurationSection section)
    {
        return section.Value is not null || section.GetChildren().Any(SectionHasEffectiveData);
    }

    private static bool ValidateRequiredWhen<TOptions>(TOptions options, RequiredWhenConstraint rule)
    {
        Type type = typeof(TOptions);
        var source = type.GetProperty(rule.SourceProperty)?.GetValue(options);
        if (!string.Equals(Convert.ToString(source, CultureInfo.InvariantCulture), rule.Value, StringComparison.Ordinal))
        {
            return true;
        }
        var target = type.GetProperty(rule.TargetProperty)?.GetValue(options);
        return target switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true,
        };
    }
}
