# Work claim — Room Finish mutation/regeneration safety

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish`
- Registered: `2026-08-11T19:37:00+07:00`
- Baseline main SHA: `3319fba7bf1b0845539ea0aec25536ab61335496`
- Completed: `2026-08-11T19:48:00+07:00`
- Result commit: `49bcaf114a200c70cab641fce86b78d8004dda71`
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

## Validation result

Current `SemanticCaptureService.GenerateRoomFinishes` was already safe in product source: it acquires current-selection snapshots, filters Rooms by selected semantic reference handles, synchronizes only the matching Room Finish elements, and invokes `Regenerate(project, finish)` per synchronized finish. The audited method contains no full-project `RegenerateDirty(...)` or `RegenerateProject(...)` path, so unrelated pre-existing dirty semantic elements are not consumed by this localized authoring operation.

Commit `49bcaf114a200c70cab641fce86b78d8004dda71` strengthens `scripts/preflight-room-finish-project-lifecycle.py` to require the element-scoped regeneration call and reject future full-project regeneration inside `GenerateRoomFinishes`. Existing selection-before-bind, canonical-existing-project, rollback snapshot, and synchronization guards remain in place.

No product-source change was required. No GitHub Actions, C# build, BricsCAD V25 `NETLOAD`, private-DWG execution, Undo, or save/reopen runtime qualification was run or claimed in this lane; existing LOCAL-001 remains the owner of native V25 proof.

## Coordination

The lane remained outside Direct Draw/Create Similar, Workspace, material refresh, modeless viewer, and Core mutation-atomicity claims. No neighboring agent claim was edited.

## Completion condition

Satisfied: current Room Finish source is proven selection-scoped and element-regenerated, its static lifecycle preflight now locks that invariant, and no remote-only claim is made for native V25 runtime behavior.
