# Sample - End-to-End Code-First Schema Authoring

Project: `samples/code-first-authoring`

Demonstrates:

- annotated C# domain types as the source of truth;
- generated canonical `AppSemanticTypeModel.Create()` model usage;
- Draft 2020-12 JSON Schema export;
- JSON Editor-compatible UI-hint export;
- EF Core `ModelBuilder.ApplySemanticTypeModel(...)` projection;
- deterministic sample artifact output to `artifacts/samples/code-first-authoring/`.

Run:

```sh
dotnet run --project samples/code-first-authoring/code-first-authoring.csproj
```
