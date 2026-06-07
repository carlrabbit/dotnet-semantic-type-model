# Concepts

## Code Source

Annotated .NET code is the supported authoring source for canonical semantic type models.

## Canonical Model

The canonical semantic type model is the common representation used by extractors, generators, transformations, diagnostics, inspection, and projections.

## Core Semantics

Core semantics describe projection-neutral meaning, such as entity, value object, key, relationship, requiredness, nullability, format, constraint, or envelope. See [guides/core-semantics.md](guides/core-semantics.md).

## Envelope

An envelope is a wrapper boundary that carries, manages, versions, transports, persists, audits, authorizes, caches, or otherwise contextualizes a distinguished payload. Envelope semantics do not erase payload semantics; projection policy decides whether the envelope or payload is used as a target root.

## Transformations

Transformation pipelines normalize, validate, and derive semantic information. Domain packages use transformations to produce domain semantic models.

## Domain Semantic Models

A domain semantic model is package-owned metadata derived from the canonical model for a specific target, such as JSON Schema, EF Core, or Power BI.

## Projections

Projections produce target-specific output or configuration from a domain semantic model. Projection capability contracts document which canonical features are directly supported, option-dependent, annotation-preserved, or unsupported per target. See [guides/projection-capabilities.md](guides/projection-capabilities.md).

## System.Text.Json Names

Semantic member names identify model concepts. `System.Text.Json` property names identify serialization contracts and are preserved as `systemTextJson.propertyName` unless explicitly promoted. Semantic names may be used as JSON serialization names only through explicit resolver configuration.

## Stable Release Status

`2.0.0` is the code-first semantic model release. Documented public APIs, diagnostics, annotation keys, and package boundaries follow the compatibility policy unless explicitly marked preview.
