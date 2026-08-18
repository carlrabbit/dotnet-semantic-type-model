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

    internal static readonly DiagnosticDescriptor TypedLiteralSourceNotFound = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralSourceNotFound, "Typed literal source not found");
    internal static readonly DiagnosticDescriptor TypedLiteralSourceTypeUnsupported = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralSourceTypeUnsupported, "Typed literal source type unsupported");
    internal static readonly DiagnosticDescriptor TypedLiteralValueInvalid = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralValueInvalid, "Typed literal value invalid");
    internal static readonly DiagnosticDescriptor TypedLiteralEnumMemberNotFound = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralEnumMemberNotFound, "Typed literal enum member not found");
    internal static readonly DiagnosticDescriptor TypedLiteralNumericFormatInvalid = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralNumericFormatInvalid, "Typed literal numeric format invalid");
    internal static readonly DiagnosticDescriptor TypedLiteralNumericOverflow = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralNumericOverflow, "Typed literal numeric overflow");
    internal static readonly DiagnosticDescriptor TypedLiteralBooleanInvalid = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralBooleanInvalid, "Typed literal Boolean invalid");
    internal static readonly DiagnosticDescriptor TypedLiteralNullNotAllowed = Extraction(DotNetExtractionDiagnosticIds.TypedLiteralNullNotAllowed, "Typed literal null not allowed");
    internal static readonly DiagnosticDescriptor ConditionalConstraintTargetInvalid = Extraction(DotNetExtractionDiagnosticIds.ConditionalConstraintTargetInvalid, "Conditional constraint target invalid");
    internal static readonly DiagnosticDescriptor ConditionalConstraintSourceInvalid = Extraction(DotNetExtractionDiagnosticIds.ConditionalConstraintSourceInvalid, "Conditional constraint source invalid");
    internal static readonly DiagnosticDescriptor ConditionalConstraintLiteralTypeMismatch = Extraction(DotNetExtractionDiagnosticIds.ConditionalConstraintLiteralTypeMismatch, "Conditional constraint literal type mismatch");
    internal static readonly DiagnosticDescriptor DisplayIdentityDefinitionInvalid = Extraction(DotNetExtractionDiagnosticIds.DisplayIdentityDefinitionInvalid, "Display Identity definition invalid");
    internal static readonly DiagnosticDescriptor AccessPathDefinitionInvalid = Extraction(DotNetExtractionDiagnosticIds.AccessPathDefinitionInvalid, "Access Path definition invalid");

    private static DiagnosticDescriptor Extraction(string code, string title)
    {
        return new(code, title, "{0}", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: HelpUriBase + "#" + code.ToLowerInvariant());
    }

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
