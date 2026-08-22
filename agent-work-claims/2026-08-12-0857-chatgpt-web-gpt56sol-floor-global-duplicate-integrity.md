# Work claim — global Floor duplicate identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-global-duplicate-integrity-20260812-0857`
- Registered: `2026-08-12T08:57:00+07:00`
- Baseline main SHA: `057d9fd153190511322fd7339c5ea0406587b276`
- Priority: P2 — reject any Floor mutation when the Floor collection is globally identity-ambiguous.

## Confirmed defect

The earlier Floor Create fix blocked duplicate existing IDs only on Create. Other `ProjectFloorService` operations resolved a requested Floor through `ProjectState.FindFloor(id)`, whose uniqueness check only detects duplicates matching the requested ID. An unrelated duplicate pair such as `F1` + `f1` could therefore coexist while a mutation on unique `F2` still proceeded.

## Implemented fix

- Existing Floor ID uniqueness is centralized in `ValidateUniqueFloorIds(project)`.
- `Create(...)` uses the shared global identity guard.
- `FindRequired(...)` now runs the same global guard before lookup, covering Update, SetActive, Assign, vertical-level assignment, Delete and ReferenceCount paths.
- Case-insensitive canonical lookup, previous Create behavior, active-floor same-target alias semantics, finite/vertical validations and unrelated services remain unchanged.
- Focused smoke proves `SetActive("F2")` fails before mutation when unrelated `F1/f1` duplicates exist, while valid case-insensitive activation still advances one revision.

## Integration evidence

- Claim registration: `60b5a6ddfcb90c7331162f41f4722ff9b3bc4d50`.
- Branch source commit: `c6a13a683b5029c86e6f4d561faf43d3464f208f`.
- Focused smoke commit: `205ae17a89b29499b1a554ae7f1cc08b3e6e5e0f`.
- Branch diff was exactly `ProjectFloorService.cs` (+13/-4) plus the new 49-line smoke.
- PR `#670` was mergeable and squash-merged at `cd05005f3c0d6fd2abb381e1db822777f7631131`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.
