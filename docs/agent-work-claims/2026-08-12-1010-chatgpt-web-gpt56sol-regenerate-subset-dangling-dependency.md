# Work claim — Subset regeneration dangling dependency integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:10:00+07:00`
- Baseline main SHA: `90e9b4a863cafbde898993a3395438ad24f4cd23`
- Priority: evidence-driven Core targeted-regeneration relation integrity

## Confirmed defect

`RegenerationEngine.RegenerateDirtySubset(...)` scans the complete `project.Elements` collection to resolve requested targets, so it has enough information to distinguish an in-project dependency outside the selected subset from a dependency whose semantic target is missing entirely. It currently does not perform that distinction. `DependencyGraph.TopologicalDirtyOrder(...)` intentionally ignores dependencies outside the supplied candidate set; consequently, a selected dirty element can depend on `MISSING` and still regenerate.

This differs from the canonical full-graph relation contract (`DependencyGraph.Rebuild(...)`) and from full-project `RegenerateDirty()` after PR #736. The graph utility's existing subset contract must remain unchanged: a dependency outside the selected subset is valid when its target still exists in the project.

## Intended scope

- after resolving all requested subset targets and preserving unknown-target error precedence, reject selected targets whose dependency IDs do not exist in the full project identity set;
- allow dependencies that exist in `project.Elements` even when those dependencies were not selected and are clean;
- leave `DependencyGraph.TopologicalDirtyOrder(...)` unchanged;
- preserve canonical/blank/duplicate dependency validation already performed by ordering, targeted regeneration order, rollback and project-touch semantics;
- add focused Core smoke coverage.

## Reserved surfaces

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationSubsetDependencyIntegritySmoke.cs`
- this claim file

## Excluded scope

Do not modify full-project `RegenerateDirty()` semantics from PR #736, `DependencyGraph`, regeneration preview/profile APIs, generated outputs, Family/Floor/Zone work, UI/CAD adapters, build/release workflows, or other concurrent claims.

## Validation boundary

Remote/static source + regression review only. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual execution.
