# Work claim — LOCAL-003 Level runner read-only DWG guard

- Status: `COMPLETED`
- Agent: `/root/fix_level_curtain_frame_z`
- Registered: `2026-08-14T16:51:28+07:00`
- Completed: `2026-08-15T07:49:10+07:00`
- Baseline main SHA: `87d9ef2e35d0baa2b99d4d19c820115e339c939a`
- Parent authorization: `/root`, bounded follow-up within the active LOCAL-003 claim

## Confirmed harness defect

The Level runtime probe intentionally commits native entities to the in-memory disposable drawing. The runner launches that drawing writable and relies on `QUIT`/`No` to prevent persistence. A read-only watcher captured a deterministic transient rewrite of the original DWG. The current restore-order runner copies its backup over the drawing before its after-hash check, so it proves recovery but can no longer prove that the opened disposable file was never written.

## Reserved scope

- `scripts/test-bricscad-v25-level-z.ps1`
- `scripts/preflight-level-z-runtime-probe.py`
- this claim file

The runner must preserve the disposable drawing's original file attributes, enforce and verify the OS read-only attribute before native launch, keep that attribute through host exit, and compare the drawing hash before any restoration. Cleanup must restore the original bytes and attributes idempotently from `finally` on every path.

## Excluded scope

- No production probe, builder, Core, adapter, marker-schema, documentation, private-data, release, or GitHub Actions change.
- Do not launch or interact with BricsCAD.
- LOCAL-003 remains `PENDING_LOCAL` until `/root` completes a fresh licensed exact-SHA rerun.

## Validation plan

- PowerShell parser validation for the runner.
- Focused `preflight-level-z-runtime-probe.py` and related Level static gates.
- Appropriate Core/installed-reference compile only where available without licensed runtime.
- Final diff/readback and a normal implementation PR/merge.

## Completion condition

A merged runner-only guard prevents persistence to the disposable DWG instead of masking it with pre-verification restoration, static coverage locks the ordering and attribute lifecycle, and an exact merged SHA is handed to `/root` for licensed rerun.

## Completion record

- Implementation commit: `ff736bf2882215b9709bb93018d912d4a4e52832`.
- Implementation PR: `#1420`; merged to `main` as `b49834f960965b75fb6a4ed20a48829ed8ededfc`.
- The runner now snapshots the disposable DWG's exact original file attributes, creates its backup, sets and verifies the OS read-only bit before launch, retains/verifies that bit through owned-host exit, and compares the pre-restore SHA-256 before the only restoration call.
- Idempotent cleanup clears read-only only while copying the backup, removes the consumed backup, restores the exact original attributes from an inner `finally`, verifies restored bytes/attributes, and restores the private process environment from the outer cleanup path.
- The focused preflight locks original-attribute capture, backup-before-guard ordering, pre-launch verification, host-exit verification, hash-before-restore ordering, one restoration call under `finally`, backup consumption and exact attribute restoration.
- Validation passed on synchronized implementation source: Windows PowerShell 5.1 parser; all nine focused Level/Beam static gates; `QS3D.Core`, `QS3D.Core.SmokeTests`, and installed-reference `QS3D.BricsCAD.V25` `Release|x64` builds, each with zero warnings and zero errors.
- No BricsCAD process, GitHub Actions workflow, private DWG/data, production probe, builder, Core, adapter or marker-schema surface was used or changed.

This bounded harness reservation is complete and released. The parent LOCAL-003 claim remains `ACTIVE`, and LOCAL-003 remains `PENDING_LOCAL`; `/root` still must rebuild and execute the licensed disposable Level probe against the final exact merged `main` SHA before recording any new runtime result.
