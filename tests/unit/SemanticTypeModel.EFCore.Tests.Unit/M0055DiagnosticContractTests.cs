#pragma warning disable CA1707, CS1591
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.EFCore.Tests.Unit;

public sealed class M0055DiagnosticContractTests
{
    [Test]
    public async Task Derivation_emits_every_unsupported_shape_diagnostic_without_throwing()
    {
        TypeSchemaModel model = CreateDiagnosticModel();
        SemanticDerivationResult<EfRelationalModel> result = model.DeriveEfRelationalModel();
        string[] required =
        [
            "EF_ENTITY_CANNOT_BE_OWNED",
            "EF_VALUE_KIND_STORAGE_NOT_DECLARED",
            "EF_ENTITY_REFERENCE_REQUIRES_IDENTIFIER",
            "EF_ENTITY_COLLECTION_REQUIRES_IDENTIFIER_SHAPE",
            "EF_DICTIONARY_STORAGE_NOT_SUPPORTED",
            "EF_UNSUPPORTED_SCALAR_TYPE",
            "EF_ENTITY_KEY_REQUIRED",
            "EF_STRONG_ID_SHAPE_NOT_SUPPORTED",
            "EF_JSON_VALUE_TYPE_NOT_SERIALIZABLE",
            "EF_DUPLICATE_TABLE_NAME",
            "EF_DUPLICATE_COLUMN_NAME",
        ];
        foreach (var code in required)
        {
            _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == code)).IsTrue();
        }
    }

    [Test]
    public async Task Invalid_derivation_is_rejected_without_ModelBuilder_mutation_or_exception()
    {
        SemanticDerivationResult<EfRelationalModel> derivation = CreateDiagnosticModel().DeriveEfRelationalModel();
        var builder = new Microsoft.EntityFrameworkCore.ModelBuilder();
        EfRelationalApplicationResult application = builder.ApplySemanticRelationalModel(derivation.Model);
        _ = await Assert.That(application.Diagnostics.Any(diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsTrue();
        _ = await Assert.That(builder.Model.GetEntityTypes()).IsEmpty();
    }

    private static TypeSchemaModel CreateDiagnosticModel()
    {
        ScalarTypeDefinition guid = Scalar<Guid>(ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar<string>(ScalarKind.String);
        ScalarTypeDefinition broken = Scalar<BrokenStrongId>(ScalarKind.Guid);
        ObjectTypeDefinition target = Object<TargetEntity>(EntityRole.Entity, "TargetEntity", [Property<TargetEntity>(nameof(TargetEntity.Id), guid.Id)], nameof(TargetEntity.Id));
        ObjectTypeDefinition valueKind = Object<DiagnosticValueKind>(EntityRole.ValueObject, "DiagnosticValueKind", [Property<DiagnosticValueKind>(nameof(DiagnosticValueKind.Name), text.Id)]);
        ObjectTypeDefinition badJson = Object<BadJsonValue>(EntityRole.ValueObject, "BadJsonValue",
        [
            Property<BadJsonValue>(nameof(BadJsonValue.Callback), new TypeId(typeof(Action).FullName!)),
            Property<BadJsonValue>(nameof(BadJsonValue.Entity), target.Id),
        ]);
        ArrayTypeDefinition entityArray = Array<IReadOnlyList<TargetEntity>>(target.Id);
        DictionaryTypeDefinition dictionary = Dictionary<IReadOnlyDictionary<string, string>>(text.Id, text.Id);
        ObjectTypeDefinition diagnostic = Object<DiagnosticEntity>(EntityRole.Entity, "DiagnosticEntity",
        [
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.Id), guid.Id),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.OwnedEntity), target.Id, ("schema.ownedObject", "true")),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.UndeclaredValue), valueKind.Id),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.EntityReference), target.Id),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.EntityCollection), entityArray.Id),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.Dictionary), dictionary.Id),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.BrokenId), broken.Id),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.BadJson), badJson.Id, ("schema.ownedObject", "true")),
            Property<DiagnosticEntity>(nameof(DiagnosticEntity.Unsupported), new TypeId(typeof(Version).FullName!)),
            Property<DiagnosticEntity>("duplicate-1", text.Id, ("dotnet.memberName", nameof(DiagnosticEntity.Duplicate))),
            Property<DiagnosticEntity>("duplicate-2", text.Id, ("dotnet.memberName", nameof(DiagnosticEntity.Duplicate))),
        ], nameof(DiagnosticEntity.Id));
        ObjectTypeDefinition missingKey = Object<MissingKeyEntity>(EntityRole.Entity, "MissingKey", []);
        ObjectTypeDefinition collisionOne = Object<CollisionOne>(EntityRole.Entity, "Collision", [Property<CollisionOne>(nameof(CollisionOne.Id), guid.Id)], nameof(CollisionOne.Id));
        ObjectTypeDefinition collisionTwo = Object<CollisionTwo>(EntityRole.Entity, "Collision", [Property<CollisionTwo>(nameof(CollisionTwo.Id), guid.Id)], nameof(CollisionTwo.Id));
        ObjectTypeDefinition action = Object(typeof(Action), EntityRole.Unspecified);
        ObjectTypeDefinition version = Object(typeof(Version), EntityRole.Unspecified);
        TypeDefinition[] types = [guid, text, broken, target, valueKind, badJson, entityArray, dictionary, diagnostic, missingKey, collisionOne, collisionTwo, action, version];
        return new TypeSchemaModel { Id = new SchemaModelId("diagnostics"), Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = new AnnotationBag() };
    }

    private static ScalarTypeDefinition Scalar<T>(ScalarKind kind)
    {
        return new() { Id = new TypeId(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = kind, Annotations = Clr(typeof(T)) };
    }

    private static ObjectTypeDefinition Object<T>(EntityRole role, string name, IReadOnlyList<PropertyDefinition> properties, string? key = null)
    {
        return new()
        {
            Id = new TypeId(typeof(T).FullName!),
            Name = name,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Clr(typeof(T)),
            Semantics = new EntitySemantics { Role = role, IsValueObject = role == EntityRole.ValueObject },
            Properties = properties,
            Keys = key is null ? [] : [new KeyDefinition { Name = $"PK_{name}", Kind = KeyKind.Primary, Properties = [new PropertyRef(new PropertyId(key))], Annotations = new AnnotationBag() }],
            Relationships = [],
        };
    }

    private static ObjectTypeDefinition Object(Type type, EntityRole role)
    {
        return new() { Id = new TypeId(type.FullName!), Name = type.Name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Clr(type), Semantics = new EntitySemantics { Role = role }, Properties = [], Keys = [], Relationships = [] };
    }

    private static ArrayTypeDefinition Array<T>(TypeId item)
    {
        return new() { Id = new TypeId(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Array, Nullability = Nullability.NonNullable, ItemType = new TypeRef(item), Annotations = Clr(typeof(T)) };
    }

    private static DictionaryTypeDefinition Dictionary<T>(TypeId key, TypeId value)
    {
        return new() { Id = new TypeId(typeof(T).FullName!), Name = typeof(T).Name, Kind = TypeKind.Dictionary, Nullability = Nullability.NonNullable, KeyType = new TypeRef(key), ValueType = new TypeRef(value), Annotations = Clr(typeof(T)) };
    }

    private static PropertyDefinition Property<T>(string name, TypeId type, params (string Key, string Value)[] annotations)
    {
        return new()
        {
            Id = new PropertyId(name),
            Name = name,
            Type = new TypeRef(type),
            Cardinality = new Cardinality { IsRequired = true },
            Mutability = Mutability.InitOnly,
            Constraints = new ConstraintSet(),
            Annotations = new AnnotationBag { Items = [new Annotation { Key = new AnnotationKey("dotnet.memberName"), Value = name, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }, .. annotations.Select(annotation => new Annotation { Key = new AnnotationKey(annotation.Key), Value = annotation.Value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared })] },
        };
    }

    private static AnnotationBag Clr(Type type)
    {
        return new() { Items = [new Annotation { Key = new AnnotationKey("dotnet.clrType"), Value = type.AssemblyQualifiedName, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }] };
    }

    private readonly record struct BrokenStrongId(Guid Other);
    private sealed class TargetEntity { public Guid Id { get; init; } }
    private sealed class DiagnosticValueKind { public string Name { get; init; } = string.Empty; }
    private sealed class BadJsonValue { public Action Callback { get; init; } = static () => { }; public TargetEntity Entity { get; init; } = new(); }
    private sealed class DiagnosticEntity
    {
        public Guid Id { get; init; }
        public TargetEntity OwnedEntity { get; init; } = new();
        public DiagnosticValueKind UndeclaredValue { get; init; } = new();
        public TargetEntity EntityReference { get; init; } = new();
        public IReadOnlyList<TargetEntity> EntityCollection { get; init; } = [];
        public IReadOnlyDictionary<string, string> Dictionary { get; init; } = new Dictionary<string, string>();
        public BrokenStrongId BrokenId { get; init; }
        public BadJsonValue BadJson { get; init; } = new();
        public Version Unsupported { get; init; } = new();
        public string Duplicate { get; init; } = string.Empty;
    }
    private sealed class MissingKeyEntity;
    private sealed class CollisionOne { public Guid Id { get; init; } }
    private sealed class CollisionTwo { public Guid Id { get; init; } }
}
