# Work claim — Drawing identity mutation touch ordering

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:13:00+07:00`
- Completed: `2026-08-12T10:17:00+07:00`
- Baseline main SHA: `7c05929fee1f7f3b90bece29e27debfed0f9f189`
- Claim commit: `ed7a9fd816eaadcbe78253c41aa8d9f89fb14066`
- Source fix: `74b7c4e3c9fcc3def7d3f4f32436f887aa8eb6be`
- Regression gate: `d4cadaae9da056b8b32dd96da70181daea69346b`
- Priority: P1 — keep cached project drawing-identity synchronization fail-before-mutation when persistence version advancement cannot succeed.

## Confirmed defect

`ProjectContextCoordinator.SyncDrawingIdentity(...)` updated `ProjectState.DrawingPath` before `ProjectState.Touch()`, and `AdoptDrawingIdentity(...)` updated project/element drawing fingerprints before `Touch()`. `ProjectState.Touch()` intentionally computes `checked(ChangeVersion + 1L)` before advancing persistence state; QSDB accepts every non-negative `long`, including `long.MaxValue`. A cached project whose drawing path needed synchronization at `ChangeVersion == long.MaxValue` could therefore have `DrawingPath` changed and then throw `OverflowException`, leaving an in-memory mutation without a matching `ChangeVersion` increment. `AdoptDrawingIdentity(...)` could similarly mutate project identity before failing on a malformed null element because `ProjectState.Elements` is a public mutable list.

## Completed scope

- `SyncDrawingIdentity(...)` now preserves its same-path no-op and calls `project.Touch()` before changing `DrawingPath`.
- `AdoptDrawingIdentity(...)` snapshots the current element collection, rejects a null element before mutation, calls `project.Touch()`, and only then assigns project drawing identity and eligible element fingerprints from the validated snapshot.
- Fingerprint mismatch behavior, read-only validation, fingerprint acquisition/path fallback, successful adoption targeting, sidecar freshness and persistence-stamp behavior were not changed.
- `scripts/preflight-project-context-drawing-identity-touch-order.py` pins the fail-before-mutation ordering and the checked `ProjectState.Touch()` version-advance contract.

## Validation evidence

- Claim registration: `ed7a9fd816eaadcbe78253c41aa8d9f89fb14066`
- Source fix on `main`: `74b7c4e3c9fcc3def7d3f4f32436f887aa8eb6be`
- Focused regression gate on `main`: `d4cadaae9da056b8b32dd96da70181daea69346b`
- Post-integration source readback confirmed same-path no-op → `Touch()` → `DrawingPath` assignment in `SyncDrawingIdentity(...)`.
- Post-integration source readback confirmed element snapshot/null validation → `Touch()` → project/element identity assignments in `AdoptDrawingIdentity(...)`.
- Regression source was read back from `main` and pins both orderings plus `checked(ChangeVersion + 1L)` in `ProjectState.Touch()`.

## Validation boundary

GitHub Actions, executable Python/.NET smoke, full build and licensed BricsCAD V25/V26 runtime were not run in this hosted session, so no runtime/build PASS is claimed.

## Completion

Completed. Drawing-identity synchronization now fails before mutating cached project identity when persistence version advancement cannot succeed, and malformed null-element adoption is rejected before project identity changes. Reservation released.
