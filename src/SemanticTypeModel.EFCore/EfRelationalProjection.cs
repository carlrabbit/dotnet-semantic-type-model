#pragma warning disable IDE0011, IDE0058, IDE0305
using System.Reflection;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore.Internal;

namespace SemanticTypeModel.EFCore;

/// <summary>Derives and applies the opinionated EF relational contract.</summary>
public static class EfRelationalExtensions
{

    /// <summary>Derives the fixed relational representation of a semantic model.</summary>
    public static SemanticDerivationResult<EfRelationalModel> DeriveEfRelationalModel(this TypeSchemaModel model, Action<EfRelationalOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        var options = new EfRelationalOptions();
        configure?.Invoke(options);
        SemanticModelTransformationResult transformed = SchemaTransformationPipeline.Create().UseCoreDefaults().Run(model);
        TypeSchemaModel effectiveModel = transformed.Model;
        var diagnostics = new List<SchemaDiagnostic>(transformed.Diagnostics);
        ObjectTypeDefinition[] semanticObjects = [.. effectiveModel.Types.OfType<ObjectTypeDefinition>().Where(t => RoleOf(t) is EntityRole.Entity or EntityRole.ValueObject)];
        ObjectTypeDefinition[] entityTypes = [.. semanticObjects.Where(t => RoleOf(t) == EntityRole.Entity)];
        var entities = new List<EfEntity>();
        foreach (ObjectTypeDefinition source in entityTypes.OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            Type? clrType = ResolveClrType(source);
            if (clrType is null)
            {
                Report(diagnostics, "EF_MEMBER_STORAGE_ENTITY_UNRESOLVED", $"Semantic entity '{source.Name}' has no CLR storage entity for its projected members.", source.Id.Value);
                Report(diagnostics, "EF_UNSUPPORTED_SCALAR_TYPE", $"Semantic entity '{source.Name}' has no resolvable CLR type.", source.Id.Value);
                continue;
            }
            var semanticBaseId = Value(source.Annotations, "dotnet.baseType");
            ObjectTypeDefinition[] baseMatches = string.IsNullOrWhiteSpace(semanticBaseId) ? [] : [.. entityTypes.Where(candidate => SameTypeId(candidate.Id.Value, semanticBaseId))];
            ObjectTypeDefinition? baseSource = baseMatches.SingleOrDefault();
            Type? semanticBaseClrType = baseSource is null ? null : ResolveClrType(baseSource);
            var clrBaseIsSemanticEntity = entityTypes.Any(candidate => ResolveClrType(candidate) == clrType.BaseType);
            if (baseMatches.Length > 1 || (baseSource is not null && semanticBaseClrType != clrType.BaseType) || (baseSource is null && clrBaseIsSemanticEntity))
            {
                Report(diagnostics, "EF_SEMANTIC_BASE_INHERITANCE_INVALID", $"Semantic inheritance for '{source.Name}' does not agree with its CLR base type.", source.Id.Value);
                baseSource = null;
            }
            var scalars = new List<EfScalarColumn>();
            var binary = new List<EfScalarColumn>();
            var json = new List<EfJsonColumn>();
            IEnumerable<PropertyDefinition> declared = source.Properties.Where(p => IsStoredOn(baseSource, clrType, p));
            foreach (PropertyDefinition property in declared)
            {
                PropertyInfo[] memberMatches = FindMembers(clrType, MemberName(property));
                PropertyInfo? member = ResolveMember(memberMatches, clrType, baseSource is not null);
                if (member is null)
                {
                    var code = memberMatches.Length > 1 ? "EF_MEMBER_DECLARATION_AMBIGUOUS" : "EF_MEMBER_DECLARING_TYPE_MISMATCH";
                    Report(diagnostics, code, memberMatches.Length > 1
                        ? $"Member '{source.Name}.{property.Name}' has multiple CLR declarations and cannot be placed deterministically."
                        : $"Member '{source.Name}.{property.Name}' has no matching public CLR declaration.", property.Id.Value);
                    continue;
                }
                if (!member.DeclaringType!.IsAssignableFrom(clrType))
                {
                    Report(diagnostics, "EF_MEMBER_DECLARING_TYPE_MISMATCH", $"CLR declaration '{member.DeclaringType.FullName}.{member.Name}' cannot be stored by '{clrType.FullName}'.", property.Id.Value);
                    continue;
                }
                TypeDefinition? target = effectiveModel.Types.FirstOrDefault(t => t.Id == property.Type.Id);
                var extension = IsTrue(property, "schema.extensionData");
                var ownedObject = IsTrue(property, "schema.ownedObject") || string.Equals(Value(property, "schema.ownership.kind"), "object", StringComparison.OrdinalIgnoreCase);
                var ownedCollection = IsTrue(property, "schema.ownedCollection") || string.Equals(Value(property, "schema.ownership.kind"), "collection", StringComparison.OrdinalIgnoreCase);
                if (EfStoragePolicy.IsJsonStorage(extension, extension ? null : ownedCollection ? "Collection" : ownedObject ? "Object" : null) && extension)
                {
                    json.Add(Json(property, member, EfJsonShape.ExtensionData, source, clrType));
                }
                else if (ownedObject || ownedCollection)
                {
                    TypeDefinition? valueTarget = target is ArrayTypeDefinition array ? effectiveModel.Types.FirstOrDefault(t => t.Id == array.ItemType.Id) : target;
                    if (valueTarget is ObjectTypeDefinition owned && RoleOf(owned) == EntityRole.Entity)
                        Report(diagnostics, "EF_ENTITY_CANNOT_BE_OWNED", $"Semantic entity '{owned.Name}' cannot be owned by '{source.Name}.{property.Name}'.", property.Id.Value);
                    else if (valueTarget is not ObjectTypeDefinition valueKind || RoleOf(valueKind) != EntityRole.ValueObject)
                        Report(diagnostics, "EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE", $"Owned member '{source.Name}.{property.Name}' must target a semantic ValueKind.", property.Id.Value);
                    else if (ValidateJsonValueKind(effectiveModel, valueKind, member.PropertyType, diagnostics, []))
                        json.Add(Json(property, member, ownedCollection ? EfJsonShape.Array : EfJsonShape.Object, source, clrType));
                }
                else if (target is ObjectTypeDefinition entityTarget && RoleOf(entityTarget) == EntityRole.Entity)
                    Report(diagnostics, member.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(member.PropertyType) ? "EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE" : "EF_ENTITY_REFERENCE_REQUIRES_IDENTIFIER", $"Entity member '{source.Name}.{property.Name}' must use an identifier property.", property.Id.Value);
                else if (target is ObjectTypeDefinition valueKind && RoleOf(valueKind) == EntityRole.ValueObject)
                    Report(diagnostics, "EF_VALUE_KIND_STORAGE_NOT_DECLARED", $"ValueKind member '{source.Name}.{property.Name}' requires SemanticOwned.", property.Id.Value);
                else if (target is DictionaryTypeDefinition)
                    Report(diagnostics, "EF_DICTIONARY_STORAGE_NOT_SUPPORTED", $"Dictionary member '{source.Name}.{property.Name}' is not an extension-data JSON shape.", property.Id.Value);
                else if (target is ArrayTypeDefinition unownedArray)
                {
                    TypeDefinition? item = effectiveModel.Types.FirstOrDefault(candidate => candidate.Id == unownedArray.ItemType.Id);
                    if (item is ObjectTypeDefinition entityItem && RoleOf(entityItem) == EntityRole.Entity)
                        Report(diagnostics, "EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE", $"Entity collection '{source.Name}.{property.Name}' must use an identifier collection shape.", property.Id.Value);
                    else
                        Report(diagnostics, "EF_VALUE_KIND_STORAGE_NOT_DECLARED", $"Collection member '{source.Name}.{property.Name}' requires an explicitly supported semantic storage shape.", property.Id.Value);
                }
                else if (TryProviderType(member.PropertyType, out Type provider))
                {
                    var column = new EfScalarColumn { PropertyId = property.Id.Value, MemberName = member.Name, ColumnName = member.Name, ClrType = member.PropertyType, ProviderType = provider, IsNullable = !property.Cardinality.IsRequired && (!member.PropertyType.IsValueType || Nullable.GetUnderlyingType(member.PropertyType) is not null), DeclaringClrType = member.DeclaringType!, StorageClrType = clrType, SemanticDeclaringTypeId = source.Id.Value, StorageSemanticTypeId = source.Id.Value };
                    (provider == typeof(byte[]) ? binary : scalars).Add(column);
                }
                else if (target is ScalarTypeDefinition) Report(diagnostics, "EF_WRAPPER_SHAPE_NOT_SUPPORTED", $"Wrapper member '{source.Name}.{property.Name}' must expose one supported scalar Value property and matching constructor.", property.Id.Value);
                else Report(diagnostics, "EF_UNSUPPORTED_SCALAR_TYPE", $"Member '{source.Name}.{property.Name}' has unsupported scalar type '{member.PropertyType}'.", property.Id.Value);
            }
            string[] keys = [.. source.Keys.FirstOrDefault(k => k.Kind == KeyKind.Primary)?.Properties.Select(reference => source.Properties.FirstOrDefault(property => property.Id == reference.Id)).Where(property => property is not null).Select(property => MemberName(property!)) ?? []];
            if (keys.Length == 0) Report(diagnostics, "EF_ENTITY_KEY_REQUIRED", $"Semantic entity '{source.Name}' requires a primary key.", source.Id.Value);
            foreach (IGrouping<string, string> duplicate in scalars.Select(c => c.ColumnName).Concat(binary.Select(c => c.ColumnName)).Concat(json.Select(c => c.ColumnName)).GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1))
            {
                Report(diagnostics, "EF_DUPLICATE_COLUMN_NAME", $"Column name '{duplicate.Key}' is duplicated on '{source.Name}'.", source.Id.Value);
            }
            entities.Add(new EfEntity { SemanticTypeId = source.Id.Value, ClrType = clrType, Table = source.Name, BaseEntityId = baseSource?.Id.Value, Key = keys, ScalarColumns = scalars, JsonColumns = json, BinaryColumns = binary });
        }
        foreach (IGrouping<string, EfEntity> duplicate in entities.GroupBy(e => e.Table, StringComparer.Ordinal).Where(g => g.Count() > 1)) Report(diagnostics, "EF_DUPLICATE_TABLE_NAME", $"Table name '{duplicate.Key}' is duplicated.", duplicate.Key);
        var relational = new EfRelationalModel { Name = model.Id.Value, Entities = entities, Diagnostics = diagnostics };
        return new SemanticDerivationResult<EfRelationalModel> { Model = relational, Diagnostics = diagnostics, Trace = transformed.Trace };
    }

    private static bool TryProviderType(Type type, out Type provider)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        PropertyInfo? value = actual.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        var hasSingleValueShape = value is not null && actual.GetConstructor([value.PropertyType]) is not null;
        var scalarName = actual == typeof(ReadOnlyMemory<byte>)
            ? "System.ReadOnlyMemory<System.Byte>"
            : actual == typeof(byte[])
                ? "System.Byte[]"
                : actual.FullName ?? actual.Name;
        if (value is not null && IsUnsupportedUnsignedInteger(value.PropertyType))
        {
            provider = typeof(void);
            return false;
        }
        EfScalarStorageKind storage = EfStoragePolicy.ClassifyScalar(scalarName, actual.IsEnum, hasSingleValueShape);
        provider = storage switch
        {
            EfScalarStorageKind.EnumString or EfScalarStorageKind.UriString => typeof(string),
            EfScalarStorageKind.CharString => typeof(string),
            EfScalarStorageKind.ReadOnlyMemoryBinary or EfScalarStorageKind.DirectBinary => typeof(byte[]),
            EfScalarStorageKind.SingleValueWrapper => value!.PropertyType,
            EfScalarStorageKind.Direct => actual,
            EfScalarStorageKind.Unsupported => typeof(void),
            _ => typeof(void),
        };
        return storage != EfScalarStorageKind.Unsupported;
    }

    private static bool IsUnsupportedUnsignedInteger(Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual == typeof(sbyte) || actual == typeof(ushort) || actual == typeof(uint) || actual == typeof(ulong);
    }
    private static bool ValidateJsonValueKind(TypeSchemaModel model, ObjectTypeDefinition valueKind, Type declaredClrType, List<SchemaDiagnostic> diagnostics, HashSet<TypeId> visited)
    {
        if (!visited.Add(valueKind.Id)) return true;
        Type clrType = JsonElementType(declaredClrType);
        try
        {
            var value = clrType.IsValueType ? Activator.CreateInstance(clrType) : System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(clrType);
            _ = JsonSerializer.Serialize(value, clrType, CreateJsonOptions());
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
        {
            Report(diagnostics, "EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE", $"ValueKind '{valueKind.Name}' is not serializable as JSON: {exception.Message}", valueKind.Id.Value);
            return false;
        }
        var valid = true;
        foreach (PropertyDefinition nested in valueKind.Properties)
        {
            TypeDefinition? target = model.Types.FirstOrDefault(candidate => candidate.Id == nested.Type.Id);
            var owned = IsTrue(nested, "schema.ownedObject") || IsTrue(nested, "schema.ownedCollection");
            if (target is ObjectTypeDefinition nestedObject && RoleOf(nestedObject) == EntityRole.Entity)
            {
                Report(diagnostics, "EF_ENTITY_CANNOT_BE_OWNED", $"JSON ValueKind '{valueKind.Name}' contains entity member '{nested.Name}'.", nested.Id.Value);
                valid = false;
            }
            else if (target is ObjectTypeDefinition nestedValueKind && RoleOf(nestedValueKind) == EntityRole.ValueObject)
            {
                if (!owned)
                {
                    Report(diagnostics, "EF_VALUE_KIND_STORAGE_NOT_DECLARED", $"Nested ValueKind member '{valueKind.Name}.{nested.Name}' requires SemanticOwned.", nested.Id.Value);
                    valid = false;
                }
                else
                {
                    PropertyInfo? member = clrType.GetProperty(MemberName(nested));
                    valid &= member is not null && ValidateJsonValueKind(model, nestedValueKind, member.PropertyType, diagnostics, visited);
                }
            }
            else if (target is ObjectTypeDefinition unsupportedObject && RoleOf(unsupportedObject) == EntityRole.Unspecified)
            {
                Report(diagnostics, "EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE", $"Nested member '{valueKind.Name}.{nested.Name}' targets unsupported JSON type '{unsupportedObject.Name}'.", nested.Id.Value);
                valid = false;
            }
            else if (target is DictionaryTypeDefinition && !IsTrue(nested, "schema.extensionData"))
            {
                Report(diagnostics, "EF_DICTIONARY_STORAGE_NOT_SUPPORTED", $"Nested dictionary '{valueKind.Name}.{nested.Name}' is not a supported JSON shape.", nested.Id.Value);
                valid = false;
            }
            else if (target is ArrayTypeDefinition entityArray && model.Types.FirstOrDefault(candidate => candidate.Id == entityArray.ItemType.Id) is ObjectTypeDefinition entityItem && RoleOf(entityItem) == EntityRole.Entity)
            {
                Report(diagnostics, "EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE", $"Nested entity collection '{valueKind.Name}.{nested.Name}' must use identifiers.", nested.Id.Value);
                valid = false;
            }
            else if (target is ArrayTypeDefinition array && model.Types.FirstOrDefault(candidate => candidate.Id == array.ItemType.Id) is ObjectTypeDefinition item && RoleOf(item) == EntityRole.ValueObject)
            {
                if (!IsTrue(nested, "schema.ownedCollection"))
                {
                    Report(diagnostics, "EF_VALUE_KIND_STORAGE_NOT_DECLARED", $"Nested ValueKind collection '{valueKind.Name}.{nested.Name}' requires collection ownership.", nested.Id.Value);
                    valid = false;
                }
                else
                {
                    PropertyInfo? member = clrType.GetProperty(MemberName(nested));
                    valid &= member is not null && ValidateJsonValueKind(model, item, member.PropertyType, diagnostics, visited);
                }
            }
        }
        return valid;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.General);
        return options;
    }

    private static bool SameTypeId(string left, string right)
    {
        static string Normalize(string value)
        {
            return value.Replace("global::", string.Empty, StringComparison.Ordinal);
        }

        return string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
    }

    private static Type JsonElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType()!;
        Type? enumerable = type.GetInterfaces().Append(type).FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0] ?? type;
    }

    private static EntityRole RoleOf(ObjectTypeDefinition type)
    {
        var role = Value(type.Annotations, "schema.role");
        return Enum.TryParse(role, true, out EntityRole parsed) ? parsed : type.Semantics.Role;
    }
    private static EfJsonColumn Json(PropertyDefinition p, PropertyInfo member, EfJsonShape shape, ObjectTypeDefinition storage, Type storageClrType)
    {
        return new() { PropertyId = p.Id.Value, MemberName = member.Name, ColumnName = member.Name, JsonShape = shape, ValueType = member.PropertyType, IsNullable = !p.Cardinality.IsRequired && (!member.PropertyType.IsValueType || Nullable.GetUnderlyingType(member.PropertyType) is not null), DeclaringClrType = member.DeclaringType!, StorageClrType = storageClrType, SemanticDeclaringTypeId = storage.Id.Value, StorageSemanticTypeId = storage.Id.Value };
    }

    private static bool IsStoredOn(ObjectTypeDefinition? semanticBase, Type clrType, PropertyDefinition property)
    {
        PropertyInfo[] matches = FindMembers(clrType, MemberName(property));
        return semanticBase is null ? matches.Length > 0 : matches.Any(member => member.DeclaringType == clrType);
    }

    private static PropertyInfo[] FindMembers(Type exposedOn, string name)
    {
        var matches = new List<PropertyInfo>();
        for (Type? current = exposedOn; current is not null && current != typeof(object); current = current.BaseType)
            matches.AddRange(current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(property => property.Name == name));
        return [.. matches];
    }

    private static PropertyInfo? ResolveMember(PropertyInfo[] matches, Type storageClrType, bool hasSemanticBase)
    {
        if (matches.Length == 1) return matches[0];
        if (hasSemanticBase)
        {
            PropertyInfo[] local = [.. matches.Where(member => member.DeclaringType == storageClrType)];
            if (local.Length == 1) return local[0];
        }
        return null;
    }

    private static Type? ResolveClrType(ObjectTypeDefinition type)
    {
        var name = Value(type.Annotations, "dotnet.clrType") ?? type.Id.Value;
        if (string.IsNullOrWhiteSpace(name)) return null;
        name = name.Replace("global::", string.Empty, StringComparison.Ordinal);
        return Type.GetType(name, false) ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, false)).FirstOrDefault(candidate => candidate is not null);
    }
    private static string MemberName(PropertyDefinition p)
    {
        return Value(p.Annotations, "dotnet.memberName") ?? p.Name;
    }

    private static bool IsTrue(PropertyDefinition p, string key)
    {
        return string.Equals(Value(p.Annotations, key), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Value(PropertyDefinition p, string key)
    {
        return Value(p.Annotations, key);
    }

    private static string? Value(AnnotationBag bag, string key)
    {
        return bag.Items.FirstOrDefault(a => a.Key.Value == key)?.Value?.ToString();
    }

    private static void Report(List<SchemaDiagnostic> diagnostics, string code, string message, string path)
    {
        diagnostics.Add(new() { Code = code, Message = message, Severity = SchemaDiagnosticSeverity.Error, Stage = SchemaDiagnosticStage.Projection, ModelPath = path, ProjectionTarget = ProjectionTarget.EfCore });
    }

}
