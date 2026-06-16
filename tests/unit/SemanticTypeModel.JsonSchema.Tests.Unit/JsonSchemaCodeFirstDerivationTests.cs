using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Domain;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies code-first JSON Schema domain derivation and export.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaCodeFirstDerivationTests
{
    /// <summary>
    /// Derivation returns domain model, diagnostics, and trace.
    /// </summary>
    [Test]
    public async Task Derivation_should_return_domain_model_diagnostics_and_trace()
    {
        TypeSchemaModel model = CustomerModel(includeUnion: true);

        SemanticDerivationResult<JsonSchemaSemanticModel> result = model.DeriveJsonSchemaModel(options => options.UseDefaultTransformations());

        _ = await Assert.That(result.Model).IsTypeOf<JsonSchemaSemanticModel>();
        _ = await Assert.That(result.Diagnostics).IsEmpty();
        _ = await Assert.That(result.Trace.Entries.Select(static entry => entry.TransformationId)).Contains("json-schema.diagnose-unsupported-composition");
    }

    /// <summary>
    /// Export emits deterministic Draft 2020-12 baseline features from the domain model.
    /// </summary>
    [Test]
    public async Task Export_should_emit_deterministic_baseline_schema_from_domain_model()
    {
        TypeSchemaModel model = CustomerModel(includeUnion: true);

        SemanticDerivationResult<JsonSchemaSemanticModel> result = model.DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
        JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model);
        JsonElement root = export.Document.RootElement;

        _ = await Assert.That(root.GetProperty("$schema").GetString()).IsEqualTo(JsonSchemaDialectUris.Draft202012);
        _ = await Assert.That(root.GetProperty("type").GetString()).IsEqualTo("object");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("emailAddress").GetProperty("description").GetString()).IsEqualTo("Primary contact email address.");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("emailAddress").GetProperty("$ref").GetString()).IsEqualTo("#/$defs/Email");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("tags").GetProperty("$ref").GetString()).IsEqualTo("#/$defs/Tags");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("attributes").GetProperty("$ref").GetString()).IsEqualTo("#/$defs/Attributes");
        _ = await Assert.That(root.GetProperty("required").EnumerateArray().Select(static value => value.GetString()!).ToArray()).IsEquivalentTo(["emailAddress", "id"]);
        _ = await Assert.That(root.GetProperty("$defs").GetProperty("Email").GetProperty("format").GetString()).IsEqualTo("email");
        _ = await Assert.That(root.GetProperty("$defs").GetProperty("Status").GetProperty("enum").EnumerateArray().Select(static value => value.GetString()!).ToArray()).IsEquivalentTo(["Active", "Inactive"]);
        _ = await Assert.That(root.GetProperty("$defs").GetProperty("ContactMethod").GetProperty("anyOf").GetArrayLength()).IsEqualTo(2);
    }

    /// <summary>
    /// Derivation transformations are configurable and replaceable.
    /// </summary>
    [Test]
    public async Task Users_should_replace_json_schema_derivation_transformations()
    {
        TypeSchemaModel model = CustomerModel(includeUnion: true);

        SemanticDerivationResult<JsonSchemaSemanticModel> result = model.DeriveJsonSchemaModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Transformations.Replace("json-schema.diagnose-unsupported-composition", new ReplacementTransformation());
        });

        _ = await Assert.That(result.Trace.Entries.Select(static entry => entry.TransformationId)).Contains("test.replacement");
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_TEST_REPLACED")).IsTrue();
    }

    /// <summary>
    /// Unsupported composition shapes emit deterministic diagnostics.
    /// </summary>
    [Test]
    public async Task Unsupported_composition_should_emit_diagnostics()
    {
        TypeSchemaModel model = new()
        {
            Id = new SchemaModelId("BrokenUnion"),
            Types =
            [
                new UnionTypeDefinition
                {
                    Id = new TypeId("BrokenUnion"),
                    Name = "BrokenUnion",
                    Kind = TypeKind.Union,
                    Nullability = Nullability.NonNullable,
                    Annotations = EmptyAnnotations,
                    Options = [],
                    Semantics = UnionSemantics.OneOf,
                },
            ],
            TypesById = new Dictionary<TypeId, TypeDefinition>
            {
                [new TypeId("BrokenUnion")] = new UnionTypeDefinition
                {
                    Id = new TypeId("BrokenUnion"),
                    Name = "BrokenUnion",
                    Kind = TypeKind.Union,
                    Nullability = Nullability.NonNullable,
                    Annotations = EmptyAnnotations,
                    Options = [],
                    Semantics = UnionSemantics.OneOf,
                },
            },
            Annotations = EmptyAnnotations,
        };

        SemanticDerivationResult<JsonSchemaSemanticModel> result = model.DeriveJsonSchemaModel(options => options.UseDefaultTransformations());
        JsonSchemaExportResult export = JsonSchemaExporter.Export(result.Model);

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_DERIVE_EMPTY_ALTERNATIVES")).IsTrue();
        _ = await Assert.That(export.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_EXPORT_EMPTY_ALTERNATIVES")).IsTrue();
    }

    private static TypeSchemaModel CustomerModel(bool includeUnion = false)
    {
        TypeDefinition[] types =
        [
            new ObjectTypeDefinition
            {
                Id = new TypeId("Customer"),
                Name = "Customer",
                DisplayName = "Customer",
                Description = "A customer account.",
                Kind = TypeKind.Object,
                Nullability = Nullability.NonNullable,
                Annotations = EmptyAnnotations,
                Properties =
                [
                    Property("id", "Customer_Id", "String", required: true),
                    Property("emailAddress", "Customer_Email", "Email", required: true, description: "Primary contact email address."),
                    Property("status", "Customer_Status", "Status", required: false, nullable: true),
                    Property("tags", "Customer_Tags", "Tags", required: false),
                    Property("attributes", "Customer_Attributes", "Attributes", required: false),
                    Property("contact", "Customer_Contact", "ContactMethod", required: false),
                ],
                Keys = [],
                Relationships = [],
            },
            Scalar("String", ScalarKind.String),
            Scalar("Email", ScalarKind.String, "email"),
            new EnumTypeDefinition
            {
                Id = new TypeId("Status"),
                Name = "Status",
                Kind = TypeKind.Enum,
                Nullability = Nullability.NonNullable,
                Annotations = EmptyAnnotations,
                StorageKind = EnumStorageKind.String,
                Values =
                [
                    new EnumValueDefinition { Name = "Active", Value = "Active", Annotations = EmptyAnnotations },
                    new EnumValueDefinition { Name = "Inactive", Value = "Inactive", Annotations = EmptyAnnotations },
                ],
            },
            new ArrayTypeDefinition
            {
                Id = new TypeId("Tags"),
                Name = "Tags",
                Kind = TypeKind.Array,
                Nullability = Nullability.NonNullable,
                Annotations = EmptyAnnotations,
                ItemType = new TypeRef(new TypeId("String")),
                UniqueItems = true,
            },
            new DictionaryTypeDefinition
            {
                Id = new TypeId("Attributes"),
                Name = "Attributes",
                Kind = TypeKind.Dictionary,
                Nullability = Nullability.NonNullable,
                Annotations = EmptyAnnotations,
                KeyType = new TypeRef(new TypeId("String")),
                ValueType = new TypeRef(new TypeId("String")),
            },
            new UnionTypeDefinition
            {
                Id = new TypeId("ContactMethod"),
                Name = "ContactMethod",
                Kind = TypeKind.Union,
                Nullability = Nullability.NonNullable,
                Annotations = EmptyAnnotations,
                Options = includeUnion ? [new TypeRef(new TypeId("Email")), new TypeRef(new TypeId("String"))] : [],
                Semantics = UnionSemantics.AnyOf,
            },
        ];

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("Customer"),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id, static type => type),
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind, string? format = null)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(id),
            Name = id,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = kind,
            Format = format,
        };
    }

    private static PropertyDefinition Property(string name, string id, string typeId, bool required, bool nullable = false, string? description = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(id),
            Name = name,
            DisplayName = name,
            Description = description,
            Type = new TypeRef(new TypeId(typeId)),
            Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = EmptyAnnotations,
        };
    }

    private static AnnotationBag EmptyAnnotations { get; } = new();

    private sealed class ReplacementTransformation : ISemanticModelTransformation
    {
        public string Id => "test.replacement";

        public string DisplayName => nameof(ReplacementTransformation);

        public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
        {
            context.Diagnostics.Report(new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "JSONSCHEMA_TEST_REPLACED",
                Message = "Replacement ran.",
                Stage = SchemaDiagnosticStage.Transformation,
                PipelineStage = context.TransformationId,
                ModelPath = "/",
                ProjectionTarget = ProjectionTarget.JsonSchema,
            });

            return SemanticModelTransformationStepResult.Unchanged(model);
        }
    }
}
