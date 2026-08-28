# Bulk edit known-Count early drift

Status: `SOURCE_READY`

Lane-Key: `issue-4357`

## Purpose

Bulk edit accepts both semantic object targets and target IDs. When an enumerable exposes a supported known Count, the first target beyond that Count is proof that the input changed or lied during traversal. That cardinality failure must occur before validation or materialization of the unexpected target.

## Source contract

`BulkEditService` owns this boundary for both object targets and target IDs.

- Negative, conflicting, or greater-than-10,000 known Count values fail during preflight.
- For counted object targets, the first item beyond known Count fails before null, semantic-id, project-ownership, or duplicate processing of that unexpected target.
- For counted target IDs, the first item beyond known Count fails before the unexpected ID is appended and before canonical-ID/project lookup validation.
- A source that produces fewer entries than its known Count fails after traversal completes; under-yield does not mutate semantic state.
- Sources without Count evidence keep the independent 10,000-entry streaming maximum and stop at max-plus-one as covered by `BulkEditTargetInputBoundSmoke`.
- Honest counted inputs retain existing target resolution, duplicate semantics, freshness/ownership validation, numeric/property behavior, Family assignment rules, and all-or-nothing semantic mutation.

The change is traversal-order hardening only. It does not relax any existing bulk-edit validation.

## Deterministic regression

`BulkEditKnownCountEarlyDriftSmoke` covers:

1. object targets reporting Count=1 and yielding a valid owned element followed by null; Count drift must outrank null validation and stop after the second `MoveNext`;
2. target IDs reporting Count=1 and yielding a valid ID followed by a blank ID; Count drift must outrank ID validation and stop after the second `MoveNext`;
3. object-target under-yield Count=2 with one item;
4. target-ID under-yield Count=2 with one item;
5. honest counted object targets;
6. honest counted target IDs.

`BulkEditTargetInputBoundSmoke` remains authoritative for pure/lazy streaming maximum behavior.

## Validation and runtime boundary

This is Core data-integrity work. Protected merge requires current-candidate `preflight` and `core` SUCCESS, including deterministic smoke and normal V25 compile validation where selected by CI.

There is no licensed BricsCAD runtime acceptance for this package. Hosted CI, Core smoke, source guards, or merge evidence must not be represented as `LOCAL_PASS`.
