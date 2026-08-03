using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names describe behavior.")]
public sealed class M0053SourceLineageDiagnosticsTests
{
    [Test]
    [Arguments("missing", "EFCORE_OWNED_TARGET_TYPE_NOT_FOUND")]
    [Arguments("scalar", "EFCORE_OWNED_TARGET_SHAPE_UNSUPPORTED")]
    [Arguments("collection", "EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED")]
    public async Task OwnedTarget_invalid_shapes_are_diagnostics_not_runtime_exceptions(string shape, string expectedCode)
    {
        SemanticDerivationResult<EfCoreSemanticModel> result = BuildModel(shape).DeriveEfCoreModel();

        SchemaDiagnostic diagnostic = result.Diagnostics.Single(item => item.Code == expectedCode && item.Severity == SchemaDiagnosticSeverity.Error);
        _ = await Assert.That(diagnostic.Severity).IsEqualTo(SchemaDiagnosticSeverity.Error);
        _ = await Assert.That(result.Model.Diagnostics).Contains(diagnostic);
    }

    [Test]
    public async Task ApplicationMode_controls_optional_CLR_lineage_severity_and_is_carried_by_model()
    {
        TypeSchemaModel model = BuildModel("valid", includeClrLineage: false);
        SemanticDerivationResult<EfCoreSemanticModel> closed = model.DeriveEfCoreModel();
        SemanticDerivationResult<EfCoreSemanticModel> shared = model.DeriveEfCoreModel(options => options.ApplicationMode = EfCoreApplicationMode.SharedTypeModel);

        _ = await Assert.That(closed.Model.ApplicationPolicy).IsEqualTo(EfCoreApplicationMode.ClosedClrModel);
        _ = await Assert.That(closed.Diagnostics.Any(d => d.Code == "EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED" && d.Severity == SchemaDiagnosticSeverity.Error)).IsTrue();
        _ = await Assert.That(shared.Model.ApplicationPolicy).IsEqualTo(EfCoreApplicationMode.SharedTypeModel);
        _ = await Assert.That(shared.Diagnostics.Any(d => d.Code == "EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED" && d.Severity == SchemaDiagnosticSeverity.Warning)).IsTrue();
    }

    [Test]
    public async Task ApplySemanticTypeModel_returns_the_same_derivation_lineage_diagnostics()
    {
        TypeSchemaModel model = BuildModel("missing");
        var builder = new ModelBuilder();
        EfCoreModelBuilderProjectionResult applied = builder.ApplySemanticTypeModel(model, options => options.ApplicationMode = EfCoreApplicationMode.SharedTypeModel);

        _ = await Assert.That(applied.Diagnostics.Any(d => d.Code == "EFCORE_OWNED_TARGET_TYPE_NOT_FOUND")).IsTrue();
    }

    [Test]
    public async Task SourceLineage_is_limited_to_projected_roots_and_reachable_owned_types()
    {
        TypeSchemaModel baseline = BuildModel("valid");
        ObjectTypeDefinition infrastructure = new()
        {
            Id = new("System.IEquatable`1"),
            Name = "IEquatable",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation(("dotnet.clrType", "System.IEquatable`1")),
            Semantics = new(),
            Properties = [],
            Keys = [],
            Relationships = [],
        };
        TypeDefinition[] types = [.. baseline.Types, infrastructure];
        TypeSchemaModel model = new()
        {
            Id = baseline.Id,
            Types = types,
            TypesById = types.ToDictionary(type => type.Id),
            Annotations = baseline.Annotations,
        };

        SemanticDerivationResult<EfCoreSemanticModel> result = model.DeriveEfCoreModel();

        _ = await Assert.That(result.Model.SourceTypes.Any(type => type.SourceSemanticTypeId == infrastructure.Id.Value)).IsFalse();
        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code.Contains("SOURCE_LINEAGE", StringComparison.Ordinal) && diagnostic.Message.Contains("IEquatable", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task SourceLineage_uses_stable_source_id_instead_of_projected_name()
    {
        TypeSchemaModel baseline = BuildModel("valid");
        ObjectTypeDefinition sameName = new()
        {
            Id = new("not-the-projected-owner"),
            Name = "owner",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Clr(typeof(SameNameInfrastructure)),
            Semantics = new(),
            Properties = [],
            Keys = [],
            Relationships = [],
        };
        TypeDefinition[] types = [.. baseline.Types, sameName];
        TypeSchemaModel model = new() { Id = baseline.Id, Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = baseline.Annotations };

        EfCoreSemanticModel derived = model.DeriveEfCoreModel().Model;

        _ = await Assert.That(derived.SourceTypes.Any(type => type.SourceSemanticTypeId == sameName.Id.Value)).IsFalse();
    }

    private static TypeSchemaModel BuildModel(string shape, bool includeClrLineage = true)
    {
        AnnotationBag ownerAnnotations = includeClrLineage ? Clr(typeof(LineageOwner)) : new();
        AnnotationBag targetAnnotations = includeClrLineage ? Clr(typeof(LineageTarget)) : new();
        ScalarTypeDefinition scalar = new() { Id = new("scalar"), Name = "scalar", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = ScalarKind.String, Annotations = Clr(typeof(string)) };
        ObjectTypeDefinition target = new() { Id = new("target"), Name = "target", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = targetAnnotations, Semantics = new() { Role = EntityRole.ValueObject, IsValueObject = true }, Properties = [], Keys = [], Relationships = [] };
        TypeId targetId = shape switch { "missing" => new("absent"), "scalar" => scalar.Id, _ => target.Id };
        var ownershipKey = shape == "collection" ? "schema.ownedCollection" : "schema.ownedObject";
        ObjectTypeDefinition owner = new()
        {
            Id = new("owner"),
            Name = "owner",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = ownerAnnotations,
            Semantics = new() { Role = EntityRole.Entity },
            Keys = [],
            Relationships = [],
            Properties = [new PropertyDefinition { Id = new(nameof(LineageOwner.Target)), Name = nameof(LineageOwner.Target), Type = new(targetId), Cardinality = new(), Mutability = Mutability.InitOnly, Constraints = new(), Annotations = Annotation((ownershipKey, "true"), ("dotnet.memberName", nameof(LineageOwner.Target))) }],
        };
        TypeDefinition[] types = [scalar, target, owner];
        return new TypeSchemaModel { Id = new("M0053"), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new() };
    }

    private static AnnotationBag Clr(Type type)
    {
        return Annotation(("dotnet.clrType", type.AssemblyQualifiedName!));
    }

    private static AnnotationBag Annotation(params (string Key, string Value)[] values)
    {
        return new()
        {
            Items = [.. values.Select(value => new Annotation { Key = new(value.Key), Value = value.Value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared })],
        };
    }

    private sealed class LineageOwner { public LineageTarget? Target { get; init; } }
    private sealed class LineageTarget;
    private sealed class SameNameInfrastructure;
}
#pragma warning restore CS1591
