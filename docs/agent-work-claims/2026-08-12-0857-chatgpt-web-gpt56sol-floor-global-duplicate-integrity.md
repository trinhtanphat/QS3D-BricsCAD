# Work claim — global Floor duplicate identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-global-duplicate-integrity-20260812-0857`
- Registered: `2026-08-12T08:57:00+07:00`
- Baseline main SHA: `057d9fd153190511322fd7339c5ea0406587b276`
- Priority: P2 — reject any Floor mutation when the Floor collection is globally identity-ambiguous.

## Confirmed defect

The previous Floor Create fix blocks duplicate existing IDs before Create, but other `ProjectFloorService` operations resolve a requested Floor through `ProjectState.FindFloor(id)`. `FindUnique` only detects duplicates matching the requested ID. If the collection contains unrelated duplicate IDs such as `F1` + `f1`, an operation on unique `F2` can still proceed and mutate an already-invalid project. `ProjectZoneService` has just been hardened with a global identity preflight for the same reason.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorGlobalDuplicateIntegritySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Factor the existing Create duplicate scan into `ValidateUniqueFloorIds(project)`.
- Call that preflight from both Create and `FindRequired`, so Update/SetActive/Assign/vertical-level/Delete/ReferenceCount paths fail before mutation when any Floor identity is duplicated globally.
- Preserve case-insensitive canonical lookup, previous Create behavior, active-floor alias no-op semantics, finite/vertical validations and all unrelated services.
- Add focused smoke proving an operation on unique `F2` does not mutate when unrelated `F1/f1` duplicates exist, while a valid project still updates normally.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
