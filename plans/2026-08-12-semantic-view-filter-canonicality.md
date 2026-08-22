# Semantic View Floor/Zone filter canonicality plan — 2026-08-12

## Goal

Make `SemanticViewPlanner.Build()` evaluate candidate Floor/Zone relations with the same semantic identity used by canonical reference resolution and Floor/Zone mutation no-op behavior, without changing catalog, native view or persistence semantics.

## Confirmed defect

Claim commit: `20cccc7f5c47dbede0517409d9e0b2d3a3586d76`.

Post-claim source was re-fetched from moving `main` at `0583ae9499caad16844fed00d54f85e66b54be04`. The planner still normalized/validated explicit Floor/Zone filters and then applied raw candidate comparisons:

```text
string.Equals(x.FloorId, floorId, StringComparison.OrdinalIgnoreCase)
string.Equals(x.ZoneId, zoneId, StringComparison.OrdinalIgnoreCase)
```

Existing Floor/Zone mutation semantics intentionally preserve padded/case-varied stored relation strings when they identify the same canonical target. The raw predicates therefore allow a view to resolve a valid Floor/Zone filter but silently omit an element whose stored relation is semantically identical after trimming.

## Invariants to preserve

- View id/name and optional Floor/Zone filters continue through existing normalization limits.
- Explicit Floor/Zone references still use the existing null-safe unique-reference resolver.
- Missing/ambiguous Floor/Zone references still fail closed.
- Project element ids remain uniquely validated before filtering.
- Category, include/exclude, duplicate filter-id and stale element-id validation remain unchanged.
- Selected element ids retain deterministic case-insensitive/ordinal ordering.
- Planning remains read-only: no `ProjectState.Touch()` and no relation-string rewrite.
- `BuildCatalog()` 10,000-view capacity and ordering remain unchanged.

## Implementation

For candidate filtering only:

- compare `(x.FloorId ?? string.Empty).Trim()` with the already-normalized `floorId` using `OrdinalIgnoreCase`;
- compare `(x.ZoneId ?? string.Empty).Trim()` with the already-normalized `zoneId` using `OrdinalIgnoreCase`.

No persistence normalization or migration is introduced.

## Regression coverage

`SemanticViewFilterCanonicalitySmoke` creates canonical Floor/Zone definitions and a Beam whose raw relations are `"  f-01  "` and `"  z-01  "`. A semantic view filtered to `F-01` / `Z-01` must select that element.

The smoke also proves the read-only contract by asserting:

- exactly one selected semantic element id;
- expected element id `E-01`;
- unchanged project `ChangeVersion`;
- unchanged raw FloorId and ZoneId strings.

Registration uses an isolated module initializer to avoid shared multi-agent hotspots.

## Static preflight

`preflight-semantic-view-filter-canonicality.py` requires:

- the existing canonical Floor/Zone reference validation calls;
- trimmed case-insensitive candidate relation comparisons;
- absence of the legacy raw equality expressions;
- regression evidence for selected identity, unchanged project version and unchanged raw relation strings;
- module registration.

## Moving-main integration

- Work branch: `agent/semantic-view-filter-canonicality-20260812`.
- Branch baseline: `0583ae9499caad16844fed00d54f85e66b54be04`.
- Refresh moving `main` before PR and before merge.
- If `SemanticViewPlanner.cs` changed concurrently, inspect the winner and never overwrite overlapping work.
- Otherwise merge only the two predicate changes plus isolated regression/preflight/plan files.
- Close the claim on `main` with exact PR/merge evidence.

## Validation policy

This is deterministic Core read-only planning behavior. GitHub Actions are manual-only and are not dispatched. Executable smoke/preflight PASS and licensed BricsCAD runtime PASS are not claimed unless actually run in an appropriate environment.
