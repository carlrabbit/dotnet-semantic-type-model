#pragma warning disable IDE0011, IDE0058, IDE0305
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.EFCore;

/// <summary>Derives and applies the opinionated EF relational contract.</summary>
public static class EfRelationalExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

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
            IEnumerable<PropertyDefinition> declared = source.Properties.Where(p => clrType.GetProperty(MemberName(p))?.DeclaringType == clrType || baseSource is null);
            foreach (PropertyDefinition property in declared)
            {
                PropertyInfo? member = clrType.GetProperty(MemberName(property));
                if (member is null) continue;
                TypeDefinition? target = effectiveModel.Types.FirstOrDefault(t => t.Id == property.Type.Id);
                var extension = IsTrue(property, "schema.extensionData");
                var ownedObject = IsTrue(property, "schema.ownedObject") || string.Equals(Value(property, "schema.ownership.kind"), "object", StringComparison.OrdinalIgnoreCase);
                var ownedCollection = IsTrue(property, "schema.ownedCollection") || string.Equals(Value(property, "schema.ownership.kind"), "collection", StringComparison.OrdinalIgnoreCase);
                if (extension)
                {
                    json.Add(Json(property, member, EfJsonShape.ExtensionData));
                }
                else if (ownedObject || ownedCollection)
                {
                    TypeDefinition? valueTarget = target is ArrayTypeDefinition array ? effectiveModel.Types.FirstOrDefault(t => t.Id == array.ItemType.Id) : target;
                    if (valueTarget is ObjectTypeDefinition owned && RoleOf(owned) == EntityRole.Entity)
                        Report(diagnostics, "EF_ENTITY_CANNOT_BE_OWNED", $"Semantic entity '{owned.Name}' cannot be owned by '{source.Name}.{property.Name}'.", property.Id.Value);
                    else if (valueTarget is not ObjectTypeDefinition valueKind || RoleOf(valueKind) != EntityRole.ValueObject)
                        Report(diagnostics, "EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE", $"Owned member '{source.Name}.{property.Name}' must target a semantic ValueKind.", property.Id.Value);
                    else if (ValidateJsonValueKind(effectiveModel, valueKind, member.PropertyType, diagnostics, []))
                        json.Add(Json(property, member, ownedCollection ? EfJsonShape.Array : EfJsonShape.Object));
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
                    var column = new EfScalarColumn { PropertyId = property.Id.Value, MemberName = member.Name, ColumnName = member.Name, ClrType = member.PropertyType, ProviderType = provider, IsNullable = !property.Cardinality.IsRequired && (!member.PropertyType.IsValueType || Nullable.GetUnderlyingType(member.PropertyType) is not null) };
                    (provider == typeof(byte[]) ? binary : scalars).Add(column);
                }
                else if (target is ScalarTypeDefinition) Report(diagnostics, "EF_STRONG_ID_SHAPE_NOT_SUPPORTED", $"Strong identifier member '{source.Name}.{property.Name}' must expose one supported scalar Value property and matching constructor.", property.Id.Value);
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

    /// <summary>Applies a previously derived relational model to a CLR-backed ModelBuilder.</summary>
    public static EfRelationalApplicationResult ApplySemanticRelationalModel(this ModelBuilder modelBuilder, EfRelationalModel model, string? defaultSchema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(model);
        var diagnostics = new List<SchemaDiagnostic>(model.Diagnostics);
        var allowed = model.Entities.Select(entity => entity.ClrType).ToHashSet();
        var contained = model.Entities.SelectMany(entity => entity.JsonColumns).Select(column => JsonElementType(column.ValueType)).ToHashSet();
        Type[] unexpected = [.. modelBuilder.Model.GetEntityTypes().Select(entity => entity.ClrType).Where(type => !allowed.Contains(type) && !contained.Contains(type))];
        foreach (Type type in unexpected)
        {
            Report(diagnostics, "EF_UNEXPECTED_CONVENTION_ENTITY", $"EF convention entity '{type.FullName}' is not an explicitly projected semantic entity.", type.FullName ?? type.Name);
        }
        if (diagnostics.Any(diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error))
        {
            return new EfRelationalApplicationResult { Diagnostics = diagnostics };
        }
        foreach (EfEntity entity in model.Entities.OrderBy(entity => entity.BaseEntityId is null ? 0 : 1))
        {
            EntityTypeBuilder suppress = modelBuilder.Entity(entity.ClrType);
            foreach (PropertyInfo property in entity.ClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.DeclaringType == entity.ClrType)) _ = suppress.Ignore(property.Name);
        }
        foreach (IMutableEntityType? discovered in modelBuilder.Model.GetEntityTypes().Where(entity => contained.Contains(entity.ClrType)).ToArray()) _ = modelBuilder.Model.RemoveEntityType(discovered);
        foreach (EfEntity entity in model.Entities.OrderBy(entity => entity.BaseEntityId is null ? 0 : 1).ThenBy(entity => entity.Table, StringComparer.Ordinal))
        {
            EntityTypeBuilder builder = modelBuilder.Entity(entity.ClrType);
            _ = builder.ToTable(entity.Table, defaultSchema);
            foreach (PropertyInfo property in entity.ClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.DeclaringType == entity.ClrType)) _ = builder.Ignore(property.Name);
            foreach (EfScalarColumn column in entity.ScalarColumns.Concat(entity.BinaryColumns)) ConfigureScalar(builder, column);
            foreach (EfJsonColumn column in entity.JsonColumns) ConfigureJson(builder, column);
            if (entity.BaseEntityId is null && entity.Key.Count > 0) _ = builder.HasKey(entity.Key.ToArray());
        }
        foreach (EfEntity root in model.Entities.Where(entity => entity.BaseEntityId is null && model.Entities.Any(candidate => candidate.BaseEntityId == entity.SemanticTypeId))) _ = modelBuilder.Entity(root.ClrType).UseTptMappingStrategy();
        foreach (IMutableEntityType? discovered in modelBuilder.Model.GetEntityTypes().Where(entity => !allowed.Contains(entity.ClrType)).ToArray()) _ = modelBuilder.Model.RemoveEntityType(discovered);
        return new EfRelationalApplicationResult { Diagnostics = diagnostics };
    }

    /// <summary>Derives and applies the relational model through the single supported application path.</summary>
    public static SemanticDerivationResult<EfRelationalModel> ApplySemanticTypeModel(this ModelBuilder modelBuilder, TypeSchemaModel model, Action<EfRelationalOptions>? configure = null)
    {
        SemanticDerivationResult<EfRelationalModel> result = model.DeriveEfRelationalModel(configure);
        string? schema = null; if (configure is not null) { var options = new EfRelationalOptions(); configure(options); schema = options.DefaultSchema; }
        EfRelationalApplicationResult application = modelBuilder.ApplySemanticRelationalModel(result.Model, schema);
        return result with { Diagnostics = application.Diagnostics };
    }

    private static void ConfigureScalar(EntityTypeBuilder builder, EfScalarColumn column)
    {
        PropertyBuilder property = builder.Property(column.ClrType, column.MemberName).HasColumnName(column.ColumnName).IsRequired(!column.IsNullable);
        Type actual = Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType;
        if (actual.IsEnum) property.HasConversion<string>();
        else if (actual == typeof(Uri)) property.HasConversion(new ValueConverter<Uri, string>(v => v.ToString(), v => new Uri(v, UriKind.RelativeOrAbsolute)));
        else if (actual == typeof(ReadOnlyMemory<byte>)) property.HasConversion(new ValueConverter<ReadOnlyMemory<byte>, byte[]>(value => value.ToArray(), value => new ReadOnlyMemory<byte>(value)));
        else if (actual != column.ProviderType) property.HasConversion(CreateStrongConverter(column.ClrType, column.ProviderType));
    }

    private static void ConfigureJson(EntityTypeBuilder builder, EfJsonColumn column)
    {
        PropertyBuilder property = builder.Property(column.ValueType, column.MemberName).HasColumnName(column.ColumnName).IsRequired(!column.IsNullable);
        property.HasConversion(CreateJsonConverter(column.ValueType));
        property.Metadata.SetValueComparer(CreateJsonComparer(column.ValueType));
    }

    private static ValueConverter CreateJsonConverter(Type type)
    {
        ParameterExpression value = Expression.Parameter(type, "value");
        MethodInfo serialize = typeof(EfRelationalExtensions).GetMethod(nameof(Serialize), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
        MethodInfo deserialize = typeof(EfRelationalExtensions).GetMethod(nameof(Deserialize), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
        ParameterExpression json = Expression.Parameter(typeof(string), "json");
        Type converterType = typeof(ValueConverter<,>).MakeGenericType(type, typeof(string));
        return (ValueConverter)Activator.CreateInstance(converterType, Expression.Lambda(Expression.Call(serialize, value), value), Expression.Lambda(Expression.Call(deserialize, json), json), null)!;
    }
    private static ValueComparer CreateJsonComparer(Type type)
    {
        return (ValueComparer)Activator.CreateInstance(typeof(JsonValueComparer<>).MakeGenericType(type))!;
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }

    private static ValueConverter CreateStrongConverter(Type wrapper, Type provider)
    {
        Type actual = Nullable.GetUnderlyingType(wrapper) ?? wrapper;
        PropertyInfo? valueProperty = actual.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        ConstructorInfo? constructor = actual.GetConstructor([provider]);
        System.Diagnostics.Debug.Assert(valueProperty is not null && constructor is not null, "Strong identifier shape is validated during derivation.");
        ParameterExpression input = Expression.Parameter(wrapper, "value");
        Expression unwrapped = wrapper == actual ? input : Expression.Property(input, "Value");
        LambdaExpression to = Expression.Lambda(Expression.Property(unwrapped, valueProperty!), input);
        ParameterExpression stored = Expression.Parameter(provider, "value");
        Expression created = Expression.New(constructor!, stored);
        LambdaExpression from = Expression.Lambda(wrapper == actual ? created : Expression.Convert(created, wrapper), stored);
        Type converterType = typeof(ValueConverter<,>).MakeGenericType(wrapper, provider);
        return (ValueConverter)Activator.CreateInstance(converterType, to, from, null)!;
    }

    private static bool TryProviderType(Type type, out Type provider)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual.IsEnum || actual == typeof(Uri)) { provider = typeof(string); return true; }
        Type[] supported = [typeof(string), typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(Guid), typeof(DateOnly), typeof(TimeOnly), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(byte[])];
        if (supported.Contains(actual)) { provider = actual; return true; }
        if (actual == typeof(ReadOnlyMemory<byte>)) { provider = typeof(byte[]); return true; }
        PropertyInfo? value = actual.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        if (value is not null && actual.GetConstructor([value.PropertyType]) is not null && supported.Contains(value.PropertyType)) { provider = value.PropertyType; return true; }
        provider = typeof(void); return false;
    }
    private static bool ValidateJsonValueKind(TypeSchemaModel model, ObjectTypeDefinition valueKind, Type declaredClrType, List<SchemaDiagnostic> diagnostics, HashSet<TypeId> visited)
    {
        if (!visited.Add(valueKind.Id)) return true;
        Type clrType = JsonElementType(declaredClrType);
        try
        {
            var value = clrType.IsValueType ? Activator.CreateInstance(clrType) : System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(clrType);
            _ = JsonSerializer.Serialize(value, clrType, JsonOptions);
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
    private static EfJsonColumn Json(PropertyDefinition p, PropertyInfo member, EfJsonShape shape)
    {
        return new() { PropertyId = p.Id.Value, MemberName = member.Name, ColumnName = member.Name, JsonShape = shape, ValueType = member.PropertyType, IsNullable = !p.Cardinality.IsRequired && (!member.PropertyType.IsValueType || Nullable.GetUnderlyingType(member.PropertyType) is not null) };
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

    private sealed class JsonValueComparer<T>() : ValueComparer<T>((a, b) => Serialize(a).Equals(Serialize(b), StringComparison.Ordinal), value => Serialize(value).GetHashCode(StringComparison.Ordinal), value => Deserialize<T>(Serialize(value)));
}
