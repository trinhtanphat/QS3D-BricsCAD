# Work claim — Selection Inspector input-enumeration freshness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:27:00+07:00`
- Baseline main SHA observed: `dd5213b42b5d0edf6d15d2eacb334379be97803e`
- Priority: P2 — Core read-side snapshot consistency

## Confirmed defect

`SemanticSelectionInspector.Inspect(...)` builds project/family indexes and then enumerates the caller-supplied `IEnumerable<string>` without checking `ProjectState.ChangeVersion`. A re-entrant/lazy enumerable can mutate the project while yielding selection ids, after the indexes were captured but before the inspection snapshot is assembled. The method can then combine stale indexed element/family state with current project lookups instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs` — input enumeration freshness guard only
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-selection-inspector-input-freshness.md`
- this claim file

## Contract

1. Normal deterministic selection inspection remains unchanged.
2. If project `ChangeVersion` changes while caller selection ids are being materialized, inspection fails before building/returning a mixed-state result.
3. Element/family/reference/property/quantity validation semantics remain unchanged.
4. No BricsCAD/native CAD or release workflow changes.

## Validation boundary

Focused deterministic Core smoke plus exact source diff and moving-main ancestry review. No GitHub Actions dispatch and no licensed BricsCAD runtime PASS claim.
