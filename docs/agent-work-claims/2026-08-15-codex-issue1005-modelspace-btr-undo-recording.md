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

## Exact implementation handoff

- Implementation commit `86afdf93b58169ef8ce755de459a3e4beedbbe29` is merged on `origin/main`.
- Installed `TD_Mgd.dll` identifies V25 managed assembly version `25.9.0.0`. Reflection confirmed public instance signature `Void DisableUndoRecording(Boolean disable)` on `Teigha.DatabaseServices.DBObject`; the related `AssertWriteEnabled(Boolean autoUndo, Boolean recordModified)` member is protected and was not used.
- `PendingTransition.StageAfter` now calls `_modelSpace.DisableUndoRecording(false);` exactly once, immediately before `_modelSpace.XData = marker;`. The object is the existing ModelSpace BTR already opened `ForWrite` by `BeginTransition`; no additional object, transaction, history or service path was introduced.
- The focused guard requires exact adjacency, requires the call exactly once, and rejects moving it into `BeginTransition`. All six focused Source Reconcile gates passed.
- Core `Release` build passed with `0 warnings / 0 errors`; Core smoke reported `ALL PASS`. Installed-reference V25 `Release|x64` adapter/Core build passed with `0 warnings / 0 errors`, and both ProductVersion values end in `+86afdf93b58169ef8ce755de459a3e4beedbbe29`.
- No BricsCAD process, private data or GitHub Actions were used. This claim remains `ACTIVE`; issue `#1005` and LOCAL-004 remain `OPEN / PENDING_LOCAL` for `/root` to rerun the unchanged licensed discriminator/matrix on an exact clean descendant.

## Read-before-upgrade continuation

- Exact licensed rerun evidence for implementation `86afdf93b58169ef8ce755de459a3e4beedbbe29` is issue comment `#issuecomment-5299890294`. The late `DisableUndoRecording(false)` call did not change the result: post-Undo and post-Redo marker classes were both `AFTER`, semantic Undo remained false and Redo true, generated entities were removed by native Undo, and all pre-Undo history/project/revision plus cold-reopen and cleanup checks passed.
- Baseline `main` for this continuation is `96862d6cdfddd8bb2ea4a0055505005859467ea7`. The failed candidate enables recording only after `BeginTransition` has already opened the ModelSpace BTR `ForWrite`. The installed V25 API exposes `DBObject.DisableUndoRecording(Boolean)` and the paired public `UpgradeOpen()` transition; undo recording is disabled by default, so the exact negative result proves the late toggle does not retroactively enroll that already-open-for-write object.
- The single bounded production correction reserved by this claim is to open that same ModelSpace BTR `ForRead` while `BeginTransition` reads the prior marker, then in `StageAfter`, after the existing private-history staging and registration-app preparation, execute exactly `DisableUndoRecording(false)`, `UpgradeOpen()`, and the existing XData assignment in that order. No database-level undo record or alternate carrier is introduced.
- Owned implementation surfaces remain only `src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs` and the minimum focused assertion in `scripts/_guard_bases/source-reconcile-undo-coherence.py`. Every semantic snapshot/history/project/revision/stamp/cache/backing-store/affinity/event/generated/service guard and every LOCAL-004/private/BricsCAD/Actions surface remain excluded and unchanged.
- This claim and issue `#1005` stay `ACTIVE / OPEN / PENDING_LOCAL`; source/build evidence will provide an exact-SHA handoff for `/root` to rerun the unchanged licensed matrix and will not infer runtime success.

## Read-before-upgrade candidate

- Implementation commit `0542131348e6393a4d28d6e0945ec60c2ee3bff6` is merged on `origin/main`. `BeginTransition` now keeps the ModelSpace BTR `ForRead` while reading its previous revision. `StageAfter` performs the existing private history staging and registration-app preparation before the exact `DisableUndoRecording(false)` -> `UpgradeOpen()` -> revision-XData sequence.
- The focused coherence guard requires that exact read/open/write lifecycle and single staged use. No database-level undo record, alternate marker carrier, semantic snapshot/history/project/revision/stamp/cache/backing-store/affinity/event/generated/service change, or LOCAL-004/private/Actions surface was introduced.
- All six focused Source Reconcile gates passed. Core `Release` build passed with `0 warnings / 0 errors`; Core smoke reported `ALL PASS`. Installed-reference V25 `Release|x64` adapter/Core build passed with `0 warnings / 0 errors`, and both ProductVersion values end in `+0542131348e6393a4d28d6e0945ec60c2ee3bff6`.
- No BricsCAD process, private data or GitHub Actions were used. This claim remains `ACTIVE`; issue `#1005` and LOCAL-004 remain `OPEN / PENDING_LOCAL` for `/root` to rerun the unchanged licensed discriminator/matrix on the exact implementation SHA. Source/static/build evidence does not infer runtime success.
