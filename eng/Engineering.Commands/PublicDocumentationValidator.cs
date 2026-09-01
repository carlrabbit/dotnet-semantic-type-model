using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SemanticTypeModel.Engineering;

internal static partial class PublicDocumentationValidator
{
    private static readonly string[] RequiredFiles =
    [
        "README.md", "CONTRIBUTING.md", "AGENTS.md", "docs/PUBLIC-DOCS.md", "public-docs/usage.md",
        "public-docs/configuration.md", "public-docs/troubleshooting.md", "public-docs/diagnostics.md",
        "public-docs/versioning.md", "public-docs/release-notes.md", "public-docs/samples.md",
        "public-docs/api/compatibility.md", "public-docs/guides/core-semantics.md",
        "public-docs/guides/json-schema.md",
        "public-docs/guides/ef-core.md", "public-docs/guides/power-bi.md",
        "public-docs/guides/system-text-json.md", "public-docs/guides/test-data.md",
        "public-docs/guides/projection-capabilities.md", "public-docs/nuget/SemanticTypeModel.md",
        "public-docs/diagnostics/stm0xxx.md", "public-docs/diagnostics/stm1xxx.md",
        "public-docs/diagnostics/stm3xxx.md", "public-docs/diagnostics/stm5xxx.md",
    ];

    private static readonly string[] ForbiddenPaths =
    [
        "public-docs/getting-started.md", "public-docs/installation.md", "public-docs/concepts.md",
        "public-docs/packages.md", "public-docs/api/public-api.md", "public-docs/guides/ef-core-projection.md",
        "public-docs/guides/power-bi-projection.md", "public-docs/guides/configuration.md",
        "public-docs/diagnostics/preview-status.md", "docs/engineering/building-blocks.md",
    ];

    private static readonly string[] StaleTokens =
    [
        "public-docs/getting-started.md", "public-docs/installation.md", "public-docs/concepts.md",
        "public-docs/packages.md", "public-docs/api/public-api.md", "guides/ef-core-projection.md",
        "guides/power-bi-projection.md", "guides/configuration.md", "public-docs/samples/",
    ];

    internal static IReadOnlyList<string> Validate(string root)
    {
        var errors = new List<string>();
        foreach (var file in RequiredFiles)
        {
            if (!File.Exists(Resolve(root, file)))
            {
                errors.Add($"Missing public documentation file: {file}");
            }
        }
        foreach (var file in ForbiddenPaths)
        {
            if (Path.Exists(Resolve(root, file)))
            {
                errors.Add($"Superseded documentation path must not exist: {file}");
            }
        }

        var samplesDirectory = Resolve(root, "public-docs/samples");
        if (Directory.Exists(samplesDirectory) && Directory.EnumerateFiles(samplesDirectory, "*.md", SearchOption.AllDirectories).Any())
        {
            errors.Add("Per-sample public Markdown pages are not allowed; use public-docs/samples.md and executable sample source.");
        }

        var nugetDirectory = Resolve(root, "public-docs/nuget");
        string[] nugetDocs = Directory.Exists(nugetDirectory)
            ? [.. Directory.EnumerateFiles(nugetDirectory, "*.md").Select(path => Relative(root, path)).Order(StringComparer.Ordinal)]
            : [];
        if (!nugetDocs.SequenceEqual(["public-docs/nuget/SemanticTypeModel.md"], StringComparer.Ordinal))
        {
            errors.Add("Exactly one shared NuGet README source is allowed: public-docs/nuget/SemanticTypeModel.md");
        }

        string[] readmes = [.. Directory.EnumerateFiles(root, "README.md", SearchOption.AllDirectories)
            .Where(path => !IsUnder(path, Resolve(root, ".git")) && !string.Equals(Relative(root, path), "README.md", StringComparison.Ordinal))];
        if (readmes.Length != 0)
        {
            errors.Add("Non-root README.md files are not allowed.");
        }

        var sharedReadme = Read(root, "public-docs/nuget/SemanticTypeModel.md");
        var rootReadme = Read(root, "README.md");
        if (!sharedReadme.Contains("same exact version", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Shared NuGet README must state the same-exact-version rule.");
        }

        if (!rootReadme.Contains("same exact version", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("README.md must state the same-exact-version rule.");
        }

        ValidatePackageProjects(root, sharedReadme, errors);

        string[] activeDocs = [.. Directory.EnumerateFiles(Resolve(root, "public-docs"), "*.md", SearchOption.AllDirectories)
            .Where(path => Relative(root, path) is not "public-docs/release-notes.md" and not "public-docs/api/compatibility.md")
            .Prepend(Resolve(root, "README.md"))];
        ValidateVersions(activeDocs, root, errors);
        foreach (var path in activeDocs)
        {
            var text = File.ReadAllText(path);
            if (MilestoneRegex().IsMatch(text))
            {
                errors.Add($"Evergreen public doc contains milestone narration: {Relative(root, path)}");
            }

            if (ReleaseNarrationRegex().IsMatch(text))
            {
                errors.Add($"Evergreen public doc contains release-candidate narration: {Relative(root, path)}");
            }
        }

        string[] referenceDocs = [.. activeDocs, Resolve(root, "CONTRIBUTING.md"), Resolve(root, "AGENTS.md"), Resolve(root, "docs/PUBLIC-DOCS.md")];
        foreach (var path in referenceDocs.Distinct(StringComparer.Ordinal))
        {
            var text = File.ReadAllText(path);
            foreach (var token in StaleTokens.Where(text.Contains))
            {
                errors.Add($"{Relative(root, path)} references superseded documentation path/token: {token}");
            }
        }
        ValidateLinks(root, referenceDocs, errors);
        return errors;
    }

    private static void ValidatePackageProjects(string root, string sharedReadme, List<string> errors)
    {
        foreach (PackageProject package in RepositoryPolicy.Packages)
        {
            if (!sharedReadme.Contains($"`{package.Id}`", StringComparison.Ordinal))
            {
                errors.Add($"Shared NuGet README is missing package ID {package.Id}.");
            }

            var path = Resolve(root, package.ProjectPath);
            if (!File.Exists(path)) { errors.Add($"Package project is missing: {package.ProjectPath}."); continue; }
            var project = XDocument.Load(path);
            string[] ids = [.. project.Descendants("PackageId").Select(node => node.Value)];
            if (!ids.SequenceEqual([package.Id], StringComparer.Ordinal))
            {
                errors.Add($"{package.ProjectPath} PackageId must be exactly {package.Id}.");
            }

            string[] readmes = [.. project.Descendants("PackageReadmeFile").Select(node => node.Value)];
            if (!readmes.SequenceEqual(["README.md"], StringComparer.Ordinal))
            {
                errors.Add($"{package.ProjectPath} must set PackageReadmeFile to README.md.");
            }

            string[] packed = [.. project.Descendants("None").Where(node => string.Equals(node.Attribute("Pack")?.Value, "true", StringComparison.OrdinalIgnoreCase) && node.Attribute("PackagePath")?.Value == "README.md").Select(node => node.Attribute("Include")?.Value ?? "")];
            if (!packed.SequenceEqual(["../../public-docs/nuget/SemanticTypeModel.md"], StringComparer.Ordinal))
            {
                errors.Add($"{package.ProjectPath} must pack the shared README as README.md.");
            }
        }
    }

    private static void ValidateVersions(IEnumerable<string> paths, string root, List<string> errors)
    {
        var versions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var text = File.ReadAllText(path);
            foreach (Match match in PackageVersionRegex().Matches(text).Cast<Match>().Concat(PackageReferenceVersionRegex().Matches(text).Cast<Match>()))
            {
                var version = match.Groups[2].Value;
                if (!versions.TryGetValue(version, out List<string>? uses))
                {
                    versions[version] = uses = [];
                }

                uses.Add($"{Relative(root, path)}:{match.Groups[1].Value}");
            }
        }
        if (versions.Count > 1)
        {
            errors.Add("Evergreen docs contain mixed SemanticTypeModel package versions: " + string.Join("; ", versions.Keys.Order(StringComparer.Ordinal)));
        }
    }

    private static void ValidateLinks(string root, IEnumerable<string> paths, List<string> errors)
    {
        foreach (var path in paths.Distinct(StringComparer.Ordinal))
        {
            foreach (Match match in MarkdownLinkRegex().Matches(File.ReadAllText(path)))
            {
                var target = match.Groups[1].Value.Trim().Split(' ')[0].Trim('<', '>').Split('#')[0];
                if (target.Length == 0 || target.StartsWith("http://", StringComparison.Ordinal) || target.StartsWith("https://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal))
                {
                    continue;
                }

                var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, target));
                if (!IsUnder(resolved, root))
                {
                    errors.Add($"{Relative(root, path)} links outside repository: {target}");
                }
                else if (!Path.Exists(resolved))
                {
                    errors.Add($"{Relative(root, path)} has broken local link: {target}");
                }
            }
        }
    }

    private static string Read(string root, string path)
    {
        return File.Exists(Resolve(root, path)) ? File.ReadAllText(Resolve(root, path)) : string.Empty;
    }

    private static string Resolve(string root, string path)
    {
        return Path.GetFullPath(Path.Combine(root, path));
    }

    private static string Relative(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }

    private static bool IsUnder(string path, string root)
    {
        return Path.GetFullPath(path).StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal) || string.Equals(Path.GetFullPath(path), Path.GetFullPath(root), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\bM\d{4}\b", RegexOptions.CultureInvariant)] private static partial Regex MilestoneRegex();
    [GeneratedRegex(@"\brelease[- ]preparation\b|\brelease candidate\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)] private static partial Regex ReleaseNarrationRegex();
    [GeneratedRegex(@"dotnet add package\s+(SemanticTypeModel\.[\w.]+)\s+--version\s+([^\s`]+)", RegexOptions.CultureInvariant)] private static partial Regex PackageVersionRegex();
    [GeneratedRegex("<Package(?:Reference|Version)\\s+Include=\"(SemanticTypeModel\\.[^\"]+)\"[^>]*\\sVersion=\"([^\"]+)\"", RegexOptions.CultureInvariant)] private static partial Regex PackageReferenceVersionRegex();
    [GeneratedRegex(@"(?<!!)\[[^\]]*\]\(([^)]+)\)", RegexOptions.CultureInvariant)] private static partial Regex MarkdownLinkRegex();
}
