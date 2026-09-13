# Solibri QA Gate 2.0 — IFC relationship evidence validation

## Purpose

P0 QA Gate 2.0 must fail closed when an IFC adapter exposes a required relationship key but supplies explicit negative/placeholder evidence instead of a real relationship identifier. This closes a gap where non-blank values such as `false`, `0`, `missing`, `none`, `n/a`, `null`, `absent`, or `no` could satisfy the required-relationship check.

## Contract

`QA2.MISSING_RELATIONSHIP` remains the public rule ID and remains `Critical` in `SolibriQuantityStrict()`. Required `IfcRel.<name>` evidence is usable only when it is non-blank and not one of the canonical negative markers, matched after trimming and case folding.

The same evidence predicate protects `IfcRel.SpatialContainer` and `IfcRel.TypeAssignment` before consistency checks run. Invalid placeholders therefore produce the missing/unusable relationship finding rather than being misinterpreted as a storey/type identifier and generating misleading mismatch findings.

A Critical active finding continues to hard-block Takeoff, BOQ, and Estimate through the existing QA Gate 2.0 decision/executor pipeline. Existing audited waiver behavior is unchanged; this lane does not alter waiver DTOs, expiry rules, severity profiles, rule IDs, or QS Intelligence integration contracts.

## Compatibility and migration

No API or persisted-schema migration is required. IFC import/adaptor code that previously emitted negative placeholders for absent relationships should instead omit the relationship, emit blank evidence, or preferably supply the canonical target identity/name when the relationship really exists. Existing meaningful identifiers such as `Level 01`, `IfcBuildingStorey#42`, or `WallType-A` remain valid.

Adapters that used `true` or `1` as coarse positive-presence markers remain compatible, although canonical relationship identifiers are preferred because they support spatial/type consistency checks.

## Validation

The auto-discovered `scripts/preflight-qa2-relationship-evidence.py` guard verifies the production source contract, negative and positive evidence fixtures, hard-gate workflow documentation, compatibility guidance, and waiver compatibility expectations.
