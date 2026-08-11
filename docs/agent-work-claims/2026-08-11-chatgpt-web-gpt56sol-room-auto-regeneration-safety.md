# Work claim — Room Auto regeneration safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-auto-regen`
- Registered: `2026-08-11T19:51:00+07:00`
- Baseline main SHA: `a6c133cbada6013c45ac55805c9cfa2897d4cc30`
- Priority: continue localized semantic-mutation hardening after Room Finish; prevent `QS3DROOMAUTO` from consuming unrelated dirty project state while preserving discovery, provenance, stale-room and generated-finish contracts.

## Reserved scope

Audit and, only where current source proves a defect, harden regeneration scope in `QS3DROOMAUTO` / `RoomBoundaryCommands.DiscoverRooms`. Determine the exact semantic mutation set created, updated or marked stale by one Room Auto run, and ensure regeneration is limited to that affected set rather than unrelated project dirty elements.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs`
- `scripts/preflight-room-auto-project-lifecycle.py`
- an existing focused Room Auto regeneration preflight, or one focused static preflight if no suitable guard exists
- this claim file for close-out

## Excluded scope

- No generated-native source recognition / eligibility work reserved by the active generated-source-recognition claim.
- No Core mutation atomicity / transaction primitive changes reserved by the active Core mutation-atomicity claim.
- No Direct Draw/Create Similar, Workspace, Material Catalog/refresh, modeless viewer, Level Z-chain or registration-protocol work.
- No intentionally global `QS3DREGEN` / `QS3DREFRESH` behavior changes.
- No BricsCAD V25 runtime PASS, GitHub Actions dispatch, release, installer, signing or private-DWG qualification.

## Validation plan

- Re-fetch current `main` and active claims before any implementation write.
- Read current `DiscoverRooms` and its helper/service calls to identify affected Room/RoomFinish IDs, stale-auto-room handling and current regeneration calls.
- If full-project regeneration exists on a localized run, replace only that regeneration boundary with an affected-ID path while preserving canonical binding, snapshot/rollback and audit semantics; do not redesign Core atomicity.
- Add/update focused static regression coverage that rejects full-project regeneration in this command if a source defect is confirmed.
- Inspect exact pushed diff and ancestry; leave native V25 proof to existing LOCAL_ONLY qualification.

## Coordination

This lane owns only Room Auto command-side regeneration scope. Recognition input ownership remains with the generated-source-recognition claim; transaction primitives remain with Core mutation-atomicity. If a later active claim overlaps `RoomBoundaryCommands.DiscoverRooms`, stop and re-scope rather than compete.

## Completion condition

Current Room Auto regeneration is either proven already scoped with a focused regression guard, or a minimal command/preflight fix is merged to current `main`; the audited result is recorded here and this claim is marked `COMPLETED` or `RELEASED`.
