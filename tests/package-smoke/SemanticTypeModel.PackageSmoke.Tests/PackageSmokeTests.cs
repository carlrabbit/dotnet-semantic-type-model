using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Model = SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.PowerBI;
using SemanticTypeModel.SystemTextJson;

namespace SemanticTypeModel.PackageSmoke.Tests;

[SemanticType(Name = "SmokeCustomer")]
internal sealed partial class SmokeCustomer
{
    public string Id { get; set; } = string.Empty;
}

internal sealed class PackageSmokeTests
{
    [Test]
    public async Task PackageSmokeShouldCoverPublicPackageApis()
    {
        Model.TypeSchemaModel canonicalModel = BuildCanonicalModel();
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(canonicalModel.DeriveJsonSchemaModel().Model);
        _ = await Assert.That(exported.Document.RootElement.GetRawText()).Contains("string");


        Model.SchemaProjectionContext powerBiContext = new() { Target = Model.ProjectionTarget.PowerBi };
        PowerBiProjectionModel powerBiProjection = new PowerBiModelProjection().Project(canonicalModel, powerBiContext);
        _ = await Assert.That(powerBiProjection).IsNotNull();

        Model.SchemaProjectionContext efCoreContext = new() { Target = Model.ProjectionTarget.EfCore };
        EfModelDefinition efCoreProjection = new EfCoreModelProjection().Project(canonicalModel, efCoreContext);
        _ = await Assert.That(efCoreProjection).IsNotNull();
        var modelBuilder = new ModelBuilder(new ConventionSet());
        EfCoreModelBuilderProjectionResult efCoreApplyResult = modelBuilder.ApplySemanticTypeModel(canonicalModel, options => options.ProjectUnannotatedObjectsAsEntities = true);
        _ = await Assert.That(efCoreApplyResult.Model).IsNotNull();

        using ServiceProvider provider = new ServiceCollection()
            .AddSemanticTypeModel(canonicalModel)
            .AddSemanticTypeModelJsonSchema()
            .BuildServiceProvider();

        ITypeSchemaModelService modelService = provider.GetRequiredService<ITypeSchemaModelService>();
        TypeSchemaModelResult modelResult = await modelService.GetModelAsync();
        _ = await Assert.That(modelResult.Model).IsNotNull();

        _ = typeof(SemanticTypeAttribute);
        _ = typeof(SemanticDisplayNameAttribute);
        _ = typeof(SemanticFormatAttribute);
        _ = typeof(SemanticStringConstraintsAttribute);
        _ = typeof(SemanticAnnotationAttribute);
        JsonSerializerOptions jsonOptions = new()
        {
            TypeInfoResolver = PackageSmokeJsonContext.Default.WithSemanticTypeModelJson(
                BuildPackageSmokeJsonModel(),
                projectionOptions => projectionOptions.PropertyNameSource = SemanticJsonPropertyNameSource.SemanticPropertyName),
        };
        string smokeJson = JsonSerializer.Serialize(new SmokeJsonCustomer { Id = "C-001" }, jsonOptions);
        SmokeJsonCustomer? smokeCustomer = JsonSerializer.Deserialize<SmokeJsonCustomer>("""
            { "smokeId": "C-002" }
            """, jsonOptions);

        _ = SystemTextJsonAnnotationNames.PropertyName;
        _ = await Assert.That(smokeJson).Contains("smokeId");
        _ = await Assert.That(smokeCustomer?.Id).IsEqualTo("C-002");
        _ = await Assert.That(nameof(SmokeCustomer)).IsEqualTo("SmokeCustomer");
    }

    private static Model.TypeSchemaModel BuildPackageSmokeJsonModel()
    {
        Model.ScalarTypeDefinition scalar = new()
        {
            Id = new Model.TypeId("global::System.String"),
            Name = "String",
            Kind = Model.TypeKind.Scalar,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            ScalarKind = Model.ScalarKind.String,
        };
        var customer = new Model.ObjectTypeDefinition
        {
            Id = new Model.TypeId("global::SemanticTypeModel.PackageSmoke.Tests.SmokeJsonCustomer"),
            Name = "SmokeJsonCustomer",
            Kind = Model.TypeKind.Object,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            Properties =
            [
                new Model.PropertyDefinition
                {
                    Id = new Model.PropertyId("Id"),
                    Name = "smokeId",
                    Type = new Model.TypeRef(scalar.Id),
                    Cardinality = new Model.Cardinality { IsRequired = true },
                    Mutability = Model.Mutability.Mutable,
                    Constraints = new Model.ConstraintSet(),
                    Annotations = new Model.AnnotationBag
                    {
                        Items =
                        [
                            new Model.Annotation { Key = new Model.AnnotationKey("dotnet.memberName"), Value = "Id", Scope = Model.AnnotationScope.Member, Source = Model.AnnotationSource.Imported },
                            new Model.Annotation { Key = new Model.AnnotationKey(SystemTextJsonAnnotationNames.PropertyName), Value = "smoke_id", Scope = Model.AnnotationScope.Member, Source = Model.AnnotationSource.Imported },
                        ],
                    },
                },
            ],
            Keys = [],
            Relationships = [],
        };
        return new Model.TypeSchemaModel { Id = new Model.SchemaModelId(customer.Id.Value), Types = [customer, scalar], TypesById = new Dictionary<Model.TypeId, Model.TypeDefinition> { [customer.Id] = customer, [scalar.Id] = scalar }, Annotations = new Model.AnnotationBag() };
    }

    private static Model.TypeSchemaModel BuildCanonicalModel()
    {
        Model.ScalarTypeDefinition scalar = new()
        {
            Id = new Model.TypeId("String"),
            Name = "String",
            Kind = Model.TypeKind.Scalar,
            Nullability = Model.Nullability.NonNullable,
            Annotations = new Model.AnnotationBag(),
            ScalarKind = Model.ScalarKind.String,
        };

        System.Collections.Generic.Dictionary<Model.TypeId, Model.TypeDefinition> typesById = new()
        {
            [scalar.Id] = scalar,
        };

        return new Model.TypeSchemaModel
        {
            Id = new Model.SchemaModelId("String"),
            Types = [scalar],
            TypesById = typesById,
            Annotations = new Model.AnnotationBag(),
        };
    }
}

internal sealed class SmokeJsonCustomer
{
    [JsonPropertyName("smoke_id")]
    public required string Id { get; init; }
}

[JsonSerializable(typeof(SmokeJsonCustomer))]
internal sealed partial class PackageSmokeJsonContext : JsonSerializerContext
{
}
