# Work claim — Semantic Schedule Floor/Zone filter canonicality

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:32:00+07:00`
- Baseline main SHA observed: `11fcc65b75f1daecf718502d463a8adf0af315f0`
- Priority: P1 — deterministic semantic documentation correctness.

## Confirmed defect

`SemanticScheduleCatalog.Build()` validates the requested schedule Floor/Zone through canonical `ProjectState.FindFloor()` / `FindZone()` lookups, but then filters candidate elements with raw `x.FloorId` / `x.ZoneId` equality. Existing Floor/Zone mutation semantics intentionally treat trimmed case-insensitive relation identity as the same target and preserve padded/case-varied stored relation strings on no-op assignment. A valid schedule can therefore exclude semantically matching elements solely because their persisted/mutable relation string contains padding.

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

## Implementation plan

1. Re-fetch moving `main` after claim and verify the raw filtering remains.
2. Compare candidate element Floor/Zone relation identity after trimming against the normalized schedule id with `OrdinalIgnoreCase`.
3. Preserve category, include/exclude, ordering, stale-reference validation and header-only zero-match semantics.
4. Add a Core smoke where an element stores padded/case-varied FloorId/ZoneId but a canonical schedule filter must still include it without mutating project state or rewriting raw relations.
5. Add static preflight requiring canonicalized relation comparisons and rejecting the raw equality path.
6. Refresh moving `main`, verify no reserved-source overlap, then merge a focused PR and close the claim with exact evidence.

## Validation policy

This is pure Core read-only rendering behavior. GitHub Actions remain manual-only and are not dispatched. No licensed BricsCAD V25 runtime PASS will be claimed without actual local evidence.
