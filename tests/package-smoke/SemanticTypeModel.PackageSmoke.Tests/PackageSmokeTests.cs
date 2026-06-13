using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Canonical = SemanticTypeModel.Abstractions.Canonical;
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
        Canonical.TypeSchemaModel canonicalModel = BuildCanonicalModel();
        JsonSchemaExportResult exported = JsonSchemaExporter.Export(canonicalModel.DeriveJsonSchemaModel().Model);
        _ = await Assert.That(exported.Document.RootElement.GetRawText()).Contains("string");


        Canonical.SchemaProjectionContext powerBiContext = new() { Target = Canonical.ProjectionTarget.PowerBi };
        PowerBiProjectionModel powerBiProjection = new PowerBiModelProjection().Project(canonicalModel, powerBiContext);
        _ = await Assert.That(powerBiProjection).IsNotNull();

        Canonical.SchemaProjectionContext efCoreContext = new() { Target = Canonical.ProjectionTarget.EfCore };
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

    private static Canonical.TypeSchemaModel BuildPackageSmokeJsonModel()
    {
        Canonical.ScalarTypeDefinition scalar = new()
        {
            Id = new Canonical.TypeId("global::System.String"),
            Name = "String",
            Kind = Canonical.TypeKind.Scalar,
            Nullability = Canonical.Nullability.NonNullable,
            Annotations = new Canonical.AnnotationBag(),
            ScalarKind = Canonical.ScalarKind.String,
        };
        var customer = new Canonical.ObjectTypeDefinition
        {
            Id = new Canonical.TypeId("global::SemanticTypeModel.PackageSmoke.Tests.SmokeJsonCustomer"),
            Name = "SmokeJsonCustomer",
            Kind = Canonical.TypeKind.Object,
            Nullability = Canonical.Nullability.NonNullable,
            Annotations = new Canonical.AnnotationBag(),
            Properties =
            [
                new Canonical.PropertyDefinition
                {
                    Id = new Canonical.PropertyId("Id"),
                    Name = "smokeId",
                    Type = new Canonical.TypeRef(scalar.Id),
                    Cardinality = new Canonical.Cardinality { IsRequired = true },
                    Mutability = Canonical.Mutability.Mutable,
                    Constraints = new Canonical.ConstraintSet(),
                    Annotations = new Canonical.AnnotationBag
                    {
                        Items =
                        [
                            new Canonical.Annotation { Key = new Canonical.AnnotationKey("dotnet.memberName"), Value = "Id", Scope = Canonical.AnnotationScope.Member, Source = Canonical.AnnotationSource.Imported },
                            new Canonical.Annotation { Key = new Canonical.AnnotationKey(SystemTextJsonAnnotationNames.PropertyName), Value = "smoke_id", Scope = Canonical.AnnotationScope.Member, Source = Canonical.AnnotationSource.Imported },
                        ],
                    },
                },
            ],
            Keys = [],
            Relationships = [],
        };
        return new Canonical.TypeSchemaModel { Id = new Canonical.SchemaModelId(customer.Id.Value), Types = [customer, scalar], TypesById = new Dictionary<Canonical.TypeId, Canonical.TypeDefinition> { [customer.Id] = customer, [scalar.Id] = scalar }, Annotations = new Canonical.AnnotationBag() };
    }

    private static Canonical.TypeSchemaModel BuildCanonicalModel()
    {
        Canonical.ScalarTypeDefinition scalar = new()
        {
            Id = new Canonical.TypeId("String"),
            Name = "String",
            Kind = Canonical.TypeKind.Scalar,
            Nullability = Canonical.Nullability.NonNullable,
            Annotations = new Canonical.AnnotationBag(),
            ScalarKind = Canonical.ScalarKind.String,
        };

        System.Collections.Generic.Dictionary<Canonical.TypeId, Canonical.TypeDefinition> typesById = new()
        {
            [scalar.Id] = scalar,
        };

        return new Canonical.TypeSchemaModel
        {
            Id = new Canonical.SchemaModelId("String"),
            Types = [scalar],
            TypesById = typesById,
            Annotations = new Canonical.AnnotationBag(),
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
