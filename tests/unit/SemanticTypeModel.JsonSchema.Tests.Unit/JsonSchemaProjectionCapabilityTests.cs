using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Runtime;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

// CS1591 is disabled in this test fixture to keep focus on projection capability assertions.
#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class JsonSchemaProjectionCapabilityTests
{
    [Test]
    public async Task Runtime_projection_should_expose_capability_metadata()
    {
        ProjectionCompatibilityContract capabilities = new JsonSchemaRuntimeProjection().GetCapabilities();

        _ = await Assert.That(capabilities.Projection).IsEqualTo(ProjectionTarget.JsonSchema);
        _ = await Assert.That(capabilities.GetSupport(SemanticModelFeature.UiHints).SupportLevel).IsEqualTo(ProjectionFeatureSupportLevel.SupportedWithOptions);
    }

    [Test]
    public async Task Catalog_should_include_json_editor_projection_contract()
    {
        ProjectionCompatibilityContract capabilities = ProjectionCapabilityCatalog.ForTarget(ProjectionTarget.JsonEditor);

        _ = await Assert.That(capabilities.GetSupport(SemanticModelFeature.UiHints).SupportLevel).IsEqualTo(ProjectionFeatureSupportLevel.Supported);
    }
}
#pragma warning restore CS1591
