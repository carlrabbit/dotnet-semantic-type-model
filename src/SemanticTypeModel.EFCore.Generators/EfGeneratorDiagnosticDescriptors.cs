using Microsoft.CodeAnalysis;
using SemanticTypeModel.DotNet.Diagnostics;

namespace SemanticTypeModel.EFCore.Generators;

internal static class EfGeneratorDiagnosticDescriptors
{
    internal static readonly DiagnosticDescriptor SelectedManifestMissing = Create(DotNetExtractionDiagnosticIds.EfSelectedManifestMissing, "Selected semantic manifest is missing", "The assembly selected by '{0}' has no semantic manifest.");
    internal static readonly DiagnosticDescriptor SelectedManifestAmbiguous = Create(DotNetExtractionDiagnosticIds.EfSelectedManifestAmbiguous, "Selected semantic manifest is ambiguous", "Selected assembly '{0}' contains more than one semantic manifest.");
    internal static readonly DiagnosticDescriptor ManifestVersionUnsupported = Create(DotNetExtractionDiagnosticIds.EfManifestVersionUnsupported, "Semantic manifest version is unsupported", "Semantic manifest version '{0}' is unsupported; expected version '{1}'.");
    internal static readonly DiagnosticDescriptor ManifestSuiteVersionMismatch = Create(DotNetExtractionDiagnosticIds.EfManifestSuiteVersionMismatch, "SemanticTypeModel package versions do not match", "The semantic manifest was produced by SemanticTypeModel version '{0}', but this EF generator is version '{1}'. Use the same exact version for all SemanticTypeModel packages.");
    internal static readonly DiagnosticDescriptor SelectedManifestInvalid = Create(DotNetExtractionDiagnosticIds.EfSelectedManifestInvalid, "Selected semantic manifest is invalid", "The semantic manifest in assembly '{0}' is invalid: {1}.");
    internal static readonly DiagnosticDescriptor EntityOwnershipCollision = Create(DotNetExtractionDiagnosticIds.EfEntityOwnershipCollision, "Semantic entity ownership collision", "CLR Entity '{0}' is owned by multiple selected semantic models: {1}.");
    internal static readonly DiagnosticDescriptor ConfigurationNameCollision = Create(DotNetExtractionDiagnosticIds.EfConfigurationNameCollision, "Generated configuration name collision", "Generated entity configuration name '{0}' is not unique.");
    internal static readonly DiagnosticDescriptor RegistrationNameCollision = Create(DotNetExtractionDiagnosticIds.EfRegistrationNameCollision, "Generated registration name collision", "Generated semantic model registration name '{0}' is not unique.");
    internal static readonly DiagnosticDescriptor ClrTypeUnresolved = Create(DotNetExtractionDiagnosticIds.EfClrTypeUnresolved, "Semantic CLR type cannot be resolved", "Semantic CLR Entity '{0}' cannot be resolved from the selected assembly.");
    internal static readonly DiagnosticDescriptor ClrMemberUnresolved = Create(DotNetExtractionDiagnosticIds.EfClrMemberUnresolved, "Semantic CLR member cannot be resolved", "Semantic CLR member '{0}.{1}' cannot be resolved.");
    internal static readonly DiagnosticDescriptor ProjectionError = Create(DotNetExtractionDiagnosticIds.EfProjectionError, "Semantic EF projection failed", "Semantic Entity '{0}' cannot be generated: {1}.");

    private static DiagnosticDescriptor Create(string id, string title, string message)
    {
        return new(id, title, message, "SemanticTypeModel.EFCore.Generators", DiagnosticSeverity.Error, isEnabledByDefault: true);
    }
}
