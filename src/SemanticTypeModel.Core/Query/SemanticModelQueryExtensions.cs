#pragma warning disable IDE0046
using System.Globalization;
using System.Linq.Expressions;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.Core.Query;

/// <summary>
/// Provides deterministic query helpers for canonical semantic type models.
/// </summary>
public static class SemanticModelQueryExtensions
{
    private const string ClrMemberNameAnnotation = "dotnet.memberName";
    private const string SemanticTypeAnnotation = "semantic.type";
    private const string SemanticPrimitiveAnnotation = "semantic.primitive";

    /// <summary>
    /// Returns all canonical semantic model types in deterministic identifier order.
    /// </summary>
    public static IEnumerable<TypeDefinition> Types(this TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Attempts to find a canonical semantic model type by canonical string identifier.
    /// </summary>
    public static bool TryGetType(this TypeSchemaModel model, string identifier, out TypeDefinition? type)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        return model.TypesById.TryGetValue(new TypeId(identifier), out type);
    }

    /// <summary>
    /// Requires a canonical semantic model type by canonical string identifier.
    /// </summary>
    public static TypeDefinition RequireType(this TypeSchemaModel model, string identifier)
    {
        if (TryGetType(model, identifier, out TypeDefinition? type))
        {
            return type!;
        }

        throw new InvalidOperationException(BuildMissingTypeMessage("string identifier", identifier, model.Types().Select(static type => type.Id.Value)));
    }

    /// <summary>
    /// Attempts to find a canonical semantic model type for the CLR type <typeparamref name="T"/>.
    /// </summary>
    public static bool TryGetType<T>(this TypeSchemaModel model, out TypeDefinition? type)
    {
        return TryGetType(model, GetClrTypeIdentifier(typeof(T)), out type);
    }

    /// <summary>
    /// Requires a canonical semantic model type for the CLR type <typeparamref name="T"/>.
    /// </summary>
    public static TypeDefinition RequireType<T>(this TypeSchemaModel model)
    {
        var identifier = GetClrTypeIdentifier(typeof(T));
        if (TryGetType(model, identifier, out TypeDefinition? type))
        {
            return type!;
        }

        throw new InvalidOperationException(BuildMissingTypeMessage("CLR type", typeof(T).FullName ?? typeof(T).Name, model.Types().Select(static type => type.Id.Value), identifier));
    }

    /// <summary>
    /// Returns all canonical object properties in deterministic model-path order.
    /// </summary>
    public static IEnumerable<PropertyDefinition> Properties(this TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Types()
            .OfType<ObjectTypeDefinition>()
            .SelectMany(static type => type.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal));
    }

    /// <summary>
    /// Returns all canonical object types marked as envelope wrappers.
    /// </summary>
    public static IEnumerable<ObjectTypeDefinition> Envelopes(this TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Types()
            .OfType<ObjectTypeDefinition>()
            .Where(static type => HasAnnotation(type.Annotations, CoreSemanticAnnotationKeys.Envelope, "true"));
    }

    /// <summary>
    /// Returns envelope payload properties in deterministic model order.
    /// </summary>
    public static IEnumerable<PropertyDefinition> EnvelopePayloads(this ObjectTypeDefinition envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return envelope.Properties
            .Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.EnvelopePayload, "true"))
            .OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns envelope metadata properties in deterministic model order.
    /// </summary>
    public static IEnumerable<PropertyDefinition> EnvelopeMetadata(this ObjectTypeDefinition envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return envelope.Properties
            .Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.EnvelopeMetadata, "true"))
            .OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>Returns object properties marked with ownership semantics.</summary>
    public static IEnumerable<PropertyDefinition> OwnedMembers(this ObjectTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Properties.Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.Ownership, "true") || HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedObject, "true") || HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedCollection, "true")).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>Returns version or revision members in deterministic order.</summary>
    public static IEnumerable<PropertyDefinition> VersionMembers(this ObjectTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Properties.Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.Version, "true") || HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.Revision, "true") || HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.CurrentVersion, "true")).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>Returns temporal-validity endpoint members in deterministic order.</summary>
    public static IEnumerable<PropertyDefinition> TemporalValidityMembers(this ObjectTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Properties.Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ValidFrom, "true") || HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ValidTo, "true")).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>Returns lifecycle-state members in deterministic order.</summary>
    public static IEnumerable<PropertyDefinition> LifecycleStateMembers(this ObjectTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Properties.Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.LifecycleState, "true")).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>Returns extension-data members in deterministic order.</summary>
    public static IEnumerable<PropertyDefinition> ExtensionDataMembers(this ObjectTypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.Properties.Where(static property => HasAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData, "true")).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns properties for the canonical object type identified by <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<PropertyDefinition> PropertiesOf<T>(this TypeSchemaModel model)
    {
        return RequireObjectType<T>(model).Properties.OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Attempts to find a canonical property by canonical type identifier and property name.
    /// </summary>
    public static bool TryGetProperty(this TypeSchemaModel model, string typeIdentifier, string propertyName, out PropertyDefinition? property)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrEmpty(typeIdentifier);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        property = null;

        if (!TryGetType(model, typeIdentifier, out TypeDefinition? type) || type is not ObjectTypeDefinition objectType)
        {
            return false;
        }

        property = objectType.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, propertyName, StringComparison.Ordinal));
        return property is not null;
    }

    /// <summary>
    /// Requires a canonical property by canonical type identifier and property name.
    /// </summary>
    public static PropertyDefinition RequireProperty(this TypeSchemaModel model, string typeIdentifier, string propertyName)
    {
        if (TryGetProperty(model, typeIdentifier, propertyName, out PropertyDefinition? property))
        {
            return property!;
        }

        var candidates = TryGetType(model, typeIdentifier, out TypeDefinition? type) && type is ObjectTypeDefinition objectType
            ? FormatCandidates(objectType.Properties.Select(static prop => prop.Name))
            : "none; type was not found or is not an object type";
        throw new InvalidOperationException($"Property query by string identifier failed. Type '{typeIdentifier}' property '{propertyName}' was not found. Available properties: {candidates}.");
    }

    /// <summary>
    /// Attempts to find a canonical property by CLR property expression.
    /// </summary>
    public static bool TryGetProperty<T>(this TypeSchemaModel model, Expression<Func<T, object?>> propertyExpression, out PropertyDefinition? property)
    {
        var memberName = GetSimplePropertyName(propertyExpression);
        property = RequireObjectType<T>(model).Properties.FirstOrDefault(candidate => PropertyMatchesMember(candidate, memberName));
        return property is not null;
    }

    /// <summary>
    /// Requires a canonical property by CLR property expression.
    /// </summary>
    public static PropertyDefinition RequireProperty<T>(this TypeSchemaModel model, Expression<Func<T, object?>> propertyExpression)
    {
        var memberName = GetSimplePropertyName(propertyExpression);
        ObjectTypeDefinition objectType = RequireObjectType<T>(model);
        PropertyDefinition? property = objectType.Properties.FirstOrDefault(candidate => PropertyMatchesMember(candidate, memberName));
        return property ?? throw new InvalidOperationException($"Property query by CLR expression failed. CLR type '{typeof(T).FullName}' member '{memberName}' was not found in model type '{objectType.Id.Value}'. Use RequireProperty(\"{objectType.Id.Value}\", \"{memberName}\") when CLR member metadata is unavailable. Available properties: {FormatCandidates(objectType.Properties.Select(static prop => prop.Name))}.");
    }

    /// <summary>
    /// Filters canonical types by semantic primitive.
    /// </summary>
    public static IEnumerable<TypeDefinition> WithSemanticType(this IEnumerable<TypeDefinition> types, string semanticPrimitive)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentException.ThrowIfNullOrEmpty(semanticPrimitive);
        return types.Where(type => TypeHasSemantic(type, semanticPrimitive)).OrderBy(static type => type.Id.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical properties by semantic primitive.
    /// </summary>
    public static IEnumerable<PropertyDefinition> WithSemantic(this IEnumerable<PropertyDefinition> properties, string semanticPrimitive)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentException.ThrowIfNullOrEmpty(semanticPrimitive);
        return properties.Where(property => HasAnnotation(property.Annotations, SemanticTypeAnnotation, semanticPrimitive) || HasAnnotation(property.Annotations, SemanticPrimitiveAnnotation, semanticPrimitive))
            .OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical types by annotation key.
    /// </summary>
    public static IEnumerable<TypeDefinition> WithAnnotation(this IEnumerable<TypeDefinition> types, string annotationKey)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentException.ThrowIfNullOrEmpty(annotationKey);
        return types.Where(type => HasAnnotation(type.Annotations, annotationKey)).OrderBy(static type => type.Id.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical types by annotation key and value.
    /// </summary>
    public static IEnumerable<TypeDefinition> WithAnnotation(this IEnumerable<TypeDefinition> types, string annotationKey, object? value)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentException.ThrowIfNullOrEmpty(annotationKey);
        return types.Where(type => HasAnnotation(type.Annotations, annotationKey, value)).OrderBy(static type => type.Id.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical properties by annotation key.
    /// </summary>
    public static IEnumerable<PropertyDefinition> WithAnnotation(this IEnumerable<PropertyDefinition> properties, string annotationKey)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentException.ThrowIfNullOrEmpty(annotationKey);
        return properties.Where(property => HasAnnotation(property.Annotations, annotationKey)).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical properties by annotation key and value.
    /// </summary>
    public static IEnumerable<PropertyDefinition> WithAnnotation(this IEnumerable<PropertyDefinition> properties, string annotationKey, object? value)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentException.ThrowIfNullOrEmpty(annotationKey);
        return properties.Where(property => HasAnnotation(property.Annotations, annotationKey, value)).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical properties by constraint name.
    /// </summary>
    public static IEnumerable<PropertyDefinition> WithConstraint(this IEnumerable<PropertyDefinition> properties, string constraintName)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentException.ThrowIfNullOrEmpty(constraintName);
        return properties.Where(property => HasConstraint(property.Constraints, constraintName)).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Filters canonical properties by constraint name and value.
    /// </summary>
    public static IEnumerable<PropertyDefinition> WithConstraint(this IEnumerable<PropertyDefinition> properties, string constraintName, object? value)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentException.ThrowIfNullOrEmpty(constraintName);
        return properties.Where(property => HasConstraint(property.Constraints, constraintName, value)).OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    private static ObjectTypeDefinition RequireObjectType<T>(TypeSchemaModel model)
    {
        TypeDefinition type = RequireType<T>(model);
        return type as ObjectTypeDefinition ?? throw new InvalidOperationException($"Type query by CLR type '{typeof(T).FullName}' resolved '{type.Id.Value}', but that model type is '{type.Kind}' and not an object type.");
    }

    private static bool HasConstraint(ConstraintSet constraints, string constraintName)
    {
        return GetConstraintValue(constraints, constraintName, out _);
    }

    private static bool HasConstraint(ConstraintSet constraints, string constraintName, object? value)
    {
        var expected = Convert.ToString(value, CultureInfo.InvariantCulture);
        return GetConstraintValue(constraints, constraintName, out var actual)
            && string.Equals(Convert.ToString(actual, CultureInfo.InvariantCulture), expected, StringComparison.Ordinal);
    }

    private static bool GetConstraintValue(ConstraintSet constraints, string constraintName, out object? value)
    {
        value = constraintName switch
        {
            "string.minLength" => constraints.String?.MinLength,
            "string.maxLength" => constraints.String?.MaxLength,
            "string.pattern" => constraints.String?.Pattern,
            "numeric.minimum" => constraints.Numeric?.Minimum,
            "numeric.maximum" => constraints.Numeric?.Maximum,
            "numeric.multipleOf" => constraints.Numeric?.MultipleOf,
            "array.minItems" => constraints.Array?.MinItems,
            "array.maxItems" => constraints.Array?.MaxItems,
            "array.uniqueItems" => constraints.Array?.UniqueItems == true ? true : null,
            _ => constraints.Custom.FirstOrDefault(custom => string.Equals(custom.Name, constraintName, StringComparison.Ordinal))?.Value,
        };

        return value is not null;
    }

    private static bool TypeHasSemantic(TypeDefinition type, string semanticPrimitive)
    {
        if (HasAnnotation(type.Annotations, SemanticTypeAnnotation, semanticPrimitive) || HasAnnotation(type.Annotations, SemanticPrimitiveAnnotation, semanticPrimitive))
        {
            return true;
        }

        return type is ObjectTypeDefinition objectType
            && (string.Equals(objectType.Semantics.Role.ToString(), semanticPrimitive, StringComparison.Ordinal)
                || (objectType.Semantics.IsValueObject && string.Equals("ValueObject", semanticPrimitive, StringComparison.Ordinal))
                || (objectType.Semantics.IsAggregateRoot && string.Equals("AggregateRoot", semanticPrimitive, StringComparison.Ordinal)));
    }

    private static bool PropertyMatchesMember(PropertyDefinition property, string memberName)
    {
        return string.Equals(property.Name, memberName, StringComparison.Ordinal) || HasAnnotation(property.Annotations, ClrMemberNameAnnotation, memberName);
    }

    private static bool HasAnnotation(AnnotationBag annotations, string key)
    {
        return annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal));
    }

    private static bool HasAnnotation(AnnotationBag annotations, string key, object? value)
    {
        var expected = Convert.ToString(value, CultureInfo.InvariantCulture);
        return annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal) && string.Equals(Convert.ToString(annotation.Value, CultureInfo.InvariantCulture), expected, StringComparison.Ordinal));
    }

    private static string GetSimplePropertyName<T>(Expression<Func<T, object?>> propertyExpression)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        Expression body = propertyExpression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Expression: ParameterExpression, Member.MemberType: System.Reflection.MemberTypes.Property } member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException($"Property expression '{propertyExpression}' is unsupported. Use a simple property access expression such as x => x.Email; method calls, anonymous objects, and nested property paths are not supported.", nameof(propertyExpression));
    }

    private static string GetClrTypeIdentifier(Type type)
    {
        if (!string.IsNullOrEmpty(type.FullName))
        {
            return "global::" + type.FullName.Replace('+', '.');
        }

        return "global::" + type.Name;
    }

    private static string BuildMissingTypeMessage(string queryKind, string requested, IEnumerable<string?> candidates, string? expectedIdentifier = null)
    {
        var expected = expectedIdentifier is null ? string.Empty : $" Expected canonical identifier '{expectedIdentifier}'.";
        return $"Type query by {queryKind} failed. Requested '{requested}' was not found.{expected} Use string fallback RequireType(\"canonical.identifier\") or TryGetType(\"canonical.identifier\", out var type) when CLR metadata is unavailable. Available types: {FormatCandidates(candidates)}.";
    }

    private static string FormatCandidates(IEnumerable<string?> candidates)
    {
        string[] ordered = [.. candidates.Where(static candidate => !string.IsNullOrWhiteSpace(candidate)).Distinct(StringComparer.Ordinal).OrderBy(static candidate => candidate, StringComparer.Ordinal).Take(8)!];
        return ordered.Length == 0 ? "none" : string.Join(", ", ordered);
    }
}

#pragma warning restore IDE0046
