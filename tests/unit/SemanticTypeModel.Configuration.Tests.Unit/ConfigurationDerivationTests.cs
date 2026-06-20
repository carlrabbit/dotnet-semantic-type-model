#pragma warning disable CS1591
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.Configuration.Tests.Unit;

public sealed class ConfigurationDerivationTests
{
    [Test]
    public async Task DeriveConfigurationModelReadsSectionAndRequiredWhen()
    {
        TypeSchemaModel model = BuildModel(Scalar("String"), Scalar("Provider"), Object("ColdStorageOptions", [
            Property("Provider", new TypeId("Provider")),
            Property("TargetFilePath", new TypeId("String"), Annotations(
                Annotation(CoreSemanticAnnotationKeys.RequiredWhen, "true"),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenSource, "Provider"),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenOperator, "equals"),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenValue, "File")))
        ], Annotations(
            Annotation("schema.role", "Configuration"),
            Annotation(ConfigurationAnnotationKeys.SectionName, "ColdStorage"),
            Annotation(ConfigurationAnnotationKeys.ValidateOnStart, "true"),
            Annotation(ConfigurationAnnotationKeys.ValidateDataAnnotations, "true"))));

        ConfigurationSemanticModel configuration = model.DeriveConfigurationModel();

        _ = await Assert.That(configuration.ConfigurationTypes).Count().IsEqualTo(1);
        ConfigurationType options = configuration.ConfigurationTypes.Single();
        _ = await Assert.That(options.Section).IsEqualTo("ColdStorage");
        _ = await Assert.That(options.ValidateOnStart).IsTrue();
        _ = await Assert.That(options.RequiredWhenConstraints.Single().TargetProperty).IsEqualTo("TargetFilePath");
        _ = await Assert.That(configuration.Inspect()).Contains("requiredWhen target=TargetFilePath source=Provider operator=equals value=File");
    }


    [Test]
    public async Task DeriveConfigurationModelDefaultsAndInspectsSectionPresence()
    {
        TypeSchemaModel model = BuildModel(Object("OptionalOptions", [], Annotations(
            Annotation("schema.role", "Configuration"),
            Annotation(ConfigurationAnnotationKeys.SectionName, "Optional"))));

        ConfigurationType options = model.DeriveConfigurationModel().ConfigurationTypes.Single();

        _ = await Assert.That(options.SectionPresence).IsEqualTo(ConfigurationSectionPresence.Optional);
        _ = await Assert.That(model.DeriveConfigurationModel().Inspect()).Contains("presence=Optional");
    }

    [Test]
    public async Task DeriveConfigurationTypeSelectsOnlyRequestedOptionsType()
    {
        TypeSchemaModel model = BuildModel(
            Object(nameof(ColdStorageOptions), [], Annotations(Annotation("schema.role", "Configuration"), Annotation(ConfigurationAnnotationKeys.SectionName, "ColdStorage"))),
            Object("UnusedOptions", [], Annotations(Annotation("schema.role", "Configuration"), Annotation(ConfigurationAnnotationKeys.SectionName, "Unused"))));

        ConfigurationTypeResult result = model.DeriveConfigurationType<ColdStorageOptions>();

        _ = await Assert.That(result.Type).IsNotNull();
        _ = await Assert.That(result.Type!.OptionsClrType).IsEqualTo(nameof(ColdStorageOptions));
    }

    [Test]
    public async Task RequiredSectionPresenceWithoutSectionNameProducesErrorDiagnostic()
    {
        TypeSchemaModel model = BuildModel(Object(nameof(ColdStorageOptions), [], Annotations(
            Annotation("schema.role", "Configuration"),
            Annotation(ConfigurationAnnotationKeys.SectionPresence, "Required"))));

        ConfigurationTypeResult result = model.DeriveConfigurationType<ColdStorageOptions>();

        _ = await Assert.That(result.Diagnostics.Any(d => d.Code == "STM1030" && d.Severity == SchemaDiagnosticSeverity.Error)).IsTrue();
    }

    private sealed class ColdStorageOptions
    {
        public string? Provider { get; init; }
        public string? TargetFilePath { get; init; }
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new() { Id = new SchemaModelId("ColdStorageOptions"), Types = types, TypesById = types.ToDictionary(static t => t.Id, static t => t), Annotations = new AnnotationBag() };
    }

    private static ScalarTypeDefinition Scalar(string id)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = ScalarKind.String };
    }

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties, AnnotationBag annotations)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = annotations, Properties = properties, Keys = [], Relationships = [] };
    }

    private static PropertyDefinition Property(string name, TypeId type, AnnotationBag? annotations = null)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(type), Cardinality = new Cardinality(), Mutability = Mutability.InitOnly, Constraints = new ConstraintSet(), Annotations = annotations ?? new AnnotationBag() };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new() { Items = annotations };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new() { Key = new AnnotationKey(key), Value = value, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared };
    }
}

public sealed class ConfigurationRegistrationTests
{
    [Test]
    public async Task AddSemanticOptionsRegistersOnlySelectedOptionsTypeAndReturnsBuilder()
    {
        TypeSchemaModel model = BuildModel(
            Object(nameof(ColdStorageOptions), [], Annotations(Annotation("schema.role", "Configuration"), Annotation(ConfigurationAnnotationKeys.SectionName, "ColdStorage"))),
            Object(nameof(UnusedOptions), [], Annotations(Annotation("schema.role", "Configuration"), Annotation(ConfigurationAnnotationKeys.SectionName, "Unused"))));
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ColdStorage:Provider"] = "File", ["Unused:Value"] = "ignored" })
            .Build();
        var services = new ServiceCollection();

        Microsoft.Extensions.Options.OptionsBuilder<ColdStorageOptions> builder = services.AddSemanticOptions<ColdStorageOptions>(configuration, model);
        using ServiceProvider provider = services.BuildServiceProvider();

        _ = await Assert.That(builder).IsNotNull();
        _ = await Assert.That(provider.GetService<Microsoft.Extensions.Options.IConfigureOptions<UnusedOptions>>()).IsNull();
        _ = await Assert.That(provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ColdStorageOptions>>().Value.Provider).IsEqualTo("File");
    }

    [Test]
    public async Task RequiredSectionPresenceFailsThroughOptionsValidationWhenEffectiveDataIsMissing()
    {
        TypeSchemaModel model = BuildModel(Object(nameof(ColdStorageOptions), [], Annotations(
            Annotation("schema.role", "Configuration"),
            Annotation(ConfigurationAnnotationKeys.SectionName, "ColdStorage"),
            Annotation(ConfigurationAnnotationKeys.SectionPresence, "Required"))));
        var services = new ServiceCollection();
        _ = services.AddSemanticOptions<ColdStorageOptions>(new ConfigurationBuilder().Build(), model);
        using ServiceProvider provider = services.BuildServiceProvider();

        void Resolve()
        {
            _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ColdStorageOptions>>().Value;
        }

        _ = await Assert.That(Resolve).Throws<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    [Test]
    public async Task RequiredWhenValidationFailsThroughOptionsValidation()
    {
        TypeSchemaModel model = BuildModel(Object(nameof(ColdStorageOptions), [
            Property(nameof(ColdStorageOptions.Provider), new TypeId("String")),
            Property(nameof(ColdStorageOptions.TargetFilePath), new TypeId("String"), Annotations(
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenSource, nameof(ColdStorageOptions.Provider)),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenValue, "File")))
        ], Annotations(Annotation("schema.role", "Configuration"), Annotation(ConfigurationAnnotationKeys.SectionName, "ColdStorage"))));
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ColdStorage:Provider"] = "File" })
            .Build();
        var services = new ServiceCollection();
        _ = services.AddSemanticOptions<ColdStorageOptions>(configuration, model);
        using ServiceProvider provider = services.BuildServiceProvider();

        void Resolve()
        {
            _ = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ColdStorageOptions>>().Value;
        }

        _ = await Assert.That(Resolve).Throws<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    private sealed class ColdStorageOptions
    {
        public string? Provider { get; init; }
        public string? TargetFilePath { get; init; }
    }

    private sealed class UnusedOptions
    {
        public string? Value { get; init; }
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new() { Id = new SchemaModelId("Registration"), Types = types, TypesById = types.ToDictionary(static t => t.Id, static t => t), Annotations = new AnnotationBag() };
    }

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties, AnnotationBag annotations)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = annotations, Properties = properties, Keys = [], Relationships = [] };
    }

    private static PropertyDefinition Property(string name, TypeId type, AnnotationBag? annotations = null)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(type), Cardinality = new Cardinality(), Mutability = Mutability.InitOnly, Constraints = new ConstraintSet(), Annotations = annotations ?? new AnnotationBag() };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new() { Items = annotations };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new() { Key = new AnnotationKey(key), Value = value, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared };
    }
}

#pragma warning restore CS1591
