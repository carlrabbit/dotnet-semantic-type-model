#pragma warning disable CS1591
namespace SemanticTypeModel.Abstractions.Canonical;

/// <summary>
/// Produces stable, human-readable path strings for use in diagnostics, tests, and projection errors.
/// Paths follow the convention <c>/types/{typeId}/properties/{propertyName}</c>.
/// </summary>
public static class ModelPath
{
    /// <summary>Returns the path for a type: <c>/types/{typeId}</c>.</summary>
    public static string ForType(TypeId typeId)
    {
        return $"/types/{EscapeSegment(typeId.Value)}";
    }

    /// <summary>Returns the path for a property: <c>/types/{typeId}/properties/{propertyName}</c>.</summary>
    public static string ForProperty(TypeId typeId, string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        return $"/types/{EscapeSegment(typeId.Value)}/properties/{EscapeSegment(propertyName)}";
    }

    /// <summary>Returns the path for a relationship: <c>/types/{typeId}/relationships/{relationshipId}</c>.</summary>
    public static string ForRelationship(TypeId typeId, RelationshipId relationshipId)
    {
        return $"/types/{EscapeSegment(typeId.Value)}/relationships/{EscapeSegment(relationshipId.Value)}";
    }

    /// <summary>Returns the path for a key: <c>/types/{typeId}/keys/{keyName}</c>.</summary>
    public static string ForKey(TypeId typeId, string keyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);
        return $"/types/{EscapeSegment(typeId.Value)}/keys/{EscapeSegment(keyName)}";
    }

    /// <summary>Returns the path for a computed member: <c>/types/{typeId}/computedMembers/{memberName}</c>.</summary>
    public static string ForComputedMember(TypeId typeId, string memberName)
    {
        ArgumentException.ThrowIfNullOrEmpty(memberName);
        return $"/types/{EscapeSegment(typeId.Value)}/computedMembers/{EscapeSegment(memberName)}";
    }

    /// <summary>Returns the path for an annotation: <c>{parentPath}/annotations/{annotationKey}</c>.</summary>
    public static string ForAnnotation(string parentPath, AnnotationKey annotationKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentPath);
        return $"{parentPath}/annotations/{EscapeSegment(annotationKey.Value)}";
    }

    /// <summary>Returns the path for an enum value: <c>/types/{typeId}/values/{valueName}</c>.</summary>
    public static string ForEnumValue(TypeId typeId, string valueName)
    {
        ArgumentException.ThrowIfNullOrEmpty(valueName);
        return $"/types/{EscapeSegment(typeId.Value)}/values/{EscapeSegment(valueName)}";
    }

    /// <summary>Returns the path for a property reference beneath the given parent path.</summary>
    public static string ForPropertyReference(string parentPath, PropertyId propertyId)
    {
        ArgumentException.ThrowIfNullOrEmpty(parentPath);
        return $"{parentPath}/{EscapeSegment(propertyId.Value)}";
    }

    private static string EscapeSegment(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return value.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }
}
#pragma warning restore CS1591
