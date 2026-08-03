# Code-First EF Core Sample

## Scenario

The package-based sample generates a semantic model, derives `EfRelationalModel`, and applies its explicit CLR tables and columns to `ModelBuilder`.

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample validates the 2.5.0 fixed contract without selecting a provider, connecting to a database, running migrations, or generating a `DbContext`.
