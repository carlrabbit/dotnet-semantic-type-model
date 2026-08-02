using System.Reflection;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore;

internal static class EfCoreSourceLineage
{
    public static IReadOnlyList<EfCoreSourceTypeMapping> Create(TypeSchemaModel model)
    {
        ObjectTypeDefinition[] objects = [.. model.Types.OfType<ObjectTypeDefinition>()];
        var ownedTargets = objects.SelectMany(owner => owner.Properties.Where(IsOwned)
            .Select(property => property.Type.Id)).ToHashSet();

        return [.. objects.Select(type =>
        {
            var clrName = GetAnnotation(type.Annotations, "dotnet.clrType") ?? type.Id.Value;
            Type? clrType = Resolve(clrName);
            var valueObject = type.Semantics.IsValueObject || type.Semantics.Role == EntityRole.ValueObject;
            EfCoreSourcePropertyMapping[] properties = [.. type.Properties.Select(property => CreateProperty(property, clrType))];
            EfCoreSuppressedMember[] suppressed = [.. properties.Where(static property => property.SemanticOnlyKind != EfCoreSemanticOnlyKind.None)
                .Select(property => new EfCoreSuppressedMember
                {
                    SourceMemberName = property.SourceMemberName,
                    SourceDeclaringClrTypeName = property.SourceDeclaringClrTypeName,
                    Reason = "Semantic-only extension data is not part of EF storage.",
                    SemanticOnlyKind = property.SemanticOnlyKind,
                })];
            EfCoreOwnedMapping[] owned = [.. type.Properties.Where(IsOwned).Select(property =>
            {
                ObjectTypeDefinition target = objects.Single(candidate => candidate.Id == property.Type.Id);
                var targetClr = GetAnnotation(target.Annotations, "dotnet.clrType") ?? target.Id.Value;
                return new EfCoreOwnedMapping
                {
                    OwnerSourceTypeId = type.Id.Value,
                    OwnerClrTypeName = clrName,
                    NavigationName = MemberName(property),
                    TargetSourceTypeId = target.Id.Value,
                    TargetClrTypeName = targetClr,
                    TargetSemanticRole = target.Semantics.Role,
                    StorageKind = EfCoreStorageKind.OwnedNavigation,
                };
            })];

            return new EfCoreSourceTypeMapping
            {
                SourceSemanticTypeId = type.Id.Value,
                SourceClrTypeName = clrName,
                SemanticRole = type.Semantics.Role,
                IsRootEntity = (type.Semantics.Role == EntityRole.Entity || type.Semantics.IsAggregateRoot) && !valueObject,
                IsValueObject = valueObject,
                IsOwned = ownedTargets.Contains(type.Id),
                Properties = properties,
                SuppressedMembers = suppressed,
                OwnedMappings = owned,
            };
        })];
    }

    private static EfCoreSourcePropertyMapping CreateProperty(PropertyDefinition property, Type? ownerClrType)
    {
        var memberName = MemberName(property);
        PropertyInfo? member = ownerClrType?.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
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

    private static bool IsOwned(PropertyDefinition property)
    {
        return IsTrue(property.Annotations, "schema.ownedObject") || IsTrue(property.Annotations, "schema.ownership");
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
