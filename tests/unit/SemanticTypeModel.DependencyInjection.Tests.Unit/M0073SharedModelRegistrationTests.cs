using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Abstractions.Runtime;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;
using ModelBGenerated = SemanticTypeModel.TestModels.ModelB.Generated;

namespace SemanticTypeModel.DependencyInjection.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707
public sealed class M0073SharedModelRegistrationTests
{
    [Test]
    public async Task Public_registration_and_resolution_should_remain_model_local()
    {
        using ServiceProvider modelAProvider = new ServiceCollection()
            .AddSemanticTypeModel(ModelAGenerated.ModelASemanticTypeModel.Create())
            .BuildServiceProvider();
        using ServiceProvider modelBProvider = new ServiceCollection()
            .AddSemanticTypeModel(ModelBGenerated.ModelBSemanticTypeModel.Create())
            .BuildServiceProvider();

        TypeSchemaModelResult modelA = await modelAProvider.GetRequiredService<ITypeSchemaModelService>().GetModelAsync();
        TypeSchemaModelResult modelB = await modelBProvider.GetRequiredService<ITypeSchemaModelService>().GetModelAsync();

        _ = await Assert.That(modelA.Diagnostics).IsEmpty();
        _ = await Assert.That(modelB.Diagnostics).IsEmpty();
        TypeSchemaModel modelAModel = modelA.Model!;
        TypeSchemaModel modelBModel = modelB.Model!;
        _ = await Assert.That(modelAModel.Types.Any(type => type.Name == "ProjectionMatrixEntity")).IsTrue();
        _ = await Assert.That(modelBModel.Types.Any(type => type.Name == "BillingRecord")).IsTrue();
        _ = await Assert.That(modelAModel.Types.Any(type => type.Name == "BillingRecord")).IsFalse();
        _ = await Assert.That(modelBModel.Types.Any(type => type.Name == "ProjectionMatrixEntity")).IsFalse();
    }
}
#pragma warning restore CA1707
#pragma warning restore CS1591
