using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.Core.Tests.Unit;

// CS1591 is disabled in this test fixture to keep focus on behavioral assertions.
#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class ProjectionCapabilityCatalogTests
{
    [Test]
    public async Task Catalog_should_define_contracts_for_supported_projection_targets()
    {
        ProjectionTarget[] expected =
        [
            ProjectionTarget.JsonSchema,
            ProjectionTarget.JsonEditor,
            ProjectionTarget.EfCore,
            ProjectionTarget.PowerBi,
        ];

        IReadOnlyList<ProjectionTarget> actual =
        [
            .. ProjectionCapabilityCatalog.GetAll().Select(static contract => contract.Projection),
        ];

        _ = await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Catalog_should_cover_every_core_feature_for_every_projection()
    {
        IReadOnlyList<SemanticModelFeature> coreFeatures = ProjectionCapabilityCatalog.GetCoreFeatures();

        foreach (ProjectionCompatibilityContract contract in ProjectionCapabilityCatalog.GetAll())
        {
            IReadOnlyList<SemanticModelFeature> actualFeatures =
            [
                .. contract.Features.Select(static capability => capability.Feature),
            ];

            _ = await Assert.That(actualFeatures).IsEquivalentTo(coreFeatures);
        }
    }

    [Test]
    public async Task Catalog_should_return_deterministic_contract_payload()
    {
        IReadOnlyList<ProjectionCompatibilityContract> first = ProjectionCapabilityCatalog.GetAll();
        IReadOnlyList<ProjectionCompatibilityContract> second = ProjectionCapabilityCatalog.GetAll();

        var left = JsonSerializer.Serialize(first);
        var right = JsonSerializer.Serialize(second);

        _ = await Assert.That(left).IsEqualTo(right);
    }
}
#pragma warning restore CS1591
