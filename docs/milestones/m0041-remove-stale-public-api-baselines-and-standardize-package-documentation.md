# M0041 - Remove Stale Public API Baselines and Standardize Package Documentation

## Execution Mode

`ai-executed-human-reviewed`

## Authority Documents

- `docs/ENGINEERING.md`
- `docs/engineering/command-contract.md`
- `docs/engineering/package-documentation.md`
- `docs/engineering/release-readiness.md`
- `docs/PUBLIC-DOCS.md`
- `docs/DECISIONS.md`
- `docs/decisions/remove-fake-public-api-baselines.md`
- `public-docs/api/compatibility.md`

## Focus Areas

1. Delete stale text API baseline files from package projects.
2. Remove the stale script-only baseline checker and remove it from release validation.
3. Remove repository references to the deleted baseline files and script.
4. Document compatibility review through smoke tests, samples, public docs, release notes, compatibility docs, and human review.
5. Standardize package documentation expectations in `docs/engineering/package-documentation.md`.
6. Update directly affected package README mappings and validation only; defer broad prose standardization to future documentation work.

## Human Review Required

Human review is required before accepting the removed baseline files, removed release-validation script invocation, compatibility wording, and package README / usage-guide standards.

## Validation

```sh
./eng/check.sh
./eng/package.sh 0.0.0-m0041
./eng/package-smoke.sh 0.0.0-m0041
./eng/samples.sh
./eng/public-docs.sh
./eng/release-check.sh 0.0.0-m0041
```
