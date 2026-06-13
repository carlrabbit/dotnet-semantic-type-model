using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema;
using RuntimeTypeSchemaModelBuilder = SemanticTypeModel.Abstractions.Canonical.TypeSchemaModelBuilder;

namespace SemanticTypeModel.DependencyInjection.Tests.Unit;

// Test fixtures intentionally omit XML documentation on individual test members for readability.
#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class TypeSchemaModelDependencyInjectionTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Register_existing_model_should_resolve_provider_and_return_model()
    {
        TypeSchemaModel model = BuildModel(Scalar("Customer", "Customer"));

        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSemanticTypeModel(model)
            .BuildServiceProvider();

        ITypeSchemaModelProvider provider = serviceProvider.GetRequiredService<ITypeSchemaModelProvider>();
        ITypeSchemaModelService service = serviceProvider.GetRequiredService<ITypeSchemaModelService>();

        TypeSchemaModelResult providerResult = await provider.GetModelAsync();
        TypeSchemaModelResult serviceResult = await service.GetModelAsync();

        _ = await Assert.That(providerResult.Model).IsNotNull();
        _ = await Assert.That(serviceResult.Model).IsSameReferenceAs(model);
        _ = await Assert.That(serviceResult.Diagnostics.Count).IsEqualTo(0);
        _ = await Assert.That(serviceProvider.GetRequiredService<ITypeSchemaModelService>()).IsSameReferenceAs(service);
    }

    [Test]
    public async Task Register_provider_type_should_support_async_resolution_and_cancellation()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSemanticTypeModelProvider<AsyncTestModelProvider>()
            .BuildServiceProvider();

        ITypeSchemaModelProvider provider = serviceProvider.GetRequiredService<ITypeSchemaModelProvider>();
        TypeSchemaModelResult result = await provider.GetModelAsync();

        _ = await Assert.That(result.Model).IsNotNull();
        _ = await Assert.That(result.Model!.Id.Value).IsEqualTo("AsyncModel");

        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        _ = await Assert.ThrowsAsync<OperationCanceledException>(async () => await provider.GetModelAsync(cancellationSource.Token));
    }

    [Test]
    public async Task Transformations_should_run_in_registration_order_and_accumulate_diagnostics()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSemanticTypeModel(BuildModel(Scalar("Customer", "Customer")))
            .AddSemanticTypeModelTransformation<AppendSuffixTransformationA>()
            .AddSemanticTypeModelTransformation<AppendSuffixTransformationB>()
            .BuildServiceProvider();

        TypeSchemaModelResult result = await serviceProvider.GetRequiredService<ITypeSchemaModelService>().GetModelAsync();
        var scalar = (ScalarTypeDefinition)result.Model!.GetType(new TypeId("Customer"));

        _ = await Assert.That(scalar.Name).IsEqualTo("Customer_A_B");
        _ = await Assert.That(result.Diagnostics.Select(static diagnostic => diagnostic.Code).ToArray()).IsEquivalentTo(["STM3901", "STM3902"]);
    }

    [Test]
    public async Task Validation_failure_should_block_projection_and_preserve_diagnostics()
    {
        ObjectTypeDefinition invalid = new()
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                Property("email", "P1", new TypeId("String")),
                Property("email", "P2", new TypeId("String")),
            ],
            Keys = [],
            Relationships = [],
        };

        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSemanticTypeModel(BuildModel(invalid, Scalar("String", "String")))
            .AddSemanticTypeModelTransformation<ValidateModelTransformation>()
            .AddSemanticTypeModelProjection<string, BlockingProjection>(ProjectionTarget.DotNet)
            .BuildServiceProvider();

        SchemaProjectionResult<string> result = await serviceProvider.GetRequiredService<ITypeSchemaProjectionService<string>>().ProjectAsync();

        _ = await Assert.That(result.HasProjection).IsFalse();
        _ = await Assert.That(result.IsProjectionBlocked).IsTrue();
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM0003")).IsTrue();
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM3008")).IsTrue();
        _ = await Assert.That(BlockingProjection.InvocationCount).IsEqualTo(0);
    }

    [Test]
    public async Task Json_schema_projection_should_compose_with_runtime_model_service()
    {
        var invocationCount = 0;
        TypeSchemaModel CreateCustomerModel()
        {
            invocationCount++;
            var customer = new ObjectTypeDefinition
            {
                Id = new TypeId("Customer"),
                Name = "Customer",
                Kind = TypeKind.Object,
                Nullability = Nullability.NonNullable,
                Annotations = EmptyAnnotations,
                Properties = [Property("id", "CustomerIdProperty", new TypeId("CustomerId"))],
                Keys = [],
                Relationships = [],
            };
            return BuildModel(customer, Scalar("CustomerId", "CustomerId"));
        }

        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSemanticTypeModel(CreateCustomerModel)
            .AddSemanticTypeModelTransformation<ValidateModelTransformation>()
            .AddSemanticTypeModelJsonSchema()
            .BuildServiceProvider();

        SchemaProjectionResult<JsonSchemaExportResult> result = await serviceProvider.GetRequiredService<ITypeSchemaProjectionService<JsonSchemaExportResult>>().ProjectAsync();

        _ = await Assert.That(result.HasProjection).IsTrue();
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(invocationCount).IsEqualTo(1);
        var json = result.Projection!.Document.RootElement.GetRawText();
        _ = await Assert.That(json.Contains("\"properties\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"id\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"string\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Missing_registration_should_return_deterministic_diagnostics()
    {
        using ServiceProvider modelOnlyProvider = new ServiceCollection()
            .AddSemanticTypeModelRuntime()
            .BuildServiceProvider();

        TypeSchemaModelResult modelResult = await modelOnlyProvider.GetRequiredService<ITypeSchemaModelService>().GetModelAsync();
        _ = await Assert.That(modelResult.Diagnostics.Single().Code).IsEqualTo("STM3001");

        using ServiceProvider projectionOnlyProvider = new ServiceCollection()
            .AddSemanticTypeModel(BuildModel(Scalar("Customer", "Customer")))
            .BuildServiceProvider();

        SchemaProjectionResult<string> projectionResult = await projectionOnlyProvider.GetRequiredService<ITypeSchemaProjectionService<string>>().ProjectAsync();
        _ = await Assert.That(projectionResult.Diagnostics.Single().Code).IsEqualTo("STM3006");
    }

    [Test]
    public async Task Caching_behavior_should_follow_runtime_options()
    {
        var invocationCount = 0;

        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSemanticTypeModelRuntime(new TypeSchemaRuntimeOptions { CacheModelResult = false })
            .AddSemanticTypeModel(() =>
            {
                invocationCount++;
                return new TypeSchemaModelResult { Model = BuildModel(Scalar("Customer", "Customer")) };
            })
            .BuildServiceProvider();

        ITypeSchemaModelService service = serviceProvider.GetRequiredService<ITypeSchemaModelService>();
        _ = await service.GetModelAsync();
        _ = await service.GetModelAsync();

        _ = await Assert.That(invocationCount).IsEqualTo(2);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        Dictionary<TypeId, TypeDefinition> byId = [];
        foreach (TypeDefinition type in types)
        {
            if (!byId.ContainsKey(type.Id))
            {
                byId[type.Id] = type;
            }
        }

        return new TypeSchemaModel
        {
            Id = new SchemaModelId(types.FirstOrDefault()?.Id.Value ?? "Model"),
            Types = types,
            TypesById = byId,
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string id, string name)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(id),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = ScalarKind.String,
        };
    }

    private static PropertyDefinition Property(string name, string propertyId, TypeId typeId)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(propertyId),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality { IsRequired = true },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = EmptyAnnotations,
        };
    }

    private sealed class AsyncTestModelProvider : ITypeSchemaModelProvider
    {
        public async ValueTask<TypeSchemaModelResult> GetModelAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new TypeSchemaModelResult { Model = BuildModel(Scalar("AsyncModel", "AsyncModel")) };
        }
    }

    private sealed class AppendSuffixTransformationA : ISchemaTransformation
    {
        public ValueTask TransformAsync(RuntimeTypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
        {
            AppendSuffix(model, "_A");
            context.Diagnostics.Report(new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "STM3901",
                Message = "First transformation executed.",
                Stage = SchemaDiagnosticStage.Transformation,
                PipelineStage = context.PipelineStage,
            });

            return ValueTask.CompletedTask;
        }
    }

    private sealed class AppendSuffixTransformationB : ISchemaTransformation
    {
        public ValueTask TransformAsync(RuntimeTypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
        {
            AppendSuffix(model, "_B");
            context.Diagnostics.Report(new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "STM3902",
                Message = "Second transformation executed.",
                Stage = SchemaDiagnosticStage.Transformation,
                PipelineStage = context.PipelineStage,
            });

            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingProjection : ISchemaProjection<string>
    {
        public static int InvocationCount { get; private set; }

        public string Project(TypeSchemaModel model, SchemaProjectionContext context)
        {
            InvocationCount++;
            return model.Id.Value;
        }
    }

    private static void AppendSuffix(RuntimeTypeSchemaModelBuilder builder, string suffix)
    {
        List<TypeDefinition> transformedTypes =
        [
            .. builder.Model.Types.Select(type => type switch
            {
                ScalarTypeDefinition scalar => scalar with { Name = scalar.Name + suffix },
                _ => type,
            }),
        ];

        builder.Replace(new TypeSchemaModel
        {
            Id = builder.Model.Id,
            Types = transformedTypes,
            TypesById = transformedTypes.ToDictionary(static type => type.Id, static type => type),
            Annotations = builder.Model.Annotations,
        });
    }
}
// Restore XML documentation warnings for files outside this test fixture.
#pragma warning restore CS1591
