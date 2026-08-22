# Plan — Semantic Schedule definition constructor bounds

## Goal

Make `SemanticScheduleDefinition` preserve its defensive immutable snapshots without consuming unbounded lazy include/exclude/column sources before the catalog's existing supported capacities can run.

## Existing contract

- Semantic Schedule include ids: at most 5,000.
- Semantic Schedule exclude ids: at most 5,000.
- Semantic Schedule columns: 1..32 once normalized.
- Definition collections are defensive read-only snapshots.
- Catalog `Save()` and `Build()` remain responsible for semantic value validation, duplicate checks and canonicalization.

## Defect

The public definition constructor currently snapshots `includeElementIds`, `excludeElementIds` and `columns` with unrestricted `new List<T>(IEnumerable<T>)`. Huge or non-terminating lazy sources are fully consumed before `Normalize()` can enforce the existing 5,000/32 capacities.

## Implementation

1. Add constructor-local/shared bounded snapshot helpers in `SemanticScheduleCatalog.cs`.
2. Include and exclude sources accept up to 5,000 items; observing item 5,001 throws the existing capacity message and enumeration stops immediately.
3. Column sources accept up to 32 items; observing item 33 throws the existing 1..32-column capacity message and enumeration stops immediately.
4. Preserve `AsReadOnly()` snapshots and keep categories unchanged because this lane does not invent a categories cardinality policy.
5. Preserve null/malformed/duplicate semantic validation for accepted-size collections in `Normalize()`.

## Regression

Add adversarial lazy sources that throw a sentinel exception if the constructor asks for one item beyond the first over-bound item:

- include ids: yield 5,001, sentinel at 5,002;
- exclude ids: yield 5,001, sentinel at 5,002;
- columns: yield 33, sentinel at 34.

The expected exception must be the normal Semantic Schedule capacity exception, never the sentinel. Also keep a bounded snapshot case proving source mutation after construction cannot mutate the definition.

## Static guard

A focused preflight will require:

- constructor use of bounded snapshot helpers for include/exclude/columns;
- explicit 5,000 and 32 capacity guards before adding the over-bound item;
- read-only snapshots;
- no return to unrestricted `new List<string>(includeElementIds...)`, `new List<string>(excludeElementIds...)` or `new List<SemanticDocumentationColumn>(columns...)` constructor materialization.

## Validation boundary

No GitHub Actions are dispatched. Remote validation is source/diff/static-contract review plus committed deterministic smoke/preflight coverage. No BricsCAD V25/V26 runtime PASS is claimed.
