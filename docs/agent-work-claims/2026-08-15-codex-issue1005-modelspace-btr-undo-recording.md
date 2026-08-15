# Work claim — issue #1005 ModelSpace BTR undo recording

- Status: `ACTIVE`
- Agent: `codex-issue1005-modelspace-btr-undo-20260815` (`/root/local004_postundo_marker`, delegated by `/root`)
- Registered: `2026-08-15T08:10:30+07:00`
- Baseline main SHA: `a7d68e265c60ba4869bea6ee42fa66bd87948153`
- Priority: issue `#1005` / `LOCAL-004 P0` production blocker, bounded by licensed discriminator evidence

## Reserved scope

Audit the exact installed BricsCAD V25 managed `DBObject` undo-recording API, then make one production correction: explicitly enable native undo recording on the already-open-for-write ModelSpace `BlockTableRecord` immediately before `SourceReconcileUndoCoordinator.PendingTransition.StageAfter` assigns the Source Reconcile revision XData.

The exact licensed discriminator on implementation SHA `eea8822bcb962dd01fc126d1fd046c5b67e07170` returned `post_undo_marker_class=AFTER` and `post_redo_marker_class=AFTER`, while generated entities participated in native Undo and the pre-Undo reconcile/history/marker state was otherwise coherent. Sanitized evidence is issue comment `#issuecomment-5299730339`. This isolates the correction to native enrollment of the existing ModelSpace BTR XData carrier.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs`
- `scripts/_guard_bases/source-reconcile-undo-coherence.py` only for the minimum focused ordering/API assertion
- `docs/agent-work-claims/2026-08-13-codex-issue1005-source-reconcile-undo.md` only for successor coordination/exact-SHA handoff
- this claim

## Excluded scope

- No changes to snapshots, semantic history entries, project/revision stamps, cache/backing-store identity, document/project affinity, observer events, command-intent/fallback state, restore/recovery behavior, generated invalidation, Source Reconcile service/command, persistence or lifecycle coordination
- No edits to any LOCAL-004 probe, runner, focused runtime gate, inbox, runbook, qualification document, result/evidence document or private/runtime surface
- No BricsCAD launch, customer/private data, GitHub Actions, CI workflow, release, installer, signing, V26, Curtain/issue `#987`, or broad Undo framework work

## Validation plan

- Inspect the installed V25 managed assemblies and record the exact callable undo-recording member/signature without copying proprietary binaries or metadata dumps into Git.
- Require the call on the existing `_modelSpace` object immediately before `_modelSpace.XData = marker` and inside the existing native transaction/staging boundary.
- Strengthen only the focused Source Reconcile Undo source guard needed to lock that adjacency and reject movement into snapshots/history/service/generated/event paths.
- Run the focused Source Reconcile gates, Core smoke/build, and installed-reference V25 `Release|x64` build with `0 warnings / 0 errors`; do not launch BricsCAD or dispatch Actions.

## Coordination

The active production claim `2026-08-13-codex-issue1005-source-reconcile-undo.md` remains authoritative for the full issue and is updated in the same claim-only reservation commit to delegate this exact continuation. The active root LOCAL-004 claim retains licensed execution, cleanup and result publication. No LOCAL-004 automation or evidence ownership transfers here.

## Completion condition

The minimal production call and focused guard are merged to a current-main descendant with an exact implementation SHA and source/build validation handoff. This bounded claim remains `ACTIVE` and issue `#1005` / LOCAL-004 remain `OPEN / PENDING_LOCAL` until `/root` reruns the unchanged licensed matrix on that exact descendant; source/static/build evidence alone does not close or promote the claim.
