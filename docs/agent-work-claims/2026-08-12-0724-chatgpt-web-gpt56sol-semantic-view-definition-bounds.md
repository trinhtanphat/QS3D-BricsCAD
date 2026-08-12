# Work claim — Semantic View definition filter bounds

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:24:00+07:00`
- Baseline main SHA observed: `a7958ce8a078e6aed627f8b825b9122d2dccb394`
- Priority: P1 — deterministic Core resource-bound correctness.

## Confirmed defect

`SemanticViewDefinition` is a public defensive-snapshot constructor that accepts lazy include/exclude element-id enumerables. It currently materializes both with unrestricted `new List<string>(IEnumerable<string>)`, while `SemanticViewPlanner` already supports at most 100,000 include ids and 100,000 exclude ids through `MaxFilterIds`. A huge or non-terminating source can therefore be consumed without bound before planner validation reaches the existing capacity.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs` — `SemanticViewDefinition` include/exclude snapshot materialization only, plus minimal shared visibility/helper needed to reuse the existing `MaxFilterIds` contract.
- Focused Core smoke regression for lazy over-bound include/exclude ids and defensive snapshot preservation.
- Focused static preflight and planning note.

## Explicit exclusions

- Categories collection policy; no category cardinality contract is invented.
- `BuildCatalog()` 10,000-view capacity, completed null-reference handling, Floor/Zone canonical filtering, element lookup/ordering semantics.
- Semantic Documentation persistence store/editor, Sheet planner, native CAD view/sheet materialization, WPF/UI.
- BricsCAD V25/V26 runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after claim and confirm include/exclude constructor snapshots remain unrestricted.
2. Reuse the existing 100,000 filter-id capacity while snapshotting include/exclude inputs; reject on the 100,001st yielded item and never request 100,002.
3. Preserve read-only snapshots and downstream `NormalizeIds()` semantic validation, duplicates, whitespace/id-length checks, include/exclude overlap validation and rendering behavior.
4. Add adversarial lazy include/exclude sources with a sentinel after the first over-bound item plus a bounded defensive-snapshot case.
5. Add focused static preflight and planning documentation.
6. Refresh moving `main`, verify zero reserved-source overlap, merge only a focused PR with expected-head protection, then close this claim with exact evidence.

## Validation policy

Pure Core resource-bound behavior. GitHub Actions remain manual-only and are not dispatched. Executable smoke/preflight PASS and licensed BricsCAD runtime PASS will not be claimed without actual execution evidence.
