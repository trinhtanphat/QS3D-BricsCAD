# Work claim — Room Finish mutation/regeneration safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish`
- Registered: `2026-08-11T19:37:00+07:00`
- Baseline main SHA: `3319fba7bf1b0845539ea0aec25536ab61335496`
- Priority: continue the localized-mutation hardening lane after scoped manual host-link regeneration; prevent a local Room Finish authoring operation from consuming unrelated project dirty state or violating rollback/lifecycle boundaries.

## Reserved scope

Audit and, only where current source proves a defect, harden the `QS3DFINISH` / `SemanticCaptureService.GenerateRoomFinishes` mutation path so regeneration and commit/rollback behavior are limited to the semantic elements actually affected by Room Finish generation. Preserve canonical project binding and existing generated/native ownership contracts.

## Expected surfaces

- `QS3DFINISH` command surface in `src/QS3D.BricsCAD.V25/Commands.cs`
- the current implementation file containing `SemanticCaptureService.GenerateRoomFinishes`
- existing Room Finish / semantic-capture static preflight(s), or one focused preflight if no suitable contract exists
- this claim file for close-out status

## Excluded scope

- No Direct Draw/Create Similar work reserved by `2026-08-11-chatgpt-web-create-similar.md`.
- No Workspace multi-selection/policy work reserved by the active Workspace claim.
- No agent-registration protocol/bootstrap changes.
- No intentionally global `QS3DREGEN` / `QS3DREFRESH` behavior changes.
- No BricsCAD V25 runtime PASS, GitHub Actions dispatch, release, installer or signing work.

## Validation plan

- Inspect current `main` source to determine the exact Room Finish mutation set and regeneration behavior before changing code.
- If a source defect exists, add/update static preflight coverage that fails on full-project regeneration or broken lifecycle/rollback ordering for this lane.
- Compare the implementation commit against its base and re-fetch `main` plus active claims immediately before branch update.
- Keep any required live BricsCAD V25 selection/Undo/save-reopen qualification explicitly LOCAL_ONLY and unclaimed remotely.

## Coordination

Current neighboring active claims cover Create Similar and Workspace multi-policy plus registration protocol bootstrap; this reservation is limited to Room Finish semantic generation and does not edit those capabilities. If a later claim overlaps Room Finish before implementation begins, stop and re-scope rather than competing.

## Completion condition

Current Room Finish source is either proven already safe with no product-source commit, or a focused source/preflight fix is merged to current `main`; the exact audited outcome and any LOCAL_ONLY residual are recorded here and the claim is marked `COMPLETED` or `RELEASED` accordingly.
