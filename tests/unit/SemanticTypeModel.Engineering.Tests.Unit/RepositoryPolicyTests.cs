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
    public async Task ManifestVersionDocumentationMustMatchBothGenerators()
    {
        const string producer = "Version = 4,";
        const string consumer = "internal const int CurrentVersion = 4;";
        const string spec = "The current compile-time manifest schema version is 4.";
        const string readme = "The current ephemeral manifest schema is v4.";

        _ = await Assert.That(PublicDocumentationValidator.ValidateManifestVersionDeclarations(producer, consumer, spec, readme)).IsEmpty();

        IReadOnlyList<string> staleReadme = PublicDocumentationValidator.ValidateManifestVersionDeclarations(
            producer, consumer, spec, "The current ephemeral manifest schema is v3.");
        _ = await Assert.That(staleReadme.Any(error => error.Contains("Shared NuGet README", StringComparison.Ordinal))).IsTrue();

        IReadOnlyList<string> staleSpec = PublicDocumentationValidator.ValidateManifestVersionDeclarations(
            producer, consumer, "The current compile-time manifest schema version is 3.", readme);
        _ = await Assert.That(staleSpec.Any(error => error.Contains("EF Core specification", StringComparison.Ordinal))).IsTrue();

        IReadOnlyList<string> mismatchedGenerators = PublicDocumentationValidator.ValidateManifestVersionDeclarations(
            producer, "internal const int CurrentVersion = 3;", spec, readme);
        _ = await Assert.That(mismatchedGenerators.Any(error => error.Contains("manifest consumer", StringComparison.Ordinal))).IsTrue();

        IReadOnlyList<string> missingDeclaration = PublicDocumentationValidator.ValidateManifestVersionDeclarations(
            producer, consumer, spec, "The manifest format is unspecified.");
        _ = await Assert.That(missingDeclaration.Any(error => error.Contains("Cannot determine", StringComparison.Ordinal))).IsTrue();
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
