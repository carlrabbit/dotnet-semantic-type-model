using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCoreModelShapes;

#pragma warning disable CS1591
public static class ModelShapeModels
{
    public static TypeSchemaModel Flat()
    {
        return Build("Flat", [Entity<FlatOrder>([P(nameof(FlatOrder.Id), GuidId), P(nameof(FlatOrder.Number), StringId)], nameof(FlatOrder.Id))]);
    }

    public static TypeSchemaModel NonSemanticBaseScalar()
    {
        return Build("NonSemanticBaseScalar",
    [
        Structural<VersionedObject>([P(nameof(VersionedObject.SchemaVersion), IntId)]),
        Entity<VersionedOrder>([P(nameof(VersionedOrder.Id), GuidId), P(nameof(VersionedOrder.Number), StringId), P(nameof(VersionedObject.SchemaVersion), IntId)], nameof(VersionedOrder.Id)),
    ]);
    }

    public static TypeSchemaModel ExtensionData()
    {
        return Build("ExtensionData",
    [
        Structural<ExtensibleObject>([P(nameof(ExtensibleObject.ExtensionData), StringId, false, ("schema.extensionData", "true"))]),
        Entity<ExtensibleOrder>([P(nameof(ExtensibleOrder.Id), GuidId), P(nameof(ExtensibleObject.ExtensionData), StringId, false, ("schema.extensionData", "true"))], nameof(ExtensibleOrder.Id)),
    ]);
    }

    public static TypeSchemaModel OwnedObject()
    {
        return Build("OwnedObject",
    [
        Value<RetryPolicy>([P(nameof(RetryPolicy.Attempts), IntId)]),
        Value<SourceOptions>([P(nameof(SourceOptions.Endpoint), UriId), P(nameof(SourceOptions.Retry), TypeIdOf<RetryPolicy>(), false, ("schema.ownedObject", "true")), P(nameof(VersionedValue.Version), IntId)]),
        Structural<VersionedValue>([P(nameof(VersionedValue.Version), IntId)]),
        Structural<SourceConfiguredObject>([P(nameof(SourceConfiguredObject.Source), TypeIdOf<SourceOptions>(), false, ("schema.ownedObject", "true"))]),
        Entity<SourceOrder>([P(nameof(SourceOrder.Id), GuidId), P(nameof(SourceConfiguredObject.Source), TypeIdOf<SourceOptions>(), false, ("schema.ownedObject", "true"))], nameof(SourceOrder.Id)),
    ]);
    }

    public static TypeSchemaModel OwnedCollection()
    {
        return Build("OwnedCollection",
    [
        Value<DerivedField>([P(nameof(DerivedField.Name), StringId)]),
        Array<IReadOnlyList<DerivedField>>(TypeIdOf<DerivedField>()),
        Structural<FieldConfiguredObject>([P(nameof(FieldConfiguredObject.DerivedFields), TypeIdOf<IReadOnlyList<DerivedField>>(), ("schema.ownedCollection", "true"))]),
        Entity<FieldConfiguredOrder>([P(nameof(FieldConfiguredOrder.Id), GuidId), P(nameof(FieldConfiguredObject.DerivedFields), TypeIdOf<IReadOnlyList<DerivedField>>(), ("schema.ownedCollection", "true"))], nameof(FieldConfiguredOrder.Id)),
    ]);
    }

    public static TypeSchemaModel Tpt()
    {
        return Build("Tpt",
    [
        Structural<ExtensibleObject>([P(nameof(ExtensibleObject.ExtensionData), StringId, false, ("schema.extensionData", "true"))]),
        Structural<VersionedExtensibleObject>([P(nameof(VersionedExtensibleObject.SchemaVersion), IntId), P(nameof(ExtensibleObject.ExtensionData), StringId, false, ("schema.extensionData", "true"))]),
        Entity<Specification>([P(nameof(Specification.Id), GuidId), P(nameof(Specification.DisplayName), StringId), P(nameof(VersionedExtensibleObject.SchemaVersion), IntId), P(nameof(ExtensibleObject.ExtensionData), StringId, false, ("schema.extensionData", "true"))], nameof(Specification.Id)),
        Entity<ImportSpecification>([P(nameof(ImportSpecification.Id), GuidId), P(nameof(ImportSpecification.ImportName), StringId)], nameof(ImportSpecification.Id)),
        Entity<WorkflowSpecification>([P(nameof(WorkflowSpecification.Id), GuidId), P(nameof(WorkflowSpecification.WorkflowName), StringId)], nameof(WorkflowSpecification.Id)),
    ]);
    }

    public static TypeSchemaModel ReusedNestedValueKind()
    {
        return Build("ReusedNestedValueKind",
    [
        Structural<VersionedValue>([P(nameof(VersionedValue.Version), IntId)]),
        Value<RetryPolicy>([P(nameof(RetryPolicy.Attempts), IntId)]),
        Value<SourceOptions>([P(nameof(SourceOptions.Endpoint), UriId), P(nameof(SourceOptions.Retry), TypeIdOf<RetryPolicy>(), false, ("schema.ownedObject", "true")), P(nameof(VersionedValue.Version), IntId)]),
        Entity<SourceConsumer>([P(nameof(SourceConsumer.Id), GuidId), P(nameof(SourceConsumer.Source), TypeIdOf<SourceOptions>(), ("schema.ownedObject", "true"))], nameof(SourceConsumer.Id)),
        Entity<AlternateSourceConsumer>([P(nameof(AlternateSourceConsumer.Id), GuidId), P(nameof(AlternateSourceConsumer.Source), TypeIdOf<SourceOptions>(), ("schema.ownedObject", "true"))], nameof(AlternateSourceConsumer.Id)),
    ]);
    }

    public static TypeSchemaModel Hidden()
    {
        return Build("Hidden",
    [
        Structural<HiddenBase>([P(nameof(HiddenBase.Code), StringId)]),
        Entity<HiddenOrder>([P(nameof(HiddenOrder.Id), GuidId), P(nameof(HiddenOrder.Code), StringId)], nameof(HiddenOrder.Id)),
    ]);
    }

    public static TypeSchemaModel SemanticDuplicate()
    {
        return Build("SemanticDuplicate",
    [
        Entity<SemanticDuplicateBase>([P(nameof(SemanticDuplicateBase.Id), GuidId), P(nameof(SemanticDuplicateBase.Name), StringId)], nameof(SemanticDuplicateBase.Id)),
        Entity<SemanticDuplicateDerived>([P(nameof(SemanticDuplicateDerived.Id), GuidId), P(nameof(SemanticDuplicateDerived.Name), StringId)], nameof(SemanticDuplicateDerived.Id)),
    ]);
    }

    public static TypeSchemaModel SemanticChain()
    {
        return Build("SemanticChain",
    [
        Structural<StructuralGrandbase>([P(nameof(StructuralGrandbase.Tenant), StringId)]),
        Entity<SemanticChainBase>([P(nameof(SemanticChainBase.Id), GuidId), P(nameof(StructuralGrandbase.Tenant), StringId)], nameof(SemanticChainBase.Id)),
        Entity<SemanticChainDerived>([P(nameof(SemanticChainDerived.Id), GuidId)], nameof(SemanticChainDerived.Id)),
    ]);
    }

    public static TypeSchemaModel JsonInheritance()
    {
        return Build("JsonInheritance",
    [
        Structural<VersionedValue>([P(nameof(VersionedValue.Version), IntId)]),
        Value<RetryPolicy>([P(nameof(RetryPolicy.Attempts), IntId)]),
        Value<SourceOptions>([P(nameof(SourceOptions.Endpoint), UriId), P(nameof(SourceOptions.Retry), TypeIdOf<RetryPolicy>(), false, ("schema.ownedObject", "true")), P(nameof(VersionedValue.Version), IntId)]),
        Entity<JsonBase>([P(nameof(JsonBase.Id), GuidId), P(nameof(JsonBase.OptionalSource), TypeIdOf<SourceOptions>(), false, ("schema.ownedObject", "true"))], nameof(JsonBase.Id)),
        Entity<JsonDerived>([P(nameof(JsonDerived.Id), GuidId), P(nameof(JsonDerived.RequiredSource), TypeIdOf<SourceOptions>(), ("schema.ownedObject", "true"))], nameof(JsonDerived.Id)),
    ]);
    }

    private static readonly TypeId GuidId = TypeIdOf<Guid>();
    private static readonly TypeId StringId = TypeIdOf<string>();
    private static readonly TypeId IntId = TypeIdOf<int>();
    private static readonly TypeId UriId = TypeIdOf<Uri>();

    private static TypeSchemaModel Build(string id, IReadOnlyList<TypeDefinition> definitions)
    {
        TypeDefinition[] types = [Scalar<Guid>(ScalarKind.Guid), Scalar<string>(ScalarKind.String), Scalar<int>(ScalarKind.Integer), Scalar<Uri>(ScalarKind.String), .. definitions];
        return new() { Id = new(id), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new() };
    }

    private static ScalarTypeDefinition Scalar<T>(ScalarKind kind)
    {
        return new() { Id = TypeIdOf<T>(), Name = typeof(T).Name, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = kind, Annotations = Clr(typeof(T)) };
    }

    private static ObjectTypeDefinition Entity<T>(IReadOnlyList<PropertyDefinition> properties, string key)
    {
        return Object<T>(EntityRole.Entity, properties) with { Keys = [new KeyDefinition { Name = $"PK_{typeof(T).Name}", Kind = KeyKind.Primary, Properties = [new(new(key))], Annotations = new() }] };
    }

    private static ObjectTypeDefinition Value<T>(IReadOnlyList<PropertyDefinition> properties)
    {
        return Object<T>(EntityRole.ValueObject, properties);
    }

    private static ObjectTypeDefinition Structural<T>(IReadOnlyList<PropertyDefinition> properties)
    {
        return Object<T>(EntityRole.Unspecified, properties);
    }

    private static ObjectTypeDefinition Object<T>(EntityRole role, IReadOnlyList<PropertyDefinition> properties)
    {
        return new() { Id = TypeIdOf<T>(), Name = typeof(T).Name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Clr(typeof(T)), Semantics = new() { Role = role, IsValueObject = role == EntityRole.ValueObject }, Properties = properties, Keys = [] };
    }

    private static ArrayTypeDefinition Array<T>(TypeId item)
    {
        return new() { Id = TypeIdOf<T>(), Name = typeof(T).Name, Kind = TypeKind.Array, Nullability = Nullability.NonNullable, ItemType = new(item), Annotations = Clr(typeof(T)) };
    }

    private static PropertyDefinition P(string name, TypeId type, params (string Key, string Value)[] annotations)
    {
        return P(name, type, true, annotations);
    }

    private static PropertyDefinition P(string name, TypeId type, bool required, params (string Key, string Value)[] annotations)
    {
        return new() { Id = new(name), Name = name, Type = new(type), Cardinality = new() { IsRequired = required }, Mutability = null, Constraints = new(), Annotations = new() { Items = [new Annotation { Key = new("dotnet.memberName"), Value = name, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }, .. annotations.Select(annotation => new Annotation { Key = new(annotation.Key), Value = annotation.Value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared })] } };
    }

    private static TypeId TypeIdOf<T>()
    {
        return new(typeof(T).FullName!);
    }

    private static AnnotationBag Clr(Type type)
    {
        List<Annotation> annotations = [new Annotation { Key = new("dotnet.clrType"), Value = type.AssemblyQualifiedName, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }];
        if (type.BaseType is { } baseType && baseType != typeof(object))
        {
            annotations.Add(new Annotation { Key = new("dotnet.baseType"), Value = baseType.FullName, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared });
        }
        return new() { Items = annotations };
    }
}
#pragma warning restore CS1591
