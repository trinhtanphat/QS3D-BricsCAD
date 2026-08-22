# Work claim — Structural wall null element integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:55:00+07:00`
- Completed: `2026-08-12T09:58:00+07:00`
- Baseline main SHA: `eea7eefdb45e7548be7b1abdd06d7a690ac0dbf5`
- Claim commit: `5d3cf9d828fbeb5914098b0886dfe2305996b39c`
- Source commit on branch: `954689984efa33838eb8e3ccc6b0858f2294ddca`
- Regression-source commit on branch: `e190ba813857a322c884269afcf9cf6c699481e3`
- Pull request: `#729`
- Squash merge commit: `265ea76adfd28081ca0f434a1ed0cee3d8112f9a`
- Priority: evidence-driven Core malformed-state execution integrity

## Confirmed defect

`ProjectState.FindElement(...)` explicitly fails closed when `ProjectState.Elements` contains a null semantic element, while `StructuralRegenerator.LinkedOpeningArea(...)` previously dereferenced every entry through `child.Category`. A malformed project containing an unrelated null element could therefore surface an accidental `NullReferenceException` during structural-wall regeneration instead of an explicit malformed-project `InvalidOperationException`.

## Implemented

- `LinkedOpeningArea(...)` now rejects null semantic elements before dereference.
- Rejection occurs before structural-wall `SetQuantity(...)` calls, so pre-existing quantities are not partially overwritten.
- Canonical/missing/empty `HostWallId` behavior from PR #721 is preserved.
- Canonical case-insensitive linked-opening matching and quantity formulas are unchanged.

## Regression source

`StructuralWallNullElementIntegritySmoke` covers explicit null-element rejection with quantity atomicity plus valid canonical linked-opening deduction.

## Changed surfaces

- `src/QS3D.Core/Services/StructuralRegenerator.cs`
- `tests/QS3D.Core.SmokeTests/StructuralWallNullElementIntegritySmoke.cs`
- this claim file

## Integration evidence

- Before PR creation, moving `main` had advanced 22 commits, but `StructuralRegenerator.cs` still had the exact pre-patch blob SHA `8786eb9d759cefe46fc68aeeadcc71937630a798`; no concurrent source overlap was present.
- PR `#729` was squash-merged with expected head SHA `e190ba813857a322c884269afcf9cf6c699481e3` into `265ea76adfd28081ca0f434a1ed0cee3d8112f9a`.
- Source and regression were read back directly from `main` after merge.

## Validation boundary

Remote/static source + regression review only. No GitHub Actions/build/release was dispatched, smoke source was not executed in this web session, and no BricsCAD V25/V26 or local .NET runtime PASS is claimed.
