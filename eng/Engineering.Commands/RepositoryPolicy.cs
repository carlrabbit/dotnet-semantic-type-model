namespace SemanticTypeModel.Engineering;

internal sealed record PackageProject(string Id, string ProjectPath);

internal static class RepositoryPolicy
{
    internal static IReadOnlyList<PackageProject> Packages { get; } =
    [
        Package("Abstractions"), Package("Core"), Package("JsonSchema"), Package("DotNet"),
        Package("Generators"), Package("DependencyInjection"), Package("PowerBI"), Package("EFCore"),
        Package("EFCore.Generators"), Package("SystemTextJson"),
    ];

    internal static IReadOnlyList<string> CommandsForAffectedPaths(IEnumerable<string> paths)
    {
        var commands = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var raw in paths)
        {
            var path = raw.Replace('\\', '/');
            if (path == "README.md" || path.StartsWith("docs/", StringComparison.Ordinal) || path.StartsWith("public-docs/", StringComparison.Ordinal))
            {
                _ = commands.Add("public-docs");
            }

            foreach (var area in TestAreas)
            {
                AddTest(path, area, commands);
            }

            if (path.StartsWith("eng/", StringComparison.Ordinal))
            {
                _ = commands.Add("test:tests/unit/SemanticTypeModel.Engineering.Tests.Unit/SemanticTypeModel.Engineering.Tests.Unit.csproj");
            }
            if (path.StartsWith("samples/", StringComparison.Ordinal))
            {
                _ = commands.Add("samples");
            }
        }
        return commands.Count == 0 ? ["check"] : [.. commands];
    }

    private static IReadOnlyList<string> TestAreas { get; } =
    [
        "Core", "DependencyInjection", "DotNet", "EFCore", "EFCore.Generators",
        "Generators", "JsonSchema", "PowerBI", "SystemTextJson",
    ];

    private static PackageProject Package(string suffix)
    {
        var id = $"SemanticTypeModel.{suffix}";
        return new(id, $"src/{id}/{id}.csproj");
    }

    private static void AddTest(string path, string area, SortedSet<string> commands)
    {
        var prefix = $"tests/unit/SemanticTypeModel.{area}.Tests.Unit/";
        if (path.StartsWith(prefix, StringComparison.Ordinal))
        {
            _ = commands.Add($"test:{prefix}SemanticTypeModel.{area}.Tests.Unit.csproj");
        }
    }
}
