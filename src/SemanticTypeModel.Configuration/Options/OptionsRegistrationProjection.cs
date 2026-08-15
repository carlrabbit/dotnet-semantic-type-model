#pragma warning disable CS1591
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Configuration;

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
