# Contributing

This file is the human collaborator entry point. It routes to current repository authority instead of
repeating it.

## Build and validate

Use repository commands under `eng/`:

```sh
./eng/restore.sh
./eng/build.sh
./eng/test.sh
./eng/check.sh
```

For focused validation and exact command contracts, read
[`docs/ENGINEERING.md`](docs/ENGINEERING.md) and
[`docs/engineering/command-contract.md`](docs/engineering/command-contract.md).

## Find the authority for your change

| Change | Read first |
|---|---|
| Current behavior | `docs/SPECS.md` and the relevant spec |
| Package/dependency/compile-time structure | `docs/ARCHITECTURE.md` |
| Non-obvious architectural rationale | `docs/DECISIONS.md` |
| Terminology | `docs/TERMINOLOGY.md` |
| Consumer usage/configuration/diagnostics | `docs/PUBLIC-DOCS.md` and relevant `public-docs/` page |
| CI/package/release automation | `docs/WORKFLOWS.md`, workflow docs, and workflow YAML |
| Active sequenced implementation work | `docs/MILESTONES.md` and the active milestone if one exists |

The working tree carries current truth. Git history carries superseded designs and completed work.

## Consumer-facing changes

A consumer-facing capability is not fully documented until a user can answer:

1. How do I use it?
2. How do I configure/customize it?
3. What can go wrong and how do I fix it?

Update the smallest existing public authority that answers those questions. In particular:

- generator/library configuration -> `public-docs/configuration.md`;
- projection usage/configuration/errors -> relevant `public-docs/guides/*.md`;
- recurring failure symptoms -> `public-docs/troubleshooting.md`;
- diagnostic IDs -> `public-docs/diagnostics.md` or a range page;
- migration/compatibility -> `public-docs/api/compatibility.md`;
- version chronology -> `public-docs/release-notes.md`.

Run:

```sh
./eng/public-docs.sh
```

## Package version rule

`SemanticTypeModel.*` packages form one aligned suite. Examples, package references, release validation,
and documentation must not recommend mixed SemanticTypeModel package versions.

## Samples

Executable samples live under `samples/`. Do not add per-sample README or public Markdown pages merely to
repeat project contents. Update `public-docs/samples.md` only when the sample index/routing changes.

## Pull-request completion

Use focused validation while iterating, then run `./eng/check.sh` before completing implementation work.
Documentation-only work may use the documentation validation tier defined by `docs/ENGINEERING.md`.
