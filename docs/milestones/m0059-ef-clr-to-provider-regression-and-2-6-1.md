# M0059: CLR-to-Provider EF Regression Contract and 2.6.1 Preparation

## Status

Completed as a non-publishing 2.6.1 patch-release preparation milestone.

## Completion Notes

- Pre-fix reproduction: the compiled CLR/provider regression finalized SQLite metadata with `GetProviderClrType()` unset for enum columns; the targeted assertion failed with `Expected to be equal to System.String but received` before the production fix.
- Root cause: enum conversion used the shorthand EF conversion API without explicitly stamping provider CLR metadata, leaving finalized model metadata insufficient for provider audit paths.
- Production correction: enum scalar application now creates the enum-to-string converter through the actual enum CLR type and explicitly sets the provider CLR type to `string`.
- Regression fixture: `tests/fixtures/SemanticTypeModel.RealWorldFixtures/M0059ClrEnumRegression/Model.cs` is a compiled CLR model with required/nullable enum members and an enum-guarded owned ValueKind used by the integration test extraction/provider path.
- Provider path: the integration regression uses the public Roslyn source-generator driver over the compiled fixture source, generated provider source emission, `TypeSchemaModel` provider invocation, `DbContext.OnModelCreating`, semantic EF application, SQLite model finalization, metadata audit, `EnsureCreated`, insert, and reload.
- Metadata invariant: the final audit rejects unexpected entities, navigations, skip navigations, non-TPT foreign keys, shadow properties, owned EF types, keyless types, implicit join entities, and convention-discovered non-entities while asserting enum columns and enum-guarded ValueKind JSON conversion metadata.
- Publication status: packages were prepared for 2.6.1 only; publication was intentionally not performed.
