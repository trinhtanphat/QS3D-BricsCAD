# Work claim — Drawing identity mutation touch ordering

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:13:00+07:00`
- Baseline main SHA: `7c05929fee1f7f3b90bece29e27debfed0f9f189`
- Priority: P1 — keep cached project drawing-identity synchronization fail-before-mutation when persistence version advancement cannot succeed.

## Confirmed defect

`ProjectContextCoordinator.SyncDrawingIdentity(...)` updates `ProjectState.DrawingPath` before `ProjectState.Touch()`, and `AdoptDrawingIdentity(...)` updates project/element drawing fingerprints before `Touch()`. `ProjectState.Touch()` intentionally computes `checked(ChangeVersion + 1L)` before advancing persistence state; QSDB accepts every non-negative `long`, including `long.MaxValue`. A cached project whose drawing path needs synchronization at `ChangeVersion == long.MaxValue` can therefore have `DrawingPath` changed and then throw `OverflowException`, leaving an in-memory mutation without a matching `ChangeVersion` increment. `AdoptDrawingIdentity(...)` can similarly mutate project identity before failing on a malformed null element because `ProjectState.Elements` is a public mutable list.

## Reserved scope

- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs` drawing-identity synchronization helpers only
- one focused source regression/preflight for mutation ordering and malformed-element prevalidation
- this claim file

## Intended contract

- Validate/capture adoption element targets before any drawing-identity mutation.
- Call `ProjectState.Touch()` before assigning `DrawingPath`, `DrawingFingerprint`, or adopted element fingerprints, so checked version overflow fails without partial identity mutation.
- Preserve fingerprint mismatch behavior, read-only validation, path fallback, successful identity adoption semantics, element targeting rules, sidecar freshness and persistence-stamp behavior.
- Do not change BricsCAD fingerprint acquisition, QSDB schema/persistence format, unrelated mutation services or concurrent template/quantity lanes.

## Validation boundary

Source and focused preflight will be read back from `main`. GitHub Actions, executable Python/.NET smoke, full build and licensed BricsCAD V25/V26 runtime PASS are not claimed unless actually executed.
