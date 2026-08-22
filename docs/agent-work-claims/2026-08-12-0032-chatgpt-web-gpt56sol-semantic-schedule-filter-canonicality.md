# Work claim — Semantic Schedule Floor/Zone filter canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:32:00+07:00`
- Completed: `2026-08-12T00:37:00+07:00`
- Baseline main SHA observed: `11fcc65b75f1daecf718502d463a8adf0af315f0`
- Claim commit: `a635389922783037280012be94a9d5f6b80d541e`
- PR: `#581`
- Squash merge on `main`: `e18d85f866b3ef3a333cdca68b0ae28256712f14`
- Priority: P1 — deterministic semantic documentation correctness.

## Defect closed

`SemanticScheduleCatalog.Build()` validated requested schedule Floor/Zone references through canonical `ProjectState.FindFloor()` / `FindZone()` lookups but previously filtered candidate elements with raw `x.FloorId` / `x.ZoneId` equality. Existing Floor/Zone mutation semantics intentionally treat trimmed case-insensitive relation identity as the same target and preserve padded/case-varied stored relation strings on no-op assignment. A valid schedule could therefore exclude semantically matching elements solely because their stored relation text contained padding.

## Implemented

- Floor candidate filtering now compares `(x.FloorId ?? string.Empty).Trim()` against the normalized schedule Floor id using `OrdinalIgnoreCase`.
- Zone candidate filtering now compares `(x.ZoneId ?? string.Empty).Trim()` against the normalized schedule Zone id using `OrdinalIgnoreCase`.
- Rendering remains read-only; raw stored relation strings are not rewritten and `ProjectState.Touch()` is not introduced.
- Category, include/exclude, null-element, deterministic ordering, stale-reference validation and header-only zero-match behavior remain unchanged.
- Added isolated Core regression, module registration, static preflight and planning coverage.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs` — Floor/Zone candidate filtering inside `Build()` only.
- Focused Core smoke regression for padded/case-varied relation identity.
- Focused static preflight and planning note.

## Explicit exclusions

- `Save()` bounded enumeration (completed separately in PR #574).
- Semantic Schedule constructor/collection cardinality.
- XML schema/canonical metadata format.
- Include/exclude element-id behavior.
- Native schedule placement/Table ownership, Schedule Hub or WPF.
- Floor/Zone mutation services themselves.
- BricsCAD V25 runtime qualification.

## Validation evidence

- Post-claim source re-fetched from `81032c6f86bec1b4806a9a290fb1f7a0286fda27` confirmed the raw relation comparisons remained before implementation.
- PR #581 changed exactly five files; production source changed only the two Floor/Zone filter predicates.
- Moving-main comparison before merge showed no overlap with `SemanticScheduleCatalog.cs` or this lane's new regression/plan files.
- GitHub Actions were not dispatched because repository policy is manual-only.
- Executable smoke/preflight PASS and licensed BricsCAD V25 runtime PASS are not claimed from this remote environment.
