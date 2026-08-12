# Agent work claim — Semantic Selection Inspector structural freshness

Status: ACTIVE
Owner: ChatGPT Web / GPT-5.6 Sol
Registered: 2026-08-12
Baseline main: 7b4a379da15c8c0bed60536bc0ccca7334eb4712

## Scope

- `src/QS3D.Core/Selection/SemanticSelectionInspector.cs`
  - Guard semantic selection inspection against direct `ProjectState.Elements` / `ProjectState.Families` ownership replacement/removal/addition during caller-controlled selected-ID enumeration when `ChangeVersion` does not change.
  - Fail closed before reading the stale snapshot and again before returning an inspection.
- `tests/QS3D.Core.SmokeTests/SemanticSelectionInspectorStructuralFreshnessSmoke.cs`
  - Regression for selected-element replacement and referenced-family replacement during selected-ID enumeration, plus stable inspection behavior.
- This claim file only.

## Defect evidence

`Inspect` snapshots `project.ChangeVersion`, then builds element/family indexes before enumerating caller-provided `elementIds`. The enumerable can directly mutate the public project collections without touching `ChangeVersion`. Replacing an element or family with a new instance under the same ID leaves the version unchanged, so the current code continues with the old indexed instance and can return stale properties/quantities to the semantic property inspector.

## Validation boundary

Focused source/readback + Core smoke source only unless an executable build is actually run. No GitHub Actions or BricsCAD runtime PASS is claimed by this lane.
