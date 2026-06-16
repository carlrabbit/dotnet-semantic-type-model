using System.Text.Json;
using SemanticTypeModel.Abstractions.Contracts;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.JsonSchema.Import;

#pragma warning disable CS1591

/// <summary>Minimal JSON Schema importer retained for package compatibility; JSON Schema import is not the supported M0038 authoring path.</summary>
public sealed class JsonSchemaImporter : ISchemaModelSource
{
    private readonly string _json;

    public JsonSchemaImporter(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        _json = json;
    }

    public TypeSchemaModel Load()
    {
        return Import(_json).Model;
    }

    public static JsonSchemaImportResult Import(string json, JsonSchemaImportOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        return Import(document.RootElement.Clone(), options);
    }

    public static JsonSchemaImportResult Import(Stream stream, JsonSchemaImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var document = JsonDocument.Parse(stream);
        return Import(document.RootElement.Clone(), options);
    }

    public static JsonSchemaImportResult Import(JsonDocument document, JsonSchemaImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Import(document.RootElement.Clone(), options);
    }

    public static JsonSchemaImportResult Import(JsonElement root, JsonSchemaImportOptions? options = null)
    {
        options ??= JsonSchemaImportOptions.Default;
        var rootId = root.TryGetProperty("$id", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.String
            ? idElement.GetString() ?? "JsonSchemaRoot"
            : "JsonSchemaRoot";

        TypeDefinition rootType = BuildType(root, rootId, options);
        var byId = new Dictionary<TypeId, TypeDefinition> { [rootType.Id] = rootType };
        var model = new TypeSchemaModel
        {
            Id = new SchemaModelId(rootType.Id.Value),
            Types = [rootType],
            TypesById = byId,
            Annotations = new AnnotationBag(),
        };

        return new JsonSchemaImportResult(model, []);
    }

    private static TypeDefinition BuildType(JsonElement schema, string id, JsonSchemaImportOptions options)
    {
        var name = id.Split(['/', '#', '.'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? id;
        if (schema.TryGetProperty("enum", out JsonElement enumElement) && enumElement.ValueKind == JsonValueKind.Array)
        {
            return new EnumTypeDefinition
            {
                Id = new TypeId(id),
                Name = name,
                Kind = TypeKind.Enum,
                Nullability = Nullability.NonNullable,
                Annotations = new AnnotationBag(),
                StorageKind = EnumStorageKind.String,
                Values = [.. enumElement.EnumerateArray().Select((value, index) => new EnumValueDefinition
                {
                    Name = value.ValueKind == JsonValueKind.String ? value.GetString() ?? $"Value{index}" : $"Value{index}",
                    Value = value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.GetRawText(),
                    Annotations = new AnnotationBag(),
                })],
            };
        }

        var type = schema.TryGetProperty("type", out JsonElement typeElement) && typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() : null;
        return type == "object" ? BuildObject(schema, id, name, options) : BuildScalar(schema, id, name, type);
    }

    private static ObjectTypeDefinition BuildObject(JsonElement schema, string id, string name, JsonSchemaImportOptions options)
    {
        _ = options;
        HashSet<string> required = schema.TryGetProperty("required", out JsonElement requiredElement) && requiredElement.ValueKind == JsonValueKind.Array
            ? new HashSet<string>(requiredElement.EnumerateArray().Where(static item => item.ValueKind == JsonValueKind.String).Select(static item => item.GetString()!).Where(static item => item is not null), StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        var properties = new List<PropertyDefinition>();
        if (schema.TryGetProperty("properties", out JsonElement propertiesElement) && propertiesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in propertiesElement.EnumerateObject().OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                var propertyTypeId = $"{id}.{property.Name}";
                properties.Add(new PropertyDefinition
                {
                    Id = new PropertyId($"{id}.{property.Name}"),
                    Name = property.Name,
                    Type = new TypeRef(new TypeId(propertyTypeId)),
                    Cardinality = new Cardinality { IsRequired = required.Contains(property.Name) },
                    Mutability = Mutability.Mutable,
                    Constraints = new ConstraintSet(),
                    Annotations = new AnnotationBag(),
                });
            }
        }

        return new ObjectTypeDefinition
        {
            Id = new TypeId(id),
            Name = name,
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Properties = properties,
            Keys = [],
            Relationships = [],
        };
    }

    private static ScalarTypeDefinition BuildScalar(JsonElement schema, string id, string name, string? type)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(id),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = type == "null" ? Nullability.Nullable : Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            ScalarKind = type switch
            {
                "boolean" => ScalarKind.Boolean,
                "integer" => ScalarKind.Integer,
                "number" => ScalarKind.Number,
                _ => ScalarKind.String,
            },
            Format = schema.TryGetProperty("format", out JsonElement formatElement) && formatElement.ValueKind == JsonValueKind.String ? formatElement.GetString() : null,
        };
    }
}

#pragma warning restore CS1591
