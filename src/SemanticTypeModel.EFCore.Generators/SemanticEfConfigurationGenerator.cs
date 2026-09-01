using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using SemanticTypeModel.EFCore.Internal;

namespace SemanticTypeModel.EFCore.Generators;

/// <summary>Generates ordinary EF Core configurations for explicitly selected semantic manifests.</summary>
[Generator]
public sealed class SemanticEfConfigurationGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat DeclaredTypeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
    private const string SelectionAttribute = "SemanticTypeModel.EFCore.GenerateSemanticEfModelAttribute";
    private const string ManifestMetadataName = "SemanticTypeModel.Manifest";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (productionContext, compilation) => Execute(productionContext, compilation));
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        INamedTypeSymbol? selectionType = compilation.GetTypeByMetadataName(SelectionAttribute);
        if (selectionType is null)
        {
            return;
        }

        var models = new List<SelectedModel>();
        foreach (AttributeData selection in compilation.Assembly.GetAttributes()
            .Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, selectionType)))
        {
            Location? location = selection.ApplicationSyntaxReference?.GetSyntax().GetLocation();
            if (selection.ConstructorArguments.FirstOrDefault().Value is not INamedTypeSymbol marker)
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.SelectedManifestMissing, location, "<unresolved marker>"));
                continue;
            }

            AttributeData[] attributes = [.. marker.ContainingAssembly.GetAttributes().Where(IsManifestAttribute)];
            if (attributes.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.SelectedManifestMissing, location, marker.ToDisplayString()));
                continue;
            }

            if (attributes.Length > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.SelectedManifestAmbiguous, location, marker.ContainingAssembly.Name));
                continue;
            }

            if (!TryReadManifest(attributes[0], out SemanticManifest? manifest, out string? failure))
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.SelectedManifestInvalid, location, marker.ContainingAssembly.Name, failure));
                continue;
            }

            if (manifest!.Version != SemanticManifest.CurrentVersion)
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ManifestVersionUnsupported, location, manifest.Version, SemanticManifest.CurrentVersion));
                continue;
            }

            if (!string.Equals(manifest.SemanticTypeModelVersion, SuiteVersion.Current, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ManifestSuiteVersionMismatch, location, manifest.SemanticTypeModelVersion, SuiteVersion.Current));
                continue;
            }

            models.Add(new SelectedModel(manifest, marker.ContainingAssembly, location));
        }

        ReportOwnershipCollisions(context, models);
        if (HasOwnershipCollisions(models))
        {
            return;
        }

        ReportNameCollisions(context, models);
        if (HasNameCollisions(models))
        {
            return;
        }

        foreach (SelectedModel model in models.OrderBy(static model => model.Manifest.ModelName, StringComparer.Ordinal))
        {
            GenerateModel(context, model);
        }
    }

    private static bool IsManifestAttribute(AttributeData attribute)
    {
        return attribute.AttributeClass?.ToDisplayString() == "System.Reflection.AssemblyMetadataAttribute"
            && string.Equals(attribute.ConstructorArguments.ElementAtOrDefault(0).Value as string, ManifestMetadataName, StringComparison.Ordinal);
    }

    private static bool TryReadManifest(AttributeData attribute, out SemanticManifest? manifest, out string? failure)
    {
        try
        {
            string? encoded = attribute.ConstructorArguments.ElementAtOrDefault(1).Value as string;
            if (string.IsNullOrWhiteSpace(encoded))
            {
                manifest = null;
                failure = "the metadata payload is empty";
                return false;
            }

            manifest = JsonSerializer.Deserialize<SemanticManifest>(Convert.FromBase64String(encoded));
            failure = manifest is null ? "the metadata payload did not contain a manifest" : null;
            return manifest is not null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException or NotSupportedException)
        {
            manifest = null;
            failure = exception.Message;
            return false;
        }
    }

    private static void GenerateModel(SourceProductionContext context, SelectedModel selected)
    {
        SemanticType[] entities = [.. selected.Manifest.Types
            .Where(static type => type.Kind == "Object" && EfStoragePolicy.IsEntityRole(type.Role))
            .OrderBy(type => EntityDepth(type, selected.Manifest), Comparer<int>.Default)
            .ThenBy(static type => type.Id, StringComparer.Ordinal)
            .ThenBy(static type => type.ClrName, StringComparer.Ordinal)];

        var generated = new List<(SemanticType Manifest, INamedTypeSymbol Symbol)>();
        foreach (SemanticType entity in entities)
        {
            string metadataName = MetadataName(entity.ClrName);
            INamedTypeSymbol? symbol = selected.Assembly.GetTypeByMetadataName(metadataName);
            if (symbol is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ClrTypeUnresolved, selected.Location, entity.ClrName));
                continue;
            }

            if (!TryGenerateConfiguration(context, selected, entity, symbol, out string source))
            {
                continue;
            }

            generated.Add((entity, symbol));
            context.AddSource($"{Safe(metadataName)}.SemanticEfConfiguration.g.cs", source);
        }

        context.AddSource(
            $"{Safe(selected.Manifest.ModelName)}.SemanticEfRegistration.g.cs",
            GenerateRegistration(selected.Manifest.ModelName, generated));
    }

    private static bool TryGenerateConfiguration(
        SourceProductionContext context,
        SelectedModel selected,
        SemanticType entity,
        INamedTypeSymbol symbol,
        out string source)
    {
        string namespaceName = symbol.ContainingNamespace.IsGlobalNamespace
            ? "SemanticTypeModel.Generated.EFCore"
            : symbol.ContainingNamespace.ToDisplayString();
        string entityType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string configurationName = ConfigurationName(MetadataName(entity.ClrName));
        var body = new StringBuilder();
        bool valid = true;

        body.Append("        builder.ToTable(\"").Append(Escape(entity.Name)).AppendLine("\");");

        SemanticType? semanticBase = FindSemanticBase(entity, selected.Manifest);
        SemanticProperty[] storedProperties = [.. entity.Properties
            .Where(property => IsStoredOnEntity(property, entity, semanticBase))
            .OrderBy(static property => property.Name, StringComparer.Ordinal)];
        SemanticProperty[] keys = [.. storedProperties.Where(static property => property.IsPrimaryKey).OrderBy(static property => property.KeyOrder).ThenBy(static property => property.Name, StringComparer.Ordinal)];
        if (semanticBase is null)
        {
            if (keys.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ProjectionError, selected.Location, entity.Name, "a primary key is required"));
                valid = false;
            }
            else
            {
                body.Append("        builder.HasKey(entity => new { ")
                    .Append(string.Join(", ", keys.Select(static key => "entity." + Safe(key.MemberName))))
                    .AppendLine(" });");
            }
        }

        foreach (SemanticProperty property in storedProperties)
        {
            if (!TryResolveProperty(symbol, property.MemberName, out IPropertySymbol? member))
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ClrMemberUnresolved, selected.Location, entity.ClrName, property.MemberName));
                valid = false;
                continue;
            }

            SemanticType? target = selected.Manifest.Types.FirstOrDefault(type => string.Equals(type.Id, property.TypeId, StringComparison.Ordinal));
            if (!AppendProperty(body, selected.Manifest, entity, property, member!, target, out string? error))
            {
                context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ProjectionError, selected.Location, entity.Name, error));
                valid = false;
            }
        }

        if (semanticBase is null && selected.Manifest.Types.Any(candidate => ReferenceEquals(FindSemanticBase(candidate, selected.Manifest), entity)))
        {
            body.AppendLine("        builder.UseTptMappingStrategy();");
        }

        if (!valid)
        {
            source = string.Empty;
            return false;
        }

        source = $$"""
            // <auto-generated/>
            #nullable enable
            using Microsoft.EntityFrameworkCore;

            namespace {{namespaceName}};

            internal partial class {{configurationName}}
                : global::Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<{{entityType}}>
            {
                public void Configure(global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<{{entityType}}> builder)
                {
                    ConfigureBeforeGenerated(builder);
                    ConfigureGenerated(builder);
                    ConfigureAfterGenerated(builder);
                }

                private static void ConfigureGenerated(global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<{{entityType}}> builder)
                {
            {{body.ToString().TrimEnd()}}
                }

                static partial void ConfigureBeforeGenerated(global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<{{entityType}}> builder);
                static partial void ConfigureAfterGenerated(global::Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<{{entityType}}> builder);
            }
            """;
        return true;
    }

    private static bool AppendProperty(
        StringBuilder source,
        SemanticManifest manifest,
        SemanticType entity,
        SemanticProperty property,
        IPropertySymbol member,
        SemanticType? target,
        out string? error)
    {
        string expression = "builder.Property(entity => entity." + Safe(property.MemberName) + ")";
        string configuredProperty = expression + ".IsRequired(" + (!property.IsNullable ? "true" : "false") + ")";
        // Storage classification deliberately normalizes nullable types below, but EF's generic
        // converter/comparer APIs must use the property's declared Roslyn type exactly.
        string memberType = member.Type.ToDisplayString(DeclaredTypeDisplayFormat);
        if (EfStoragePolicy.IsJsonStorage(property.IsExtensionData, property.Ownership))
        {
            SemanticType? valueTarget = target?.Kind == "Array"
                ? manifest.Types.FirstOrDefault(type => string.Equals(type.Id, target.ItemTypeId, StringComparison.Ordinal))
                : target;
            if (!EfStoragePolicy.IsValidOwnedValueKind(property.IsExtensionData, valueTarget?.Kind, valueTarget?.Role))
            {
                error = $"owned member '{property.Name}' must target a semantic ValueKind";
                return false;
            }

            source.Append("        ").Append(configuredProperty)
                .Append(".HasConversion(global::SemanticTypeModel.EFCore.SemanticEfValueConverters.Json<")
                .Append(memberType).AppendLine(">());");
            source.Append("        ").Append(expression)
                .Append(".Metadata.SetValueComparer(global::SemanticTypeModel.EFCore.SemanticEfValueConverters.JsonComparer<")
                .Append(memberType).AppendLine(">());");
            error = null;
            return true;
        }

        ITypeSymbol actual = UnwrapNullable(member.Type);
        bool nullableMember = member.Type.NullableAnnotation == NullableAnnotation.Annotated
            || !SymbolEqualityComparer.Default.Equals(actual, member.Type);
        EfScalarStorageKind scalarStorage = EfStoragePolicy.ClassifyScalar(
            actual.ToDisplayString(),
            target?.Kind == "Enum" || actual.TypeKind == TypeKind.Enum);

        if (EfStoragePolicy.IsUnsupportedUnownedShape(target?.Kind)
            && scalarStorage is not (EfScalarStorageKind.DirectBinary or EfScalarStorageKind.ReadOnlyMemoryBinary))
        {
            error = $"member '{property.Name}' has unsupported target shape '{target!.Kind}' without semantic JSON ownership";
            return false;
        }

        switch (scalarStorage)
        {
            case EfScalarStorageKind.EnumString:
                source.Append("        ").Append(configuredProperty).AppendLine(".HasConversion<string>();");
                error = null;
                return true;
            case EfScalarStorageKind.UriString:
                source.Append("        ").Append(configuredProperty)
                    .Append(nullableMember
                        ? ".HasConversion(global::SemanticTypeModel.EFCore.SemanticEfValueConverters.NullableUri());"
                        : ".HasConversion(global::SemanticTypeModel.EFCore.SemanticEfValueConverters.Uri());")
                    .AppendLine();
                error = null;
                return true;
            case EfScalarStorageKind.CharString:
                source.Append("        ").Append(configuredProperty).AppendLine(".HasConversion<string>();");
                error = null;
                return true;
            case EfScalarStorageKind.ReadOnlyMemoryBinary:
                source.Append("        ").Append(configuredProperty)
                    .AppendLine(nullableMember
                        ? ".HasConversion(value => value.HasValue ? value.Value.ToArray() : null, value => value == null ? (global::System.ReadOnlyMemory<byte>?)null : new global::System.ReadOnlyMemory<byte>(value));"
                        : ".HasConversion(value => value.ToArray(), value => new global::System.ReadOnlyMemory<byte>(value));");
                error = null;
                return true;
            case EfScalarStorageKind.DirectBinary:
                source.Append("        ").Append(configuredProperty).AppendLine(";");
                error = null;
                return true;
            case EfScalarStorageKind.Direct:
                source.Append("        ").Append(configuredProperty).AppendLine(";");
                error = null;
                return true;
            case EfScalarStorageKind.Unsupported:
                error = $"member '{entity.Name}.{property.Name}' has unsupported scalar type '{memberType}'";
                return false;
            default:
                throw new InvalidOperationException($"Unknown scalar storage kind '{scalarStorage}'.");
        }
    }

    private static string GenerateRegistration(string modelName, IReadOnlyList<(SemanticType Manifest, INamedTypeSymbol Symbol)> entities)
    {
        var calls = new StringBuilder();
        foreach ((SemanticType entity, INamedTypeSymbol symbol) in entities)
        {
            calls.Append("        modelBuilder.ApplyConfiguration(new global::")
                .Append(symbol.ContainingNamespace.ToDisplayString()).Append('.')
                .Append(ConfigurationName(MetadataName(entity.ClrName))).AppendLine("());");
        }

        return $$"""
            // <auto-generated/>
            #nullable enable
            using Microsoft.EntityFrameworkCore;

            namespace SemanticTypeModel.Generated.EFCore;

            /// <summary>Registers the generated {{modelName}} semantic EF configurations.</summary>
            public static class {{Safe(modelName)}}SemanticEfModelExtensions
            {
                /// <summary>Applies the generated {{modelName}} semantic EF configurations.</summary>
                public static global::Microsoft.EntityFrameworkCore.ModelBuilder {{ApplyName(modelName)}}(
                    this global::Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
                {
            {{calls.ToString().TrimEnd()}}
                    return modelBuilder;
                }
            }
            """;
    }

    private static void ReportOwnershipCollisions(SourceProductionContext context, IReadOnlyList<SelectedModel> models)
    {
        foreach (IGrouping<string, (SelectedModel Model, SemanticType Type)> collision in OwnedEntities(models)
            .GroupBy(static item => MetadataName(item.Type.ClrName), StringComparer.Ordinal)
            .Where(static group => group.Count() > 1))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                EfGeneratorDiagnosticDescriptors.EntityOwnershipCollision,
                collision.First().Model.Location,
                collision.Key,
                string.Join(", ", collision.Select(static item => item.Model.Manifest.ModelName).Order(StringComparer.Ordinal))));
        }
    }

    private static bool HasOwnershipCollisions(IReadOnlyList<SelectedModel> models)
    {
        return OwnedEntities(models).GroupBy(static item => MetadataName(item.Type.ClrName), StringComparer.Ordinal).Any(static group => group.Count() > 1);
    }

    private static void ReportNameCollisions(SourceProductionContext context, IReadOnlyList<SelectedModel> models)
    {
        foreach (IGrouping<string, (SelectedModel Model, SemanticType Type)> collision in OwnedEntities(models)
            .GroupBy(static item => item.Model.Manifest.ModelName + "|" + ConfigurationName(MetadataName(item.Type.ClrName).Split('.').Last()), StringComparer.Ordinal)
            .Where(static group => group.Count() > 1))
        {
            context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.ConfigurationNameCollision, collision.First().Model.Location, collision.Key));
        }

        foreach (IGrouping<string, SelectedModel> collision in models.GroupBy(static model => ApplyName(model.Manifest.ModelName), StringComparer.Ordinal).Where(static group => group.Count() > 1))
        {
            context.ReportDiagnostic(Diagnostic.Create(EfGeneratorDiagnosticDescriptors.RegistrationNameCollision, collision.First().Location, collision.Key));
        }
    }

    private static bool HasNameCollisions(IReadOnlyList<SelectedModel> models)
    {
        return OwnedEntities(models).GroupBy(static item => item.Model.Manifest.ModelName + "|" + ConfigurationName(MetadataName(item.Type.ClrName).Split('.').Last()), StringComparer.Ordinal).Any(static group => group.Count() > 1)
            || models.GroupBy(static model => ApplyName(model.Manifest.ModelName), StringComparer.Ordinal).Any(static group => group.Count() > 1);
    }

    private static IEnumerable<(SelectedModel Model, SemanticType Type)> OwnedEntities(IEnumerable<SelectedModel> models)
    {
        return models.SelectMany(model => model.Manifest.Types
            .Where(static type => type.Kind == "Object" && EfStoragePolicy.IsEntityRole(type.Role))
            .Select(type => (model, type)));
    }

    private static SemanticType? FindSemanticBase(SemanticType type, SemanticManifest manifest)
    {
        return string.IsNullOrWhiteSpace(type.BaseClrName)
            ? null
            : manifest.Types.FirstOrDefault(candidate => candidate.Kind == "Object"
            && EfStoragePolicy.IsEntityRole(candidate.Role)
            && string.Equals(MetadataName(candidate.ClrName), MetadataName(type.BaseClrName), StringComparison.Ordinal));
    }

    private static int EntityDepth(SemanticType entity, SemanticManifest manifest)
    {
        var depth = 0;
        for (SemanticType? current = FindSemanticBase(entity, manifest); current is not null; current = FindSemanticBase(current, manifest))
        {
            depth++;
        }

        return depth;
    }

    private static bool IsStoredOnEntity(SemanticProperty property, SemanticType entity, SemanticType? semanticBase)
    {
        return semanticBase is null || string.Equals(MetadataName(property.DeclaringClrName ?? entity.ClrName), MetadataName(entity.ClrName), StringComparison.Ordinal);
    }

    private static bool TryResolveProperty(INamedTypeSymbol entity, string memberName, out IPropertySymbol? property)
    {
        for (INamedTypeSymbol? current = entity; current is { SpecialType: not SpecialType.System_Object }; current = current.BaseType)
        {
            IPropertySymbol[] matches = [.. current.GetMembers(memberName).OfType<IPropertySymbol>()];
            if (matches.Length == 1)
            {
                property = matches[0];
                return true;
            }
        }

        property = null;
        return false;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        ITypeSymbol actual = type is INamedTypeSymbol { IsGenericType: true } named
            && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            ? named.TypeArguments[0]
            : type;
        return actual.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
    }

    private static string MetadataName(string value)
    {
        return value.Replace("global::", string.Empty, StringComparison.Ordinal).Split(',')[0].Trim();
    }

    private static string ConfigurationName(string metadataName)
    {
        return Safe(metadataName) + "Configuration";
    }

    private static string ApplyName(string modelName)
    {
        string name = Safe(modelName);
        return "Apply" + (name.EndsWith("SemanticTypeModel", StringComparison.Ordinal) ? name[..^17] : name) + "SemanticModel";
    }

    private static string Safe(string value)
    {
        string identifier = new(value.Select(static character => char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray());
        return identifier.Length == 0 ? "Model" : char.IsDigit(identifier[0]) ? "_" + identifier : identifier;
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private sealed record SelectedModel(SemanticManifest Manifest, IAssemblySymbol Assembly, Location? Location);
}

internal sealed class SemanticManifest
{
    internal const int CurrentVersion = 3;
    public int Version { get; set; }
    public string SemanticTypeModelVersion { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public IReadOnlyList<SemanticType> Types { get; set; } = [];
}


internal static class SuiteVersion
{
    internal static readonly string Current =
        typeof(SemanticEfConfigurationGenerator).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0]
        ?? typeof(SemanticEfConfigurationGenerator).Assembly.GetName().Version?.ToString(3)
        ?? "0.0.0";
}

internal sealed class SemanticType
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ClrName { get; set; } = string.Empty;
    public string? BaseClrName { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? ItemTypeId { get; set; }
    public IReadOnlyList<SemanticProperty> Properties { get; set; } = [];
}

internal sealed class SemanticProperty
{
    public string Name { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string? DeclaringClrName { get; set; }
    public string TypeId { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int KeyOrder { get; set; }
    public string? Ownership { get; set; }
    public bool IsExtensionData { get; set; }
}
