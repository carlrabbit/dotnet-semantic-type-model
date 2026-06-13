using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.SystemTextJson;

namespace SemanticTypeModel.Samples.SystemTextJsonResolver;

internal static class Program
{
    public static void Main()
    {
        TypeSchemaModel model = CreateCanonicalModel();
        SystemTextJsonSemanticModel stjModel = model.DeriveSystemTextJsonModel(
            semanticOptions => semanticOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName).Model;

        JsonSerializerOptions options = new()
        {
            // AppJsonContext is user-authored; SemanticTypeModel customizes its resolver from the derived domain model.
            TypeInfoResolver = AppJsonContext.Default.WithSemanticTypeModelJson(stjModel),
        };

        Customer customer = new() { Id = "C-001", DisplayName = "Ada" };
        string json = JsonSerializer.Serialize(customer, options);
        Customer? roundTripped = JsonSerializer.Deserialize<Customer>(json, options);

        Console.WriteLine(json);
        Console.WriteLine(roundTripped?.DisplayName);
        Console.WriteLine(SystemTextJsonAnnotationNames.PropertyName);
    }

    private static TypeSchemaModel CreateCanonicalModel()
    {
        // In consumer code this canonical semantic model normally comes from code-first extraction or a generated provider.
        ScalarTypeDefinition stringType = new()
        {
            Id = new TypeId("global::System.String"),
            Name = "String",
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            ScalarKind = ScalarKind.String,
        };

        var customer = new ObjectTypeDefinition
        {
            Id = new TypeId("global::SemanticTypeModel.Samples.SystemTextJsonResolver.Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag(),
            Properties =
            [
                CreateProperty("customer_id", "Id", "customer_id"),
                CreateProperty("display_name", "DisplayName", "display_name"),
            ],
            Keys = [],
            Relationships = [],
        };

        return new TypeSchemaModel
        {
            Id = new SchemaModelId(customer.Id.Value),
            Types = [customer, stringType],
            TypesById = new Dictionary<TypeId, TypeDefinition>
            {
                [customer.Id] = customer,
                [stringType.Id] = stringType,
            },
            Annotations = new AnnotationBag(),
        };
    }

    private static PropertyDefinition CreateProperty(string semanticName, string memberName, string jsonName)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(memberName),
            Name = semanticName,
            Type = new TypeRef(new TypeId("global::System.String")),
            Cardinality = new Cardinality { IsRequired = true },
            Mutability = Mutability.InitOnly,
            Constraints = new ConstraintSet(),
            Annotations = new AnnotationBag
            {
                Items =
                [
                    Annotation("dotnet.memberName", memberName),
                    Annotation(SystemTextJsonAnnotationNames.PropertyName, jsonName),
                ],
            },
        };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = AnnotationScope.Member,
            Source = AnnotationSource.Imported,
        };
    }
}

// Consumers own System.Text.Json source-generation contexts.
[JsonSerializable(typeof(Customer))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}

[SemanticType]
internal sealed class Customer
{
    [SemanticName("customer_id")]
    public required string Id { get; init; }

    [SemanticName("display_name")]
    public required string DisplayName { get; init; }
}
