using System.Diagnostics;
using SemanticTypeModel.Engineering;

return args.Length == 0 ? Usage() : args[0] switch
{
    "check-affected" => CheckAffected(args[1..]),
    "public-docs" => PublicDocs(),
    "package-ids" => PrintInventory(projects: false),
    "package-projects" => PrintInventory(projects: true),
    "package-smoke" => args.Length == 2 ? PackageSmokeRunner.Run(Directory.GetCurrentDirectory(), args[1]) : Usage(),
    _ => Usage(),
};

static int CheckAffected(string[] paths)
{
    if (paths.Length == 0)
    {
        return Run("eng/check.sh", []);
    }

    foreach (var command in RepositoryPolicy.CommandsForAffectedPaths(paths))
    {
        var result = command switch
        {
            "check" => Run("eng/check.sh", []),
            "public-docs" => Run("eng/public-docs.sh", []),
            "samples" => RunSamples(),
            _ when command.StartsWith("test:", StringComparison.Ordinal) => Run("eng/test-project.sh", [command[5..]]),
            _ => 1,
        };
        if (result != 0)
        {
            return result;
        }
    }
    return 0;
}

static int RunSamples()
{
    var version = Environment.GetEnvironmentVariable("SEMANTIC_TYPE_MODEL_CHECK_AFFECTED_PACKAGE_VERSION") ?? "0.0.0-check-affected";
    var packed = Run("eng/package.sh", [version]);
    return packed == 0 ? Run("eng/samples.sh", []) : packed;
}

static int PublicDocs()
{
    IReadOnlyList<string> errors = PublicDocumentationValidator.Validate(Directory.GetCurrentDirectory());
    if (errors.Count != 0)
    {
        return ProgramSupport.Report(errors);
    }

    Console.WriteLine("Public documentation validation passed.");
    return 0;
}

static int PrintInventory(bool projects)
{
    foreach (PackageProject package in RepositoryPolicy.Packages)
    {
        Console.WriteLine(projects ? package.ProjectPath : package.Id);
    }

    return 0;
}

static int Run(string file, IEnumerable<string> arguments)
{
    using var process = new Process { StartInfo = new(file) { UseShellExecute = false } };
    foreach (var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    if (!process.Start())
    {
        return 1;
    }

    process.WaitForExit();
    return process.ExitCode;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: Engineering.Commands <check-affected|public-docs|package-ids|package-projects|package-smoke>");
    return 2;
}

internal static class ProgramSupport
{
    internal static int Report(IEnumerable<string> errors)
    {
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }

        return 1;
    }
}
