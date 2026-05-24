#pragma warning disable CS1591
#pragma warning disable CA1720
namespace SemanticTypeModel.Abstractions.Hardening;

public readonly record struct SchemaModelId(string Value);

public readonly record struct TypeId(string Value);

public readonly record struct PropertyId(string Value);

public readonly record struct RelationshipId(string Value);

public readonly record struct PropertyRef(PropertyId Id);

public readonly record struct AnnotationKey(string Value);

public readonly record struct TypeRef(TypeId Id);

public readonly record struct Nullability(bool AllowsNull)
{
    public static Nullability NonNullable => new(false);
    public static Nullability Nullable => new(true);
}

public sealed record AnnotationBag
{
    public IReadOnlyList<Annotation> Items { get; init; } = [];
}

public sealed record Annotation
{
    public required AnnotationKey Key { get; init; }
    public required object? Value { get; init; }
    public required AnnotationScope Scope { get; init; }
    public required AnnotationSource Source { get; init; }
}

public enum AnnotationScope
{
    Model,
    Type,
    Member,
    Constraint,
    Projection,
}

public enum AnnotationSource
{
    Unknown,
    Declared,
    Imported,
    Generated,
    Transformed,
}

public sealed class TypeSchemaModel
{
    public required SchemaModelId Id { get; init; }
    public required IReadOnlyList<TypeDefinition> Types { get; init; }
    public required IReadOnlyDictionary<TypeId, TypeDefinition> TypesById { get; init; }
    public required AnnotationBag Annotations { get; init; }

    public TypeDefinition? TryGetType(TypeId id)
    {
        return TypesById.TryGetValue(id, out TypeDefinition? type) ? type : null;
    }

    public TypeDefinition GetType(TypeId id)
    {
        return TryGetType(id) ?? throw new InvalidOperationException($"Type '{id.Value}' was not found.");
    }
}

public abstract record TypeDefinition
{
    public required TypeId Id { get; init; }
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public required TypeKind Kind { get; init; }
    public required Nullability Nullability { get; init; }
    public required AnnotationBag Annotations { get; init; }
}

public enum TypeKind
{
    Scalar,
    Object,
    Array,
    Dictionary,
    Enum,
    Union,
    Intersection,
    Reference,
    Any,
    Never,
}

public sealed record ObjectTypeDefinition : TypeDefinition
{
    public required IReadOnlyList<PropertyDefinition> Properties { get; init; }
    public required IReadOnlyList<KeyDefinition> Keys { get; init; }
    public required IReadOnlyList<RelationshipDefinition> Relationships { get; init; }
    public ObjectComposition Composition { get; init; } = new();
    public EntitySemantics Semantics { get; init; } = new();
    public IReadOnlyList<ComputedMemberDefinition> ComputedMembers { get; init; } = [];
}

public sealed record PropertyDefinition
{
    public required PropertyId Id { get; init; }
    public required string Name { get; init; }
    public required TypeRef Type { get; init; }
    public required Cardinality Cardinality { get; init; }
    public required Mutability Mutability { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public required ConstraintSet Constraints { get; init; }
    public required AnnotationBag Annotations { get; init; }
}

public sealed record Cardinality
{
    public bool IsRequired { get; init; }
    public bool AllowsNull { get; init; }
    public int? MinItems { get; init; }
    public int? MaxItems { get; init; }
}

public enum Mutability
{
    Mutable,
    Immutable,
    InitOnly,
    ReadOnly,
    WriteOnly,
}

public sealed record ScalarTypeDefinition : TypeDefinition
{
    public required ScalarKind ScalarKind { get; init; }
    public string? Format { get; init; }
    public string? Unit { get; init; }
    public NumericPrecision? Precision { get; init; }
}

public enum ScalarKind
{
    Boolean,
    String,
    Integer,
    Number,
    Decimal,
    Date,
    Time,
    DateTime,
    DateTimeOffset,
    Duration,
    Guid,
    Binary,
    Json,
    Unknown,
}

public sealed record NumericPrecision
{
    public int? Precision { get; init; }
    public int? Scale { get; init; }
}

public sealed record EnumTypeDefinition : TypeDefinition
{
    public required IReadOnlyList<EnumValueDefinition> Values { get; init; }
    public required EnumStorageKind StorageKind { get; init; }
}

public enum EnumStorageKind
{
    String,
    Integer,
    Number,
    Custom,
}

public sealed record EnumValueDefinition
{
    public required string Name { get; init; }
    public required object Value { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public required AnnotationBag Annotations { get; init; }
}

public sealed record ArrayTypeDefinition : TypeDefinition
{
    public required TypeRef ItemType { get; init; }
    public bool UniqueItems { get; init; }
    public int? MinItems { get; init; }
    public int? MaxItems { get; init; }
}

public sealed record DictionaryTypeDefinition : TypeDefinition
{
    public required TypeRef KeyType { get; init; }
    public required TypeRef ValueType { get; init; }
}

public sealed record UnionTypeDefinition : TypeDefinition
{
    public required IReadOnlyList<TypeRef> Options { get; init; }
    public DiscriminatorDefinition? Discriminator { get; init; }
    public UnionSemantics Semantics { get; init; }
}

public enum UnionSemantics
{
    OneOf,
    AnyOf,
}

public sealed record IntersectionTypeDefinition : TypeDefinition
{
    public required IReadOnlyList<TypeRef> Members { get; init; }
}

public sealed record ReferenceTypeDefinition : TypeDefinition
{
    public required TypeRef Target { get; init; }
}

public sealed record DiscriminatorDefinition
{
    public required string PropertyName { get; init; }
    public IReadOnlyDictionary<string, TypeRef> Mapping { get; init; } = new Dictionary<string, TypeRef>(StringComparer.Ordinal);
}

public sealed record ObjectComposition
{
    public IReadOnlyList<TypeRef> AllOf { get; init; } = [];
}

public sealed record EntitySemantics
{
    public EntityRole Role { get; init; }
    public bool IsAggregateRoot { get; init; }
    public bool IsValueObject { get; init; }
}

public enum EntityRole
{
    Unspecified,
    Entity,
    ValueObject,
    Dimension,
    Fact,
    Lookup,
    Event,
    Configuration,
    Form,
}

public sealed record KeyDefinition
{
    public required string Name { get; init; }
    public required IReadOnlyList<PropertyRef> Properties { get; init; }
    public required KeyKind Kind { get; init; }
    public bool IsGenerated { get; init; }
    public required AnnotationBag Annotations { get; init; }
}

public enum KeyKind
{
    Primary,
    Alternate,
    Natural,
    Surrogate,
    External,
}

public sealed record RelationshipDefinition
{
    public required RelationshipId Id { get; init; }
    public required TypeRef PrincipalType { get; init; }
    public required TypeRef DependentType { get; init; }
    public required IReadOnlyList<PropertyRef> PrincipalProperties { get; init; }
    public required IReadOnlyList<PropertyRef> DependentProperties { get; init; }
    public required RelationshipCardinality Cardinality { get; init; }
    public DeleteBehaviorSemantics DeleteBehavior { get; init; }
    public required AnnotationBag Annotations { get; init; }
}

public enum RelationshipCardinality
{
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany,
}

public enum DeleteBehaviorSemantics
{
    Unspecified,
    Restrict,
    Cascade,
    SetNull,
    NoAction,
}

public sealed record ConstraintSet
{
    public StringConstraints? String { get; init; }
    public NumericConstraints? Numeric { get; init; }
    public ArrayConstraints? Array { get; init; }
    public ObjectConstraints? Object { get; init; }
    public IReadOnlyList<CustomConstraint> Custom { get; init; } = [];
}

public sealed record StringConstraints
{
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Pattern { get; init; }
}

public sealed record NumericConstraints
{
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public bool ExclusiveMinimum { get; init; }
    public bool ExclusiveMaximum { get; init; }
    public decimal? MultipleOf { get; init; }
}

public sealed record ArrayConstraints
{
    public int? MinItems { get; init; }
    public int? MaxItems { get; init; }
    public bool UniqueItems { get; init; }
}

public sealed record ObjectConstraints
{
    public int? MinProperties { get; init; }
    public int? MaxProperties { get; init; }
    public AdditionalPropertiesPolicy AdditionalProperties { get; init; }
}

public enum AdditionalPropertiesPolicy
{
    Unspecified,
    Allow,
    Disallow,
    Typed,
    Pattern,
}

public sealed record CustomConstraint
{
    public required string Name { get; init; }
    public object? Value { get; init; }
    public AnnotationBag Annotations { get; init; } = new();
}

public sealed record ComputedMemberDefinition
{
    public required string Name { get; init; }
    public required TypeRef ResultType { get; init; }
    public required ExpressionDefinition Expression { get; init; }
    public required AnnotationBag Annotations { get; init; }
}

public sealed record ExpressionDefinition
{
    public required string Language { get; init; }
    public required string Body { get; init; }
}

public sealed record SchemaDiagnostic
{
    public required SchemaDiagnosticSeverity Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required SchemaDiagnosticStage Stage { get; init; }
    public string? ModelPath { get; init; }
    public string? Source { get; init; }
    public string? PipelineStage { get; init; }
    public ProjectionTarget? ProjectionTarget { get; init; }
    public IReadOnlyList<string> RelatedModelPaths { get; init; } = [];
}

public enum SchemaDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public enum SchemaDiagnosticStage
{
    Import,
    Transformation,
    Validation,
    Export,
    Projection,
}

public enum ProjectionTarget
{
    JsonSchema,
    JsonEditor,
    EfCore,
    PowerBi,
    Tom,
    DotNet,
}

public sealed class TypeSchemaModelBuilder
{
    public TypeSchemaModelBuilder(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public TypeSchemaModel Model { get; private set; }

    public void Replace(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
    }

    public TypeSchemaModel Build()
    {
        return Model;
    }
}

public sealed record AnnotationPolicy
{
    public static AnnotationPolicy Default { get; } = new();

    public AnnotationMergeBehavior MergeBehavior { get; init; } = AnnotationMergeBehavior.LastWins;
    public bool PreserveUnknownAnnotations { get; init; } = true;
    public bool RemoveMalformedAnnotations { get; init; } = true;
    public bool DiagnoseReservedNamespaceConflicts { get; init; } = true;
}

public enum AnnotationMergeBehavior
{
    LastWins,
    FirstWins,
    Error,
}

public sealed class SchemaDiagnosticSink
{
    private readonly List<SchemaDiagnostic> _diagnostics;

    public SchemaDiagnosticSink()
        : this([], false)
    {
    }

    public SchemaDiagnosticSink(IEnumerable<SchemaDiagnostic> diagnostics, bool promoteWarningsToErrors = false)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        _diagnostics = [.. diagnostics];
        PromoteWarningsToErrors = promoteWarningsToErrors;
    }

    public bool PromoteWarningsToErrors { get; }

    public IReadOnlyList<SchemaDiagnostic> Diagnostics => _diagnostics;

    public bool HasErrors => _diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);

    public void Report(SchemaDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        _diagnostics.Add(
            PromoteWarningsToErrors && diagnostic.Severity == SchemaDiagnosticSeverity.Warning
                ? diagnostic with { Severity = SchemaDiagnosticSeverity.Error }
                : diagnostic);
    }

    public void ReportRange(IEnumerable<SchemaDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        foreach (SchemaDiagnostic diagnostic in diagnostics)
        {
            Report(diagnostic);
        }
    }
}

public sealed record SchemaTransformContext
{
    public required SchemaDiagnosticSink Diagnostics { get; init; }
    public AnnotationPolicy AnnotationPolicy { get; init; } = AnnotationPolicy.Default;
    public string? PipelineStage { get; init; }
    public IServiceProvider? Services { get; init; }
}

public sealed record SchemaPipelineOptions
{
    public static SchemaPipelineOptions Default { get; } = new();

    public bool ContinueOnError { get; init; }
    public bool PromoteWarningsToErrors { get; init; }
    public AnnotationPolicy AnnotationPolicy { get; init; } = AnnotationPolicy.Default;
    public IServiceProvider? Services { get; init; }
    public IReadOnlyList<SchemaDiagnostic> InitialDiagnostics { get; init; } = [];
}

public sealed record SchemaPipelineResult
{
    public required TypeSchemaModel Model { get; init; }
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }

    public bool HasErrors => Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error);
}

public sealed record SchemaProjectionContext
{
    public ProjectionTarget Target { get; init; }
    public IList<SchemaDiagnostic> Diagnostics { get; init; } = [];
}
#pragma warning restore CS1591
#pragma warning restore CA1720
