# Work claim — Selection Inspector input-enumeration freshness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:27:00+07:00`
- Completed: `2026-08-12T09:39:00+07:00`
- Baseline main SHA observed: `dd5213b42b5d0edf6d15d2eacb334379be97803e`
- Priority: P2 — Core read-side snapshot consistency

## Confirmed defect

`SemanticSelectionInspector.Inspect(...)` built project/family indexes and then enumerated the caller-supplied `IEnumerable<string>` without checking `ProjectState.ChangeVersion`. A re-entrant/lazy enumerable could mutate the project while yielding selection ids, after the indexes were captured but before the inspection snapshot was assembled, allowing mixed-revision inspection state.

## Completed scope

- Source fix: `4658163352e18be52f0fbc3e53d2242571f3ec32`
- Focused Core smoke: `95d6bef531274a4c21c248aa36a04462d74b49a4`
- Plan: `docs/plans/2026-08-12-selection-inspector-input-freshness.md`

## Result

1. Inspection captures `ProjectState.ChangeVersion` before building its read indexes and materializing external selection ids.
2. If the project version changes during lazy selection enumeration, inspection fails before projecting selected elements/properties/quantities.
3. Focused smoke preserves normal deterministic two-element inspection and verifies a re-entrant enumerable that calls `project.Touch()` is rejected.
4. Moving-main ancestry was checked through `f1f8e8e2e647db4d67ea9da7703e3cbc289ec98f`; source and smoke remained ancestors and concurrent commits did not touch `SemanticSelectionInspector.cs` after the source fix.
5. Element/family/reference/property/quantity semantics and bulk-edit mutation behavior were otherwise unchanged.

## Validation boundary

Source/diff/ancestry and focused smoke source were verified remotely. GitHub Actions were not dispatched, and no licensed BricsCAD runtime PASS is claimed.
