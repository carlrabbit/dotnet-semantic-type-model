namespace SemanticTypeModel.DotNet.Diagnostics;

/// <summary>
/// Contains all stable diagnostic IDs emitted by <c>SemanticTypeModel.DotNet</c> and
/// <c>SemanticTypeModel.Generators</c>.
/// </summary>
/// <remarks>
/// <para>
/// All IDs in this class belong to the STM5xxx range, which covers .NET type extraction
/// and compile-time source-generator diagnostics.
/// </para>
/// </remarks>
public static class DotNetExtractionDiagnosticIds
{
    // -------------------------------------------------------------------------
    // STM5xxx — .NET type extraction and source-generator diagnostics
    // Emitted by RoslynDotNetTypeExtractor and SemanticTypeModelSourceGenerator.
    // -------------------------------------------------------------------------

    /// <summary>STM5001 — A type could not be resolved or processed during extraction.</summary>
    public const string TypeResolutionFailed = "STM5001";

    /// <summary>STM5002 — A property or member could not be resolved or processed during extraction.</summary>
    public const string MemberResolutionFailed = "STM5002";

    /// <summary>STM5003 — A generic type argument could not be resolved.</summary>
    public const string GenericArgumentResolutionFailed = "STM5003";

    /// <summary>STM5004 — A type argument binding was skipped or partially resolved.</summary>
    public const string TypeArgumentBindingSkipped = "STM5004";

    /// <summary>STM5006 — An annotation key or value was invalid and was ignored.</summary>
    public const string AnnotationInvalid = "STM5006";

    /// <summary>STM5007 — A type was included via namespace discovery but has no <c>[SemanticType]</c> attribute.</summary>
    public const string TypeIncludedWithoutAttribute = "STM5007";

    /// <summary>STM5008 — The discovery mode value specified in build properties is not supported.</summary>
    public const string UnsupportedDiscoveryMode = "STM5008";

    /// <summary>STM5009 — The root type could not be determined from the compilation.</summary>
    public const string RootTypeNotFound = "STM5009";

    /// <summary>STM5010 — A type was excluded from extraction because it did not satisfy discovery criteria.</summary>
    public const string TypeExcluded = "STM5010";

    /// <summary>STM5011 — A property type could not be mapped to a known scalar or shape kind.</summary>
    public const string PropertyTypeMappingFailed = "STM5011";

    /// <summary>STM5012 — A type was skipped because it is abstract or has no accessible members.</summary>
    public const string TypeSkippedAbstractOrEmpty = "STM5012";

    /// <summary>STM5013 — A key definition was invalid or could not be applied.</summary>
    public const string KeyDefinitionInvalid = "STM5013";

    /// <summary>STM5014 — A relationship definition was invalid or could not be applied.</summary>
    public const string RelationshipDefinitionInvalid = "STM5014";

    /// <summary>STM5015 — A relationship endpoint reference could not be resolved.</summary>
    public const string RelationshipEndpointUnresolved = "STM5015";

    /// <summary>STM5016 — An enum member has an unsupported backing value type.</summary>
    public const string EnumMemberUnsupportedValue = "STM5016";

    /// <summary>STM5017 — An attribute argument value was invalid or out of range.</summary>
    public const string AttributeArgumentInvalid = "STM5017";

    /// <summary>STM5018 — The naming policy value specified in build properties is not supported.</summary>
    public const string UnsupportedNamingPolicy = "STM5018";

    /// <summary>STM5019 — The generated provider name collides with an existing type in the compilation.</summary>
    public const string GeneratedProviderNameCollision = "STM5019";

    /// <summary>STM5020 — A required XML documentation summary was missing.</summary>
    public const string XmlDocumentationMissing = "STM5020";

    /// <summary>STM5021 — A constraint value was invalid or inconsistent.</summary>
    public const string ConstraintValueInvalid = "STM5021";

    /// <summary>STM5022 — A constraint range minimum exceeded maximum.</summary>
    public const string ConstraintRangeInvalid = "STM5022";

    /// <summary>STM5023 — A semantic name was a duplicate of another type or member name.</summary>
    public const string SemanticNameDuplicate = "STM5023";

    /// <summary>STM5024 — A semantic annotation conflicted with another annotation on the same target.</summary>
    public const string AnnotationConflict = "STM5024";

    /// <summary>STM5025 — A type member shape was not supported by the current extraction path.</summary>
    public const string MemberShapeUnsupported = "STM5025";

    /// <summary>STM5026 — A typed-literal source property was not found.</summary>
    public const string TypedLiteralSourceNotFound = "STM5026";
    /// <summary>STM5027 — A typed-literal source type is unsupported.</summary>
    public const string TypedLiteralSourceTypeUnsupported = "STM5027";
    /// <summary>STM5028 — A typed-literal value is invalid.</summary>
    public const string TypedLiteralValueInvalid = "STM5028";
    /// <summary>STM5029 — An enum-member literal was not found.</summary>
    public const string TypedLiteralEnumMemberNotFound = "STM5029";
    /// <summary>STM5030 — A numeric literal has invalid invariant format.</summary>
    public const string TypedLiteralNumericFormatInvalid = "STM5030";
    /// <summary>STM5031 — A numeric literal exceeds its source type's range.</summary>
    public const string TypedLiteralNumericOverflow = "STM5031";
    /// <summary>STM5032 — A Boolean literal is invalid.</summary>
    public const string TypedLiteralBooleanInvalid = "STM5032";
    /// <summary>STM5033 — Null was used with a non-nullable source.</summary>
    public const string TypedLiteralNullNotAllowed = "STM5033";
    /// <summary>STM5034 — A conditional-constraint target is invalid.</summary>
    public const string ConditionalConstraintTargetInvalid = "STM5034";
    /// <summary>STM5035 — A conditional-constraint source is invalid.</summary>
    public const string ConditionalConstraintSourceInvalid = "STM5035";
    /// <summary>STM5036 — A conditional literal does not match its source type.</summary>
    public const string ConditionalConstraintLiteralTypeMismatch = "STM5036";
    /// <summary>STM5037 — A selected semantic manifest is missing.</summary>
    public const string EfSelectedManifestMissing = "STM5037";
    /// <summary>STM5038 — A selected semantic manifest is ambiguous.</summary>
    public const string EfSelectedManifestAmbiguous = "STM5038";
    /// <summary>STM5039 — A semantic manifest version is unsupported.</summary>
    public const string EfManifestVersionUnsupported = "STM5039";
    /// <summary>STM5040 — A selected semantic manifest is invalid.</summary>
    public const string EfSelectedManifestInvalid = "STM5040";
    /// <summary>STM5041 — Multiple models claim the same CLR Entity.</summary>
    public const string EfEntityOwnershipCollision = "STM5041";
    /// <summary>STM5042 — Generated entity configuration names collide.</summary>
    public const string EfConfigurationNameCollision = "STM5042";
    /// <summary>STM5043 — Generated registration names collide.</summary>
    public const string EfRegistrationNameCollision = "STM5043";
    /// <summary>STM5044 — A manifest CLR type cannot be resolved.</summary>
    public const string EfClrTypeUnresolved = "STM5044";
    /// <summary>STM5045 — A manifest CLR member cannot be resolved.</summary>
    public const string EfClrMemberUnresolved = "STM5045";
    /// <summary>STM5046 — EF configuration projection failed.</summary>
    public const string EfProjectionError = "STM5046";
}
