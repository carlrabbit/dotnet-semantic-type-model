#pragma warning disable CS1591
namespace SemanticTypeModel.Abstractions.Hardening;

/// <summary>
/// Produces stable, human-readable path strings for use in diagnostics, tests, and projection errors.
/// Paths follow the convention <c>/types/{typeId}/properties/{propertyName}</c>.
/// </summary>
public static class ModelPath
{
    /// <summary>Returns the path for a type: <c>/types/{typeId}</c>.</summary>
    public static string ForType(TypeId typeId)
    {
        return $"/types/{typeId.Value}";
    }

    /// <summary>Returns the path for a property: <c>/types/{typeId}/properties/{propertyName}</c>.</summary>
    public static string ForProperty(TypeId typeId, string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        return $"/types/{typeId.Value}/properties/{propertyName}";
    }

    /// <summary>Returns the path for a relationship: <c>/types/{typeId}/relationships/{relationshipId}</c>.</summary>
    public static string ForRelationship(TypeId typeId, RelationshipId relationshipId)
    {
        return $"/types/{typeId.Value}/relationships/{relationshipId.Value}";
    }

    /// <summary>Returns the path for a key: <c>/types/{typeId}/keys/{keyName}</c>.</summary>
    public static string ForKey(TypeId typeId, string keyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);
        return $"/types/{typeId.Value}/keys/{keyName}";
    }

    /// <summary>Returns the path for a computed member: <c>/types/{typeId}/computed/{memberName}</c>.</summary>
    public static string ForComputedMember(TypeId typeId, string memberName)
    {
        ArgumentException.ThrowIfNullOrEmpty(memberName);
        return $"/types/{typeId.Value}/computed/{memberName}";
    }

    /// <summary>Returns the path for an annotation: <c>{parentPath}/annotations/{annotationKey}</c>.</summary>
    public static string ForAnnotation(string parentPath, AnnotationKey annotationKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentPath);
        return $"{parentPath}/annotations/{annotationKey.Value}";
    }
}
#pragma warning restore CS1591
