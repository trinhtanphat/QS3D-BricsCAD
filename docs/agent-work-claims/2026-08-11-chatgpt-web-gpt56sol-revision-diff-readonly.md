# Agent work claim — Revision Diff read-only integrity

Status: ACTIVE

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Registered: 2026-08-11T21:45:12+07:00
Baseline main SHA observed before reservation: `5cf79385c52e2af3e74c19f8e67043898b2c2b29`

## Scope

Make `QS3DREVDIFF` a true read-only review command. Preserve its existing non-creating detached snapshot lifecycle, revision comparison UI and locate behavior while removing persisted compare telemetry that binds a mutation context and advances project state merely because a user viewed a diff.

Expected implementation surfaces:

- `src/QS3D.BricsCAD.V25/ReviewCommands.cs`
- `scripts/preflight-review-snapshot-project-lifecycle.py`
- this claim file for completion status

## Concrete defect

`QS3DREVDIFF` starts with `ProjectContextCoordinator.TryGetReadOnly`, captures the current revision through the detached/read-only revision coordinator path and opens a modeless review window. It then calls `TryRecordRevisionCompare`, which resolves `ExistingProjectMutationContext` and writes `AuditTrail.ForProject(project).Record("revision.compare", ...)`. `AuditTrail.Record` touches the canonical `ProjectState`, so simply opening Revision Diff can make the project dirty and advance `ChangeVersion`.

The existing lifecycle preflight describes Revision Diff as using read-only/detached snapshots but also requires `TryRecordRevisionCompare`, leaving this mutation hole explicitly unguarded.

## Intended contract

- `QS3DREVBASE` remains a deliberate persisted mutation and may keep its baseline audit event.
- Recognition apply/skip paths remain deliberate mutations and retain their audit behavior.
- `QS3DREVDIFF` may read an existing project, load/capture detached snapshots, compare quantities, open the review UI and locate current elements, but must not bind a mutation context, append an audit record, call `Touch`, save project state or otherwise dirty the canonical project just for viewing a diff.
- Existing no-project blocking and stale/modeless locate guards remain unchanged.

## Exclusions

- No revision snapshot schema, comparison semantics or baseline persistence changes.
- No recognition workflow changes.
- No modeless window ownership/locate behavior changes.
- No Core `AuditTrail` behavior changes.
- No BricsCAD V25 runtime, private-DWG, installer/signing, updater or release qualification work.
- No GitHub Actions dispatch.

## Validation plan

- Re-fetch latest `main`, `ReviewCommands.cs` and the focused lifecycle preflight immediately before writes.
- Remove only Revision Diff compare-audit mutation while retaining baseline/recognition audit paths.
- Harden the preflight to isolate the `QS3DREVDIFF` command and reject mutation-context/audit/touch/save surfaces in that review method.
- Re-read pushed source/preflight from remote `main` and inspect commit status/workflow evidence without claiming unexecuted runtime/build validation.
