# Engineering

## Purpose

Engineering documents define repository commands, toolchain setup, validation commands, implementation constraints, and release packaging policies.

## Validation Tiers

Use validation tiers to keep local feedback focused while preserving reliable completion gates.

| Tier | Name | Typical commands | Use |
|---|---|---|---|
| Tier 0 | Static/documentation | `dotnet format --verify-no-changes --include <files>`, `./eng/public-docs.sh` for public docs, shell syntax checks | Documentation-only edits, script edits, and very small static changes. |
| Tier 1 | Focused validation | `./eng/test-project.sh <project>`, `./eng/test-filter.sh <filter>`, targeted `dotnet test` through canonical wrappers | Inner-loop implementation validation for the affected project or scenario. |
| Tier 2 | Repository check | `./eng/check.sh` | Standard completion gate for implementation work. Runs restore, build, short-running tests, and format verification. |
| Tier 3 | Release candidate | `./eng/package.sh <version>`, `./eng/package-smoke.sh <version>`, `./eng/public-docs.sh`, `./eng/samples.sh`, `./eng/release-check.sh <version>` | Packaging and release-readiness validation before publishing. |
| Tier 4 | Publish | `./eng/release-check.sh <version>` then `./eng/publish.sh <version>` or the publish workflow | Final release publication. Requires configured credentials and explicit release intent. |

`./eng/check.sh` is Tier 2. It is the normal implementation completion gate, but it is not mandatory for every inner-loop validation run when a narrower Tier 1 command is more appropriate.

## Implementation Constraints

- Prefer documented behavior over inferred behavior.
- Keep changes scoped to the task.
- Avoid opportunistic refactoring and unnecessary abstractions.
- Preserve existing public contracts unless the relevant spec changes.
- Do not silently change behavior.
- Do not introduce terminology that is absent from `docs/TERMINOLOGY.md`.
- Update specs when behavior changes.
- Update architecture documents when structure changes.
- Update decisions when durable rationale is introduced.
- Update public documentation surfaces when consumer-facing behavior changes.

## Testing Constraints

- Create short-running deterministic tests by default.
- Short-running tests must avoid network dependencies, arbitrary sleeps, large datasets, and expensive benchmark behavior.
- Long-running tests, benchmarks, stress tests, and release-only integration scenarios must be explicit and isolated from the default short-running test path.
- Prefer focused Tier 1 validation during implementation, then Tier 2 before completion.

## Public Documentation Synchronization

Consumer-visible changes must be reflected in:

- `README.md` when repository entry guidance changes;
- `docs/PUBLIC-DOCS.md` when mapping or authority changes;
- relevant files under `public-docs/`;
- package README sources under `public-docs/nuget/` when package usage changes.

Run `./eng/public-docs.sh` when public documentation changes.

## Diagnostics

When adding or modifying diagnostics:

- Reserve a new ID in the appropriate STM range; never reuse a retired ID.
- Add a `public const string` to `StmDiagnosticIds` (STM0xxx/STM3xxx) or `DotNetExtractionDiagnosticIds` (STM5xxx).
- For compile-time diagnostics, add a static `DiagnosticDescriptor` field to `GeneratorDiagnosticDescriptors`; do not create descriptors inline.
- Add a reference entry to the relevant `public-docs/diagnostics/stm{range}.md` page.
- Run diagnostic stability tests to confirm uniqueness.

See `docs/specs/diagnostics.md` for the full diagnostic specification.

## Available Engineering Documents

| Document | Purpose |
|---|---|
| engineering/dotnet.md | .NET engineering profile |
| engineering/command-contract.md | Canonical repository command contract |
| engineering/packaging.md | NuGet packaging and publishing policy |
| engineering/package-documentation.md | Package README and usage-guide documentation expectations |
| engineering/release-readiness.md | Release gate sequence |
| engineering/public-documentation.md | Public documentation validation policy |
| engineering/samples.md | Runnable sample engineering policy |
