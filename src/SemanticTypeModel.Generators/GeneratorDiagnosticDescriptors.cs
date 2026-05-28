using Microsoft.CodeAnalysis;
using SemanticTypeModel.DotNet.Diagnostics;

namespace SemanticTypeModel.Generators;

/// <summary>
/// Provides stable <see cref="DiagnosticDescriptor"/> instances for all diagnostics emitted directly
/// by <see cref="SemanticTypeModelSourceGenerator"/>.
/// </summary>
/// <remarks>
/// Diagnostics originating from <c>SemanticTypeModel.DotNet</c> extraction use a shared fallback
/// descriptor built at call time, because the extractor emits a broad range of STM5xxx codes whose
/// per-code titles and help URIs are tracked separately in
/// <see cref="DotNetExtractionDiagnosticIds"/>.
/// </remarks>
internal static class GeneratorDiagnosticDescriptors
{
    private const string Category = "SemanticTypeModel";
    // Points to the main branch because no versioned release tag exists yet; update to a tag reference on first stable release.
    private const string HelpUriBase = "https://github.com/carlrabbit/dotnet-semantic-type-model/blob/main/public-docs/diagnostics/stm5xxx.md";

    /// <summary>
    /// STM5008: The discovery mode value specified in MSBuild properties is not supported.
    /// </summary>
    internal static readonly DiagnosticDescriptor UnsupportedDiscoveryMode = new(
        DotNetExtractionDiagnosticIds.UnsupportedDiscoveryMode,
        "Unsupported discovery mode",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpUriBase + "#stm5008");

    /// <summary>
    /// STM5018: The naming policy value specified in MSBuild properties is not supported.
    /// </summary>
    internal static readonly DiagnosticDescriptor UnsupportedNamingPolicy = new(
        DotNetExtractionDiagnosticIds.UnsupportedNamingPolicy,
        "Unsupported naming policy",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpUriBase + "#stm5018");

    /// <summary>
    /// STM5019: The generated provider name collides with an existing type in the compilation.
    /// </summary>
    internal static readonly DiagnosticDescriptor GeneratedProviderNameCollision = new(
        DotNetExtractionDiagnosticIds.GeneratedProviderNameCollision,
        "Generated provider name collision",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpUriBase + "#stm5019");

    /// <summary>
    /// Fallback descriptor used for STM5xxx codes emitted by the .NET type extractor.
    /// The code and message are supplied at call time.
    /// </summary>
    internal static DiagnosticDescriptor ExtractionFallback(string code)
    {
        return new(
            code,
            "SemanticTypeModel .NET extraction",
            "{0}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpUriBase);
    }
}
