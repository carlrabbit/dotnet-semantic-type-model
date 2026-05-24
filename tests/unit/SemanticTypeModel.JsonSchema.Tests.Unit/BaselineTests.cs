using System.Diagnostics.CodeAnalysis;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

/// <summary>
/// Verifies the JsonSchema unit test project can execute through the configured test runner.
/// </summary>
public sealed class BaselineTests
{
    /// <summary>
    /// Confirms the baseline test passes.
    /// </summary>
    [Test]
    [SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
    public async Task Baseline_should_pass()
    {
        var actual = true;

        _ = await Assert.That(actual).IsTrue();
    }
}
