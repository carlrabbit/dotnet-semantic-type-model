# Engineering

## Purpose

Engineering documents define repository commands, toolchain setup, validation commands, implementation constraints, documentation lifecycle, and release packaging policies.

The canonical command surface is platform-neutral. Use the PowerShell launcher on Windows and the matching Bash launcher on Linux/macOS; `docs/engineering/command-contract.md` is authoritative for launcher equivalence.

## Validation Tiers

Use validation tiers to keep local feedback focused while preserving reliable completion gates.

| Tier | Name | Typical logical commands | Use |
|---|---|---|---|
| Tier 0 | Static/documentation | `public-docs` as applicable, format verification for touched code, script syntax checks | Documentation-only edits, script edits, and very small static changes. |
| Tier 1 | Focused validation | `test-project <project>`, `test-filter <filter>`, targeted tests through canonical wrappers | Inner-loop implementation validation for the affected project or scenario. |
| Tier 2 | Repository check | `check` | Standard completion gate for implementation work; restores, builds, runs short tests, and verifies formatting. |
| Tier 3 | Release candidate | `package <version>`, `package-smoke <version>`, `public-docs`, `samples`, `release-check <version>` | Packaging and release-readiness validation before publishing. |
| Tier 4 | Publish | `release-check <version>` then explicit publish operation | Final release publication; requires configured credentials and explicit release intent. |

Use `.\eng\check.ps1` on Windows or `./eng/check.sh` on Bash-capable platforms for Tier 2. Prefer Tier 1 commands for fast iteration when a narrower command is appropriate.

## Implementation Constraints

- Prefer documented behavior over inferred behavior.
- Keep changes scoped to the task.
- Avoid opportunistic refactoring and unnecessary abstractions.
- Preserve existing public contracts unless the relevant spec changes.
- Do not silently change behavior.
- Do not introduce terminology that is absent from `docs/TERMINOLOGY.md`.
- Update specs when behavior changes.
- Update architecture documents when structure changes.
- Update decisions when durable rationale is introduced or replaced.
- Update public documentation surfaces when consumer-facing behavior changes unless an active milestone explicitly defers that synchronization through `.guide-sync/pending/`.

## Documentation Lifecycle

The repository working tree carries **current truth and active work**. Git carries detailed historical work and superseded designs.

### Authority roles

- Specifications define exact current behavioral contracts.
- Architecture defines major structural boundaries and dependency direction.
- Decisions explain non-obvious current rationale.
- Milestones route active implementation work.
- Public docs explain supported consumer usage.
- `HISTORY.md` summarizes architectural evolution without becoming a changelog or milestone archive.

### Completion and supersession

- After a milestone is implemented and durable outcomes are synchronized into specs/decisions/architecture/public docs/tests as appropriate, delete the completed milestone file.
- Never reuse milestone numbers.
- Delete superseded decisions/specifications after promoting any still-current requirement or rationale into replacement authority.
- Do not create an archive directory merely to retain files already preserved by Git.
- `.guide-sync/pending/` contains only unresolved current synchronization work and should normally be empty outside explicit deferred synchronization.
- Do not copy external setup/engineering guides into `docs/research/`.

### Anti-regrowth rule

Prefer modifying an existing authoritative document.

Create a new document only for a distinct authority boundary such as:

- a new independently implementable subsystem contract;
- a new durable cross-cutting/non-obvious architectural decision;
- a new consumer/package/audience entry point;
- an active milestone;
- a genuinely separate research subject.

A new feature, bug fix, diagnostic, mapping rule, release, or implementation detail normally extends existing authority rather than creating a new file.

## Testing Constraints

- Create short-running deterministic tests by default.
- Short-running tests must avoid network dependencies, arbitrary sleeps, large datasets, and expensive benchmark behavior.
- Long-running tests, benchmarks, stress tests, and release-only integration scenarios must be explicit and isolated from the default short-running test path.
- Prefer focused Tier 1 validation during implementation, then Tier 2 before completion.
- Boundary-crossing generator/provider/package defects require tests that exercise the actual boundary; do not substitute hand-built internal models for the integration being validated.
- EF Core compatibility tests follow `docs/engineering/ef-core-testing.md`.

## Public Documentation Synchronization

Consumer-visible changes must be reflected in:

- `README.md` when repository entry guidance changes;
- `docs/PUBLIC-DOCS.md` when mapping or authority changes;
- relevant files under `public-docs/`;
- the shared package README source under `public-docs/nuget/` when package usage changes.

When an active milestone explicitly selects the repository's separate documentation-sync policy, record the unresolved consumer work under `.guide-sync/pending/` and complete it in that later synchronization pass.

Run the platform-native `public-docs` launcher when public documentation changes.

Do not infer publication state from a completed release-preparation milestone, a package existing in source, or a README version snippet. Publication/version guidance must be based on verified package publication truth.

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
| engineering/command-contract.md | Canonical repository command contract and platform launchers |
| engineering/ef-core-testing.md | EF Core compatibility matrix, test layers, fixture architecture, and package-smoke requirements |
| engineering/packaging.md | NuGet packaging and publishing policy |
| engineering/package-documentation.md | Package README and usage-guide documentation expectations |
| engineering/release-readiness.md | Release gate sequence |
| engineering/public-documentation.md | Public documentation validation policy |
| engineering/samples.md | Runnable sample engineering policy |
