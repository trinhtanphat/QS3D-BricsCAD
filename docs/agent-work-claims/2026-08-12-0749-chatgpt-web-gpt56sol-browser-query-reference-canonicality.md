# Work claim — filtered Project Browser reference canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-query-reference-canonicality-20260812-0749`
- Registered: `2026-08-12T07:49:00+07:00`
- Baseline main SHA: `117f529eaf88b8b30ddc8a788e849924915f0eb6`
- Priority: P2 — keep filtered Browser queries from silently normalizing semantic relation state rejected by QSDB.

## Reserved scope

`ProjectBrowserQueryPlanner` has a separate validation path for filtered queries. Its `ValidateElementReferences(...)` still trims mutable `ProjectElement.FamilyId`, `FloorId`, and `ZoneId` before lookup. Thus a padded semantic relation can pass filtered-query validation even though QSDB persistence rejects the same non-canonical project state. The unfiltered Browser path has already been hardened for floor/zone references; this lane closes the filtered-path gap and includes FamilyId because filtered queries dereference families.

## Reserved surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryReferenceCanonicalitySmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- Fail closed when non-empty element `FamilyId`, `FloorId`, or `ZoneId` is whitespace-only or contains leading/trailing whitespace before filtered-query reference lookup.
- Preserve empty optional relations, case-insensitive canonical IDs, query/filter semantics, existing family/category integrity checks, query-option/definition bounds, and ordinary unfiltered delegation.
- Do not change user-supplied filter-ID whitespace behavior in this lane.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no BricsCAD V25 runtime PASS claimed.
