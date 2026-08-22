# Agent work claim — Semantic Selection Inspector structural freshness

Status: COMPLETED
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

`Inspect` snapshots `project.ChangeVersion`, then builds element/family indexes before enumerating caller-provided `elementIds`. The enumerable can directly mutate the public project collections without touching `ChangeVersion`. Replacing an element or family with a new instance under the same ID leaves the version unchanged, so the previous code continued with the old indexed instance and could return stale properties/quantities to the semantic property inspector.

## Delivery

- Claim: `0fa97a760133654d8bd7ab8d11a83f07d4d3c3c6`
- Source fix: `9e296226da2dfa1cdde67dfb6df51c5888fa4471`
- Regression smoke: `fd11267606dd596646b80554430b55bdfe8f7bcc`
- Smoke nullable cleanup: `03fade8f39ad5448722f640ebecd3b8d4f9c6c5f`

The source now snapshots canonical element/family ownership, verifies collection counts, duplicate/null state and reference identity after caller-controlled selected-ID enumeration, then verifies the same snapshot again before returning the inspection. Stable inspection behavior remains unchanged.

## Validation boundary

Source and smoke files were read back from `main`. No GitHub Actions, executable .NET/Core smoke run, or BricsCAD runtime PASS is claimed by this lane.
