using System.Diagnostics;

namespace SemanticTypeModel.Engineering;

internal static class PackageSmokeRunner
{
    internal static IReadOnlyList<string> ValidateArtifacts(string packageDirectory, string version)
    {
        var errors = new List<string>();
        if (!Directory.Exists(packageDirectory))
        {
            return [$"Package directory does not exist: {packageDirectory}"];
        }

        string[] packages = [.. Directory.EnumerateFiles(packageDirectory, $"*.{version}.nupkg").Where(path => !path.EndsWith(".snupkg", StringComparison.Ordinal))];
        if (packages.Length == 0)
        {
            errors.Add($"No local .nupkg files found for version {version} in {packageDirectory}.");
        }

        if (packages.Length != RepositoryPolicy.Packages.Count)
        {
            errors.Add($"Expected {RepositoryPolicy.Packages.Count} publishable packages for version {version}, found {packages.Length} in {packageDirectory}.");
        }

        foreach (PackageProject package in RepositoryPolicy.Packages)
        {
            var expected = Path.Combine(packageDirectory, $"{package.Id}.{version}.nupkg");
            if (!File.Exists(expected))
            {
                errors.Add($"Expected package is missing: {expected}");
            }
        }
        if (File.Exists(Path.Combine(packageDirectory, $"SemanticTypeModel.JsonEditor.{version}.nupkg")))
        {
            errors.Add("SemanticTypeModel.JsonEditor is not part of the package set.");
        }

        return errors;
    }

    internal static int Run(string repositoryRoot, string version)
    {
        var packageDirectory = Path.Combine(repositoryRoot, "artifacts", "nuget");
        IReadOnlyList<string> errors = ValidateArtifacts(packageDirectory, version);
        if (errors.Count != 0)
        {
            return ProgramSupport.Report(errors);
        }

        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"semantic-type-model-smoke-{Guid.NewGuid():N}");
        try
        {
            var consumer = Path.Combine(temporaryRoot, "consumer");
            var model = Path.Combine(temporaryRoot, "model");
            _ = Directory.CreateDirectory(temporaryRoot);
            var environment = new Dictionary<string, string?> { ["NUGET_PACKAGES"] = Path.Combine(temporaryRoot, "packages") };
            if (RunDotNet(repositoryRoot, environment, "new", "console", "--framework", "net10.0", "--output", consumer) != 0)
            {
                return 1;
            }

            File.WriteAllText(Path.Combine(consumer, "NuGet.Config"), NuGetConfig(packageDirectory));
            if (RunDotNet(repositoryRoot, environment, "add", consumer, "package", "Microsoft.EntityFrameworkCore", "--version", "10.0.0") != 0)
            {
                return 1;
            }

            foreach (PackageProject package in RepositoryPolicy.Packages)
            {
                if (RunDotNet(repositoryRoot, environment, "add", consumer, "package", package.Id, "--version", version) != 0)
                {
                    return 1;
                }
            }

            if (RunDotNet(repositoryRoot, environment, "new", "classlib", "--framework", "net10.0", "--output", model) != 0)
            {
                return 1;
            }

            File.Copy(Path.Combine(consumer, "NuGet.Config"), Path.Combine(model, "NuGet.Config"));
            if (RunDotNet(repositoryRoot, environment, "add", model, "package", "SemanticTypeModel.DotNet", "--version", version) != 0)
            {
                return 1;
            }

            if (RunDotNet(repositoryRoot, environment, "add", model, "package", "SemanticTypeModel.Generators", "--version", version) != 0)
            {
                return 1;
            }

            if (RunDotNet(repositoryRoot, environment, "add", consumer, "reference", model) != 0)
            {
                return 1;
            }

            File.WriteAllText(Path.Combine(model, "Class1.cs"), ModelSource);
            File.WriteAllText(Path.Combine(consumer, "Program.cs"), ConsumerSource);
            if (RunDotNet(repositoryRoot, environment, "run", "--project", consumer, "--configuration", "Release") != 0)
            {
                return 1;
            }

            var smokeProject = Path.Combine(repositoryRoot, "tests", "package-smoke", "SemanticTypeModel.PackageSmoke.Tests", "SemanticTypeModel.PackageSmoke.Tests.csproj");
            if (RunDotNet(repositoryRoot, environment, "test", smokeProject, "--configuration", "Release", $"-p:PackageSmokeVersion={version}", $"-p:PackageSmokeSource={packageDirectory}") != 0)
            {
                return 1;
            }

            Console.WriteLine($"Package smoke validation passed for version {version}.");
            return 0;
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    private static int RunDotNet(string workingDirectory, IReadOnlyDictionary<string, string?> environment, params string[] arguments)
    {
        using var process = new Process { StartInfo = new("dotnet") { WorkingDirectory = workingDirectory, UseShellExecute = false } };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        foreach ((var key, var value) in environment)
        {
            process.StartInfo.Environment[key] = value;
        }

        if (!process.Start())
        {
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private static string NuGetConfig(string packageDirectory)
    {
        return $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration><packageSources><clear />
          <add key="local" value="{packageDirectory}" />
          <add key="nuget" value="https://api.nuget.org/v3/index.json" />
        </packageSources></configuration>
        """;
    }

    private const string ModelSource = """
        using SemanticTypeModel.DotNet;
        namespace PackageSmoke.Model;
        [SemanticType(SemanticTypeRole.Entity)]
        public sealed class SmokeOrder
        {
            [SemanticKey, SemanticDisplayIdentity, SemanticAccessPath("ById")] public int Id { get; set; }
            public string Status { get; set; } = string.Empty;
            [SemanticOwned] public SmokeOrderDetails? Details { get; set; }
        }
        [SemanticType(SemanticTypeRole.ValueObject)]
        public sealed class SmokeOrderDetails { public string Note { get; set; } = string.Empty; }
        """;

    private const string ConsumerSource = """
        using Microsoft.EntityFrameworkCore;
        using SemanticTypeModel.EFCore;
        using SemanticTypeModel.Generated.EFCore;
        using SemanticTypeModel.TestData;
        using PackageSmoke.Model;
        [assembly: GenerateSemanticEfModel(typeof(SmokeOrder))]
        internal static class Program
        {
            private static void Main()
            {
                var builder = new ModelBuilder();
                builder.ApplyAppSemanticModel();
                if (builder.Model.FindEntityType(typeof(SmokeOrder)) is null)
                    throw new InvalidOperationException("Packed EF generator did not execute.");
                var model = SemanticTypeModel.Generated.AppSemanticTypeModel.Create();
                var generated = SemanticTestDataGenerator.Generate(model, new SemanticTypeModel.Abstractions.Model.TypeId("global::PackageSmoke.Model.SmokeOrder"));
                if (generated.HasErrors || generated.Value is null)
                    throw new InvalidOperationException("Packed TestData package did not generate a valid semantic value graph.");
                var order = model.Types.OfType<SemanticTypeModel.Abstractions.Model.ObjectTypeDefinition>()
                    .Single(type => type.Name == "SmokeOrder");
                var id = order.Properties.Single(property => property.Name == "Id");
                if (!id.Annotations.Items.Any(annotation => annotation.Key.Value == "schema.displayIdentity" && annotation.Value == "0")
                    || !id.Annotations.Items.Any(annotation => annotation.Key.Value == "schema.accessPath.ById" && annotation.Value == "0"))
                    throw new InvalidOperationException("Packed generator did not preserve M0065 annotations.");
                if (builder.Model.FindEntityType(typeof(SmokeOrder))!.FindProperty(nameof(SmokeOrder.Details)) is null)
                    throw new InvalidOperationException("Packed EF generator did not configure nullable owned JSON.");
                Console.WriteLine("Package smoke consumer succeeded.");
            }
        }
        """;
}
