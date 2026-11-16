using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore;
using Intake = SemanticTypeModel.TestModels.ModelA.Intake;
using ModelA = SemanticTypeModel.TestModels.ModelA;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;

namespace SemanticTypeModel.EFCore.Tests.Unit;

public sealed class RelationalDerivationTests
{
    [Test]
    public async Task Relational_derivation_preserves_TPT_and_inherited_member_placement()
    {
        EfRelationalModel model = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        EfEntity root = model.Entities.Single(entity => entity.ClrType == typeof(Intake.Specification));
        EfEntity derived = model.Entities.Single(entity => entity.ClrType == typeof(Intake.ImportSpecification));
        _ = await Assert.That(root.BaseEntityId).IsNull();
        _ = await Assert.That(derived.BaseEntityId).IsEqualTo(root.SemanticTypeId);
        _ = await Assert.That(derived.ScalarColumns.All(column => column.DeclaringClrType == typeof(Intake.ImportSpecification))).IsTrue();
    }

    [Test]
    public async Task Relational_derivation_preserves_JSON_and_binary_storage_rules()
    {
        EfRelationalModel json = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        EfRelationalModel binary = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        _ = await Assert.That(json.Entities.SelectMany(entity => entity.JsonColumns)).IsNotEmpty();
        _ = await Assert.That(binary.Entities.SelectMany(entity => entity.BinaryColumns)).IsNotEmpty();
    }

    [Test]
    public async Task Relational_derivation_treats_nested_string_dictionary_as_JSON_content_and_keeps_extension_data_distinct()
    {
        TypeSchemaModel source = ModelAGenerated.ModelASemanticTypeModel.Create();
        SemanticDerivationResult<EfRelationalModel> result = source.DeriveEfRelationalModel();
        EfEntity entity = result.Model.Entities.Single(value => value.ClrType == typeof(ModelA.InventoryItem));

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "EF_DICTIONARY_STORAGE_NOT_SUPPORTED")).IsFalse();
        _ = await Assert.That(entity.JsonColumns.Select(static column => column.MemberName)).Contains(nameof(ModelA.InventoryItem.Details));
        _ = await Assert.That(entity.JsonColumns.Select(static column => column.MemberName)).Contains(nameof(ModelA.InventoryItem.ExtensionData));
        _ = await Assert.That(entity.JsonColumns.Single(column => column.MemberName == nameof(ModelA.InventoryItem.Details)).JsonShape).IsEqualTo(EfJsonShape.Object);
        _ = await Assert.That(entity.JsonColumns.Single(column => column.MemberName == nameof(ModelA.InventoryItem.ExtensionData)).JsonShape).IsEqualTo(EfJsonShape.ExtensionData);

        DictionaryTypeDefinition dictionary = source.Types.OfType<DictionaryTypeDefinition>().Single(type =>
            source.Types.OfType<ObjectTypeDefinition>().Single(value => value.Name == nameof(ModelA.InventoryDetails)).Properties.Any(property => property.Type.Id == type.Id));
        TypeId integerId = source.Types.OfType<ScalarTypeDefinition>().First(static scalar => scalar.ScalarKind == ScalarKind.Integer).Id;
        TypeDefinition[] unsupportedTypes = [.. source.Types.Select(type => type.Id == dictionary.Id ? dictionary with { KeyType = new TypeRef(integerId) } : type)];
        TypeSchemaModel unsupported = new() { Id = source.Id, Types = unsupportedTypes, TypesById = unsupportedTypes.ToDictionary(static type => type.Id), Annotations = source.Annotations };
        SemanticDerivationResult<EfRelationalModel> rejected = unsupported.DeriveEfRelationalModel();

        SchemaDiagnostic[] dictionaryDiagnostics = [.. rejected.Diagnostics.Where(static diagnostic => diagnostic.Code == "EF_DICTIONARY_STORAGE_NOT_SUPPORTED")];
        _ = await Assert.That(dictionaryDiagnostics).IsNotEmpty();
        _ = await Assert.That(dictionaryDiagnostics.All(static diagnostic => diagnostic.ModelPath?.EndsWith("InventoryDetails.Labels", StringComparison.Ordinal) == true)).IsTrue();
    }

    [Test]
    public async Task Relational_derivation_rejects_direct_ordinary_entity_dictionary_storage()
    {
        TypeSchemaModel source = ModelAGenerated.ModelASemanticTypeModel.Create();
        ObjectTypeDefinition entity = source.Types.OfType<ObjectTypeDefinition>().Single(type => type.Name == nameof(ModelA.InventoryItem));
        PropertyDefinition extensionData = entity.Properties.Single(property => property.Name == nameof(ModelA.InventoryItem.ExtensionData));
        ObjectTypeDefinition changedEntity = entity with
        {
            Properties = [.. entity.Properties.Select(property => property.Id == extensionData.Id ? property with { Annotations = new AnnotationBag() } : property)],
        };
        TypeDefinition[] types = [.. source.Types.Select(type => type.Id == entity.Id ? changedEntity : type)];
        TypeSchemaModel model = new() { Id = source.Id, Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = source.Annotations };

        SemanticDerivationResult<EfRelationalModel> result = model.DeriveEfRelationalModel();

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "EF_DICTIONARY_STORAGE_NOT_SUPPORTED")).IsTrue();
    }

    [Test]
    public async Task Relational_projection_preserves_owned_JSON_property_use_nullability()
    {
        EfJsonColumn[] objects = [.. ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model.Entities.Where(entity => entity.ClrType == typeof(ModelA.InventoryItem)).SelectMany(entity => entity.JsonColumns)];
        EfJsonColumn[] collections = objects;

        _ = await Assert.That(objects.Single(column => column.MemberName == nameof(ModelA.InventoryItem.OptionalDetails)).IsNullable).IsTrue();
        _ = await Assert.That(objects.Single(column => column.MemberName == nameof(ModelA.InventoryItem.Details)).IsNullable).IsFalse();
        _ = await Assert.That(collections.Single(column => column.MemberName == nameof(ModelA.InventoryItem.DetailHistory)).IsNullable).IsFalse();
        _ = await Assert.That(collections.Single(column => column.MemberName == nameof(ModelA.InventoryItem.OptionalDetailHistory)).IsNullable).IsTrue();
    }

    [Test]
    public async Task Relational_projection_covers_supported_storage_nullability_matrix()
    {
        EfEntity entity = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model.Entities.Single(value => value.ClrType == typeof(ModelA.StorageMatrixEntity));

        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredText), nameof(ModelA.StorageMatrixEntity.OptionalText), typeof(string));
        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredState), nameof(ModelA.StorageMatrixEntity.OptionalState), typeof(string));
        await AssertColumnNullability(entity.ScalarColumns, nameof(ModelA.StorageMatrixEntity.RequiredUri), nameof(ModelA.StorageMatrixEntity.OptionalUri), typeof(string));
        await AssertColumnNullability(entity.BinaryColumns, nameof(ModelA.StorageMatrixEntity.RequiredBinary), nameof(ModelA.StorageMatrixEntity.OptionalBinary), typeof(byte[]));
        await AssertColumnNullability(entity.BinaryColumns, nameof(ModelA.StorageMatrixEntity.RequiredReadOnlyMemory), nameof(ModelA.StorageMatrixEntity.OptionalReadOnlyMemory), typeof(byte[]));
        await AssertJsonNullability(entity.JsonColumns, nameof(ModelA.StorageMatrixEntity.RequiredDetails), nameof(ModelA.StorageMatrixEntity.OptionalDetails), EfJsonShape.Object);
        await AssertJsonNullability(entity.JsonColumns, nameof(ModelA.StorageMatrixEntity.RequiredDetailsCollection), nameof(ModelA.StorageMatrixEntity.OptionalDetailsCollection), EfJsonShape.Array);
        EfJsonColumn extensionData = entity.JsonColumns.Single(column => column.MemberName == nameof(ModelA.StorageMatrixEntity.MatrixExtensionData));
        _ = await Assert.That(extensionData.IsNullable).IsTrue();
        _ = await Assert.That(extensionData.JsonShape).IsEqualTo(EfJsonShape.ExtensionData);
    }

    [Test]
    public async Task Relational_derivation_preserves_enum_string_provider_rule()
    {
        EfRelationalModel model = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model;
        EfScalarColumn[] enumColumns = [.. model.Entities.SelectMany(entity => entity.ScalarColumns).Where(column => (Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType).IsEnum)];
        _ = await Assert.That(enumColumns).IsNotEmpty();
        _ = await Assert.That(enumColumns.All(column => column.ProviderType == typeof(string))).IsTrue();
    }

    [Test]
    public async Task Relational_projection_covers_complete_scalar_matrix()
    {
        EfEntity entity = ModelAGenerated.ModelASemanticTypeModel.Create().DeriveEfRelationalModel().Model.Entities.Single(value => value.ClrType == typeof(ModelA.ProjectionMatrixEntity));
        EfScalarColumn[] columns = [.. entity.ScalarColumns.Concat(entity.BinaryColumns)];

        foreach (ModelA.ProjectionMatrixCase matrixCase in ModelA.ProjectionMatrix.Cases)
        {
            EfScalarColumn scalar = columns.Single(column => column.MemberName == matrixCase.PropertyName);
            EfScalarColumn optionalScalar = columns.Single(column => column.MemberName == matrixCase.OptionalPropertyName);
            _ = await Assert.That(scalar.ProviderType).IsEqualTo(ExpectedProviderType(matrixCase.ScalarKind));
            if (scalar.IsNullable)
            {
                throw new InvalidOperationException($"Required scalar column is nullable: {matrixCase.PropertyName}.");
            }
            _ = await Assert.That(optionalScalar.IsNullable).IsTrue();
        }
    }


    private static async Task AssertColumnNullability(
        IReadOnlyList<EfScalarColumn> columns,
        string requiredName,
        string optionalName,
        Type providerType)
    {
        EfScalarColumn required = columns.Single(column => column.MemberName == requiredName);
        EfScalarColumn optional = columns.Single(column => column.MemberName == optionalName);
        _ = await Assert.That(required.IsNullable).IsFalse();
        _ = await Assert.That(optional.IsNullable).IsTrue();
        _ = await Assert.That(required.ProviderType).IsEqualTo(providerType);
        _ = await Assert.That(optional.ProviderType).IsEqualTo(providerType);
    }

    private static async Task AssertJsonNullability(
        IReadOnlyList<EfJsonColumn> columns,
        string requiredName,
        string optionalName,
        EfJsonShape shape)
    {
        EfJsonColumn required = columns.Single(column => column.MemberName == requiredName);
        EfJsonColumn optional = columns.Single(column => column.MemberName == optionalName);
        _ = await Assert.That(required.IsNullable).IsFalse();
        _ = await Assert.That(optional.IsNullable).IsTrue();
        _ = await Assert.That(required.JsonShape).IsEqualTo(shape);
        _ = await Assert.That(optional.JsonShape).IsEqualTo(shape);
    }

    private static readonly Dictionary<ScalarKind, Type> ExpectedProviderTypes = new()
    {
        [ScalarKind.Boolean] = typeof(bool),
        [ScalarKind.String] = typeof(string),
        [ScalarKind.Integer] = typeof(long),
        [ScalarKind.Number] = typeof(double),
        [ScalarKind.Decimal] = typeof(decimal),
        [ScalarKind.Date] = typeof(DateOnly),
        [ScalarKind.Time] = typeof(TimeOnly),
        [ScalarKind.DateTime] = typeof(DateTime),
        [ScalarKind.DateTimeOffset] = typeof(DateTimeOffset),
        [ScalarKind.Duration] = typeof(TimeSpan),
        [ScalarKind.Guid] = typeof(Guid),
        [ScalarKind.Binary] = typeof(byte[]),
    };

    private static Type ExpectedProviderType(ScalarKind kind)
    {
        return ExpectedProviderTypes[kind];
    }
}
