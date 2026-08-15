# Public Documentation Engineering

## Validation command

```sh
./eng/public-docs.sh
```

## Required checks

The validator must verify:

- required consumer entry points and guides exist;
- all packable package projects use `public-docs/nuget/SemanticTypeModel.md` as their NuGet README source;
- package IDs come from the canonical inventory in `eng/common.sh`;
- the shared NuGet README mentions every package ID;
- active SemanticTypeModel package versions do not drift;
- the shared README states the same-exact-version rule;
- deleted superseded public pages do not regrow;
- per-package NuGet README sources do not regrow;
- per-sample Markdown pages do not regrow;
- evergreen public docs do not contain milestone/release-candidate narration;
- local Markdown links resolve;
- non-root `README.md` files are rejected.

Historical release/version references in `public-docs/release-notes.md` and migration history in
`public-docs/api/compatibility.md` are excluded from evergreen-version/history checks.

## Done criteria

- `./eng/public-docs.sh` succeeds.
- Consumer-visible capabilities touched by the change answer use/configure/diagnose questions.
- No new redundant documentation surface is introduced.
