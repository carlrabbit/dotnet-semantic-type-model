namespace SemanticTypeModel.EFCore.Internal;

/// <summary>Normalized relational decisions shared by runtime inspection and compile-time generation.</summary>
internal static class EfStoragePolicy
{
    internal static bool IsJsonStorage(bool extensionData, string? ownership)
    {
        return extensionData || ownership is "Object" or "Collection";
    }

    internal static bool IsValidOwnedValueKind(bool extensionData, string? targetKind, string? targetRole)
    {
        return extensionData || (targetKind == "Object" && string.Equals(targetRole, "ValueObject", StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsUnsupportedUnownedShape(string? targetKind)
    {
        return targetKind is "Object" or "Array" or "Dictionary";
    }

    internal static bool IsEntityRole(string? role)
    {
        return string.Equals(role, "Entity", StringComparison.OrdinalIgnoreCase);
    }

    internal static EfScalarStorageKind ClassifyScalar(string clrTypeName, bool isEnum, bool hasStrongShape)
    {
        return isEnum ? EfScalarStorageKind.EnumString : clrTypeName switch
        {
            "System.Uri" => EfScalarStorageKind.UriString,
            "System.Char" or "char" => EfScalarStorageKind.CharString,
            "System.ReadOnlyMemory<System.Byte>" or "System.ReadOnlyMemory<byte>" => EfScalarStorageKind.ReadOnlyMemoryBinary,
            "System.Byte[]" or "byte[]" => EfScalarStorageKind.DirectBinary,
            _ when hasStrongShape => EfScalarStorageKind.StrongScalar,
            _ when IsDirectScalar(clrTypeName) => EfScalarStorageKind.Direct,
            _ => EfScalarStorageKind.Unsupported,
        };
    }

    private static bool IsDirectScalar(string clrTypeName)
    {
        return clrTypeName is
            "System.String" or "string" or
            "System.Char" or "char" or
            "System.Boolean" or "bool" or
            "System.Byte" or "byte" or
            "System.Int16" or "short" or
            "System.Int32" or "int" or
            "System.Int64" or "long" or
            "System.Single" or "float" or
            "System.Double" or "double" or
            "System.Decimal" or "decimal" or
            "System.Guid" or
            "System.DateOnly" or
            "System.TimeOnly" or
            "System.DateTime" or
            "System.DateTimeOffset" or
            "System.TimeSpan";
    }
}

internal enum EfScalarStorageKind
{
    Unsupported,
    Direct,
    DirectBinary,
    EnumString,
    UriString,
    CharString,
    ReadOnlyMemoryBinary,
    StrongScalar,
}
