using System.Diagnostics;

namespace SemanticTypeModel.Engineering.Tests.Unit;

internal sealed class RepositoryPolicyTests
{
    [Test]
    public async Task AffectedPathsAreClassifiedDeterministically()
    {
        IReadOnlyList<string> commands = RepositoryPolicy.CommandsForAffectedPaths([
            "public-docs/usage.md",
            "tests\\unit\\SemanticTypeModel.JsonSchema.Tests.Unit\\Projection.cs",
            "eng/public-docs.sh",
        ]);
        _ = await Assert.That(commands).IsEquivalentTo([
            "public-docs",
            "test:tests/unit/SemanticTypeModel.Engineering.Tests.Unit/SemanticTypeModel.Engineering.Tests.Unit.csproj",
            "test:tests/unit/SemanticTypeModel.JsonSchema.Tests.Unit/SemanticTypeModel.JsonSchema.Tests.Unit.csproj",
        ]);
    }

    [Test]
    public async Task UnknownPathsFallBackToTierTwo()
    {
        _ = await Assert.That(RepositoryPolicy.CommandsForAffectedPaths(["src/Unknown/File.cs"])).IsEquivalentTo(["check"]);
    }

    [Test]
    public async Task PackageInventoryContainsOnlyRealPackages()
    {
        foreach (PackageProject package in RepositoryPolicy.Packages)
        {
            _ = await Assert.That(File.Exists(Path.Combine(FindRoot(), package.ProjectPath))).IsTrue();
        }
    }

    [Test]
    public async Task PublicDocumentationPolicyAcceptsRepository()
    {
        _ = await Assert.That(PublicDocumentationValidator.Validate(FindRoot())).IsEmpty();
    }

    [Test]
    public async Task PackageArtifactPolicyReportsMissingAndUnexpectedInventory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"stm-policy-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Unexpected.1.2.3.nupkg"), "");
            IReadOnlyList<string> errors = PackageSmokeRunner.ValidateArtifacts(directory, "1.2.3");
            _ = await Assert.That(errors.Any(error => error.Contains("Expected 11 publishable packages", StringComparison.Ordinal))).IsTrue();
            _ = await Assert.That(errors.Any(error => error.Contains("SemanticTypeModel.Abstractions", StringComparison.Ordinal))).IsTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task PackageSmokeLauncherForwardsInvalidUsageExitCode()
    {
        var root = FindRoot();
        var script = Path.Combine(root, "eng", OperatingSystem.IsWindows() ? "package-smoke.ps1" : "package-smoke.sh");
        ProcessStartInfo startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("pwsh") { UseShellExecute = false }
            : new ProcessStartInfo(script) { UseShellExecute = false };
        if (OperatingSystem.IsWindows())
        {
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(script);
        }

        using Process process = Process.Start(startInfo)!;
        await process.WaitForExitAsync();
        _ = await Assert.That(process.ExitCode).IsEqualTo(1);
    }

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SemanticTypeModel.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
