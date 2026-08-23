# Command Contract

## Purpose

Define the stable repository engineering commands used by humans, CI, and agents.

The logical command contract is platform-neutral. PowerShell and Bash files are equivalent platform launchers over the same repository command.

## Platform Launchers

Use the launcher native to the execution environment:

```text
Windows / PowerShell: .\eng\<command>.ps1
Linux/macOS / Bash:   ./eng/<command>.sh
```

CI may use Bash launchers on Linux. Windows development uses the PowerShell launchers.

## Validation Tiers

| Tier | Command scope | Windows | Bash / CI |
|---|---|---|---|
| Tier 0 | Static/documentation checks | `.\eng\public-docs.ps1` as applicable | `./eng/public-docs.sh` as applicable |
| Tier 1 | Focused affected-area validation | `.\eng\test-project.ps1 <project>`, `.\eng\test-filter.ps1 <filter>`, `.\eng\check-affected.ps1 [paths...]` | matching `.sh` launchers |
| Tier 2 | Full repository implementation check | `.\eng\check.ps1` | `./eng/check.sh` |
| Tier 3 | Release candidate/package validation | `.\eng\package.ps1 <version>`, `.\eng\package-smoke.ps1 <version>`, `.\eng\public-docs.ps1`, `.\eng\samples.ps1`, `.\eng\release-check.ps1 <version>` | matching `.sh` launchers |
| Tier 4 | Publish validation | `.\eng\release-check.ps1 <version>` then explicit publish operation | matching `.sh` launcher or publish workflow |

Tier 2 is the normal implementation completion gate. Prefer Tier 1 for fast inner-loop validation when the affected area is known.

## Canonical Logical Commands

| Command | Purpose |
|---|---|
| `restore` | Restore all dependencies |
| `build` | Build the solution |
| `test` | Run all short-running tests |
| `test-project <project>` | Run short-running tests for one test project |
| `test-filter <filter>` | Run focused short-running tests; a leading `/` continues to denote a complete MTP tree-node filter |
| `check-affected [paths...]` | Select focused validation from changed paths or fall back to Tier 2 |
| `format` | Apply repository formatting |
| `check` | Restore, build, run short-running tests, and verify formatting |
| `benchmark` | Run benchmarks in Release mode |
| `samples` | Build and run runnable samples against prepared local packages |
| `public-docs` | Validate public-documentation surfaces and package-documentation consistency |
| `package <version>` | Pack the aligned SemanticTypeModel NuGet suite into `artifacts/nuget` |
| `package-smoke <version>` | Validate consumption of the packed suite |
| `release-check <version>` | Run release-readiness validation without publishing |
| `publish <version>` | Publish local package artifacts; requires explicit release intent and credentials |

## Rules

- Humans, agents, and CI use the canonical `eng` command surface rather than duplicating repository policy.
- PowerShell and Bash launchers for one logical command must remain behaviorally equivalent.
- Bare focused terms remain repository source/project selectors; use a leading `/` for a complete MTP tree-node filter.
- CI uses the platform-appropriate `check` launcher instead of duplicating logic.
- `release-check <version>` must not publish.
- `publish <version>` requires explicit release intent and configured credentials.
- All commands required for the applicable validation tier must succeed before that validation tier is considered complete.
- Validation success is evidence; milestone completion additionally requires the milestone completion audit.

## Implementation Levels

The `eng/` filenames remain the stable command API. Direct process launchers remain thin. Repository policy, package inventory, documentation validation, and package-smoke orchestration belong in tested .NET engineering code where practical.

Platform launchers forward arguments and subprocess exit status unchanged.
