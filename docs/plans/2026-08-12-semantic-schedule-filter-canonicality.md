# Semantic Schedule Floor/Zone filter canonicality plan — 2026-08-12

## Goal

Make `SemanticScheduleCatalog.Build()` use the same canonical Floor/Zone relation identity as the rest of semantic Core so padded/case-varied stored relation strings do not silently remove valid schedule rows.

## Confirmed defect

Claim commit: `a635389922783037280012be94a9d5f6b80d541e`.

Post-claim source re-fetched from moving `main` at `81032c6f86bec1b4806a9a290fb1f7a0286fda27` still validated schedule references through `ProjectState.FindFloor()` / `FindZone()` but filtered candidate elements using raw relation equality:

```text
string.Equals(x.FloorId, normalized.FloorId, OrdinalIgnoreCase)
string.Equals(x.ZoneId, normalized.ZoneId, OrdinalIgnoreCase)
```

Floor/Zone mutation semantics already define padded/case-varied stored relation IDs as the same semantic target and intentionally preserve the existing raw string on no-op assignment. A valid schedule can therefore resolve its Floor/Zone successfully yet omit a semantically matching element because that element retains padded relation text.

## Invariants to preserve

- Schedule Floor/Zone definitions continue to normalize through existing `Normalize()` rules.
- Missing explicit Floor/Zone references continue to fail closed before candidate filtering.
- Element category, include/exclude, null-element, ordering and table rendering semantics remain unchanged.
- Header-only zero-match output remains valid when filters genuinely match no elements.
- Rendering remains read-only: no project `Touch()`, no relation rewrite and no implicit migration.
- `Save()` bounded enumeration from PR #574 remains unchanged.
- No native Table placement/ownership or WPF behavior is changed.

## Implementation

For candidate filtering only, compare:

- `(x.FloorId ?? string.Empty).Trim()` to the normalized schedule Floor id;
- `(x.ZoneId ?? string.Empty).Trim()` to the normalized schedule Zone id;
- using existing `StringComparison.OrdinalIgnoreCase` semantics.

This does not normalize or persist the element relation. It only evaluates semantic identity consistently at the read boundary.

## Regression coverage

`SemanticScheduleFilterCanonicalitySmoke` creates canonical Floor/Zone definitions and an element whose mutable stored relations are padded/lowercase. A schedule filtered to the canonical Floor/Zone must include that element.

The regression also asserts:

- exactly one row is returned;
- the row is the expected semantic element;
- `ProjectState.ChangeVersion` is unchanged;
- the element's raw padded FloorId/ZoneId values remain unchanged.

The smoke is registered through an isolated module initializer.

## Static preflight

`preflight-semantic-schedule-filter-canonicality.py` requires canonical `FindFloor`/`FindZone` validation plus trimmed case-insensitive candidate comparisons and rejects the legacy raw equality expressions. It also pins the read-only regression evidence and module registration.

## Moving-main integration

- Work branch: `agent/semantic-schedule-filter-canonicality-20260812`.
- Branch baseline: `81032c6f86bec1b4806a9a290fb1f7a0286fda27`.
- Refresh moving `main` before PR and before merge.
- If `SemanticScheduleCatalog.cs` changed after the baseline, inspect the winner before proceeding and never overwrite overlapping work.
- Merge only the focused Build-filter patch plus isolated regression/preflight/plan files.
- Close the claim with exact PR and merge evidence.

## Validation policy

This is deterministic Core read-only rendering behavior. GitHub Actions are manual-only and are not dispatched. Executable smoke/preflight PASS and licensed BricsCAD V25 runtime PASS are not claimed unless actually run in an appropriate environment.
