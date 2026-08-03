using System.Reflection;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore;

internal sealed record EfCoreSourceLineageResult
{
    public required IReadOnlyList<EfCoreSourceTypeMapping> SourceTypes { get; init; }
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}

internal static class EfCoreSourceLineage
{
    public static EfCoreSourceLineageResult Create(TypeSchemaModel model, EfCoreApplicationMode applicationMode)
    {
        ObjectTypeDefinition[] objects = [.. model.Types.OfType<ObjectTypeDefinition>()];
        var diagnostics = new List<SchemaDiagnostic>();
        var ownedTargetIds = new HashSet<TypeId>();
        var sourceTypes = new List<EfCoreSourceTypeMapping>();

        foreach (ObjectTypeDefinition type in objects)
        {
            var clrName = GetAnnotation(type.Annotations, "dotnet.clrType") ?? type.Id.Value;
            Type? clrType = Resolve(clrName);
            if (clrType is null)
            {
                Report(diagnostics, LineageSeverity(applicationMode), "EFCORE_SOURCE_LINEAGE_CLR_TYPE_NOT_RESOLVED",
                    $"CLR type '{clrName}' for semantic type '{type.Id.Value}' could not be resolved.", ModelPath.ForType(type.Id));
            }

            EfCoreSourcePropertyMapping[] properties = [.. type.Properties.Select(property => CreateProperty(type, property, clrType, applicationMode, diagnostics))];
            EfCoreSuppressedMember[] suppressed = [.. properties.Where(static property => property.SemanticOnlyKind != EfCoreSemanticOnlyKind.None)
                .Select(property => new EfCoreSuppressedMember
                {
                    SourceMemberName = property.SourceMemberName,
                    SourceDeclaringClrTypeName = property.SourceDeclaringClrTypeName,
                    Reason = "Semantic-only extension data is not part of EF storage.",
                    SemanticOnlyKind = property.SemanticOnlyKind,
                })];
            var owned = new List<EfCoreOwnedMapping>();
            foreach (PropertyDefinition property in type.Properties.Where(IsOwned))
            {
                ResolveOwnedMapping(model, objects, type, property, clrName, applicationMode, ownedTargetIds, owned, diagnostics);
            }

            var valueObject = type.Semantics.IsValueObject || type.Semantics.Role == EntityRole.ValueObject;
            sourceTypes.Add(new EfCoreSourceTypeMapping
            {
                SourceSemanticTypeId = type.Id.Value,
                SourceClrTypeName = clrName,
                SemanticRole = type.Semantics.Role,
                IsRootEntity = (type.Semantics.Role == EntityRole.Entity || type.Semantics.IsAggregateRoot) && !valueObject,
                IsValueObject = valueObject,
                Properties = properties,
                SuppressedMembers = suppressed,
                OwnedMappings = owned,
            });
        }

        return new EfCoreSourceLineageResult
        {
            SourceTypes = [.. sourceTypes.Select(type => type with { IsOwned = ownedTargetIds.Contains(new TypeId(type.SourceSemanticTypeId)) })],
            Diagnostics = diagnostics,
        };
    }

    private static void ResolveOwnedMapping(TypeSchemaModel model, ObjectTypeDefinition[] objects, ObjectTypeDefinition owner,
        PropertyDefinition property, string ownerClrName, EfCoreApplicationMode applicationMode, HashSet<TypeId> ownedTargetIds,
        List<EfCoreOwnedMapping> mappings, List<SchemaDiagnostic> diagnostics)
    {
        var path = ModelPath.ForProperty(owner.Id, property.Name);
        if (IsOwnedCollection(property))
        {
            Report(diagnostics, LineageSeverity(applicationMode), "EFCORE_OWNED_COLLECTION_LINEAGE_POLICY_REQUIRED",
                $"Owned collection '{owner.Name}.{property.Name}' requires an explicit EF Core collection lineage policy.", path);
            return;
        }

        TypeDefinition[] targets = [.. model.Types.Where(candidate => candidate.Id == property.Type.Id)];
        if (targets.Length == 0)
        {
            Report(diagnostics, LineageSeverity(applicationMode), "EFCORE_OWNED_TARGET_TYPE_NOT_FOUND",
                $"Owned property '{owner.Name}.{property.Name}' references unknown target type '{property.Type.Id.Value}'.", path);
            return;
        }

        if (targets.Length > 1)
        {
            Report(diagnostics, LineageSeverity(applicationMode), "EFCORE_OWNED_TARGET_TYPE_AMBIGUOUS",
                $"Owned property '{owner.Name}.{property.Name}' references target type '{property.Type.Id.Value}', which has {targets.Length} definitions.", path);
            return;
        }

        if (targets[0] is not ObjectTypeDefinition target)
        {
            Report(diagnostics, SchemaDiagnosticSeverity.Error, "EFCORE_OWNED_TARGET_SHAPE_UNSUPPORTED",
                $"Owned property '{owner.Name}.{property.Name}' targets unsupported shape '{targets[0].Kind}'. Only a single object target has CLR ownership lineage.", path);
            return;
        }

        // Guard independently from the all-type lookup so duplicate object definitions can never become a LINQ exception.
        ObjectTypeDefinition[] objectTargets = [.. objects.Where(candidate => candidate.Id == property.Type.Id)];
        if (objectTargets.Length != 1)
        {
            Report(diagnostics, LineageSeverity(applicationMode), "EFCORE_OWNED_TARGET_TYPE_AMBIGUOUS",
                $"Owned property '{owner.Name}.{property.Name}' does not resolve to exactly one object target.", path);
            return;
        }

        _ = ownedTargetIds.Add(target.Id);
        var targetClr = GetAnnotation(target.Annotations, "dotnet.clrType") ?? target.Id.Value;
        mappings.Add(new EfCoreOwnedMapping
        {
            OwnerSourceTypeId = owner.Id.Value,
            OwnerClrTypeName = ownerClrName,
            NavigationName = MemberName(property),
            TargetSourceTypeId = target.Id.Value,
            TargetClrTypeName = targetClr,
            TargetSemanticRole = target.Semantics.Role,
            StorageKind = EfCoreStorageKind.OwnedNavigation,
        });
    }

    private static EfCoreSourcePropertyMapping CreateProperty(ObjectTypeDefinition owner, PropertyDefinition property, Type? ownerClrType,
        EfCoreApplicationMode applicationMode, List<SchemaDiagnostic> diagnostics)
    {
        var memberName = MemberName(property);
        PropertyInfo? member = ownerClrType?.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (ownerClrType is not null && member is null)
        {
            Report(diagnostics, LineageSeverity(applicationMode), "EFCORE_SOURCE_LINEAGE_MEMBER_NOT_FOUND",
                $"Public CLR property '{memberName}' for semantic property '{owner.Name}.{property.Name}' could not be found on '{ownerClrType.FullName}'.",
                ModelPath.ForProperty(owner.Id, property.Name));
        }

        var extensionData = IsTrue(property.Annotations, "schema.extensionData");
        return new EfCoreSourcePropertyMapping
        {
            SourcePropertyId = property.Id.Value,
            SourceMemberName = memberName,
            SourceDeclaringClrTypeName = member?.DeclaringType?.AssemblyQualifiedName ?? ownerClrType?.AssemblyQualifiedName ?? string.Empty,
            StorageKind = extensionData ? EfCoreStorageKind.Suppressed : IsOwned(property) ? EfCoreStorageKind.OwnedNavigation : EfCoreStorageKind.Scalar,
            SemanticOnlyKind = extensionData ? EfCoreSemanticOnlyKind.ExtensionData : EfCoreSemanticOnlyKind.None,
        };
    }

    private static SchemaDiagnosticSeverity LineageSeverity(EfCoreApplicationMode mode)
    {
        return mode == EfCoreApplicationMode.ClosedClrModel ? SchemaDiagnosticSeverity.Error : SchemaDiagnosticSeverity.Warning;
    }

    private static void Report(List<SchemaDiagnostic> diagnostics, SchemaDiagnosticSeverity severity, string code, string message, string path)
    {
        diagnostics.Add(new SchemaDiagnostic { Severity = severity, Code = code, Message = message, Stage = SchemaDiagnosticStage.Projection, ModelPath = path, ProjectionTarget = ProjectionTarget.EfCore });
    }

    private static bool IsOwned(PropertyDefinition property)
    {
        return IsTrue(property.Annotations, "schema.ownedObject")
        || IsTrue(property.Annotations, "schema.ownedCollection") || IsTrue(property.Annotations, "schema.ownership");
    }

    private static bool IsOwnedCollection(PropertyDefinition property)
    {
        return IsTrue(property.Annotations, "schema.ownedCollection")
        || string.Equals(GetAnnotation(property.Annotations, "schema.ownership.kind"), "collection", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrue(AnnotationBag annotations, string key)
    {
        return string.Equals(GetAnnotation(annotations, key), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string MemberName(PropertyDefinition property)
    {
        return GetAnnotation(property.Annotations, "dotnet.memberName") ?? property.Name;
    }

    private static string? GetAnnotation(AnnotationBag annotations, string key)
    {
        return annotations.Items.FirstOrDefault(a => a.Key.Value == key)?.Value?.ToString();
    }

    internal static Type? Resolve(string name)
    {
        const string globalPrefix = "global::";
        var normalizedName = name.StartsWith(globalPrefix, StringComparison.Ordinal) ? name[globalPrefix.Length..] : name;
        return Type.GetType(normalizedName, false) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(normalizedName, false)).FirstOrDefault(static type => type is not null);
    }
}
