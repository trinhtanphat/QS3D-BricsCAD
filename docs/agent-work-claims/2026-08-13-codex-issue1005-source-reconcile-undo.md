# Work claim — Source Reconcile native Undo/Redo semantic coherence

- Status: `ACTIVE`
- Agent: `codex-issue1005-source-reconcile-desync-20260813` (`/root/fix_source_reconcile_desync`; coordinated successor to `/root/fix_source_reconcile_undo`)
- Registered: `2026-08-13T15:33:32+07:00`
- Baseline main SHA: `f926c2dcbe9a850d8be2425e940c4d4c929d324f`
- Priority: GitHub issue `#1005` / `LOCAL-004 P0` production blocker reproduced on licensed BricsCAD V25

## Reserved scope

Fix the production Source Reconcile command boundary so native Undo/Redo restores the canonical in-memory `ProjectState` snapshot corresponding to the CAD state committed by `QS3DSYNCSOURCE`. The integration must remain document/project bound, preserve the existing one-CAD-transaction invalidation and pre-commit `ProjectStateSnapshot` failure rollback, and fail closed rather than applying a semantic snapshot to a replaced project or another DWG.

The implementation will use a native transaction-bound revision marker plus an in-session, document-scoped semantic snapshot history. Native Undo/Redo command completion will observe the marker restored by BricsCAD and restore only the matching snapshot for the same cached canonical project. Lifecycle teardown/reload/forget will discard stale history.

This claim was reactivated after the exact-SHA V25 rerun exposed a second production defect at the failed-command boundary. The correction also owns transition state around a reconcile that writes/stages its native marker and then fails (including unit-policy refusal), so command completion cannot advance, restore, or rebase semantic history from an uncommitted transition before a later successful reconcile.

The latest exact-SHA rerun still reaches `history=DESYNCHRONIZED` before the runner issues its first explicit native Undo. This successor pass owns the bounded event-intent contract: an Undo/Redo completion may inspect or poison Source Reconcile history only when it matches a prior active-document native Undo/Redo start for the same document. Cancelled, failed, inactive-document, unmatched, duplicated and stale internal command events must clear or ignore intent without weakening the existing fail-closed response to an unknown revision reached by a genuinely matched native Undo/Redo.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs`
- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs`
- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs` only for exact cached-project identity/lifecycle coordination
- new focused Source Reconcile semantic/native Undo coordinator under `src/QS3D.BricsCAD.V25/`
- focused deterministic/static regression coverage under `scripts/` and, where CAD-independent seams permit, `tests/`
- `docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md`, `docs/LOCAL-AGENT-INBOX.md`, and this claim for the changed exact-SHA rerun contract/close-out

## Excluded scope

- No edits to the additive LOCAL-004 runtime probe/runner (`SourceReconcileRuntimeProbeCommands.cs`, `test-bricscad-v25-source-reconcile.ps1`, or its gate) unless a changed production contract makes a narrowly coordinated assertion update necessary.
- No local BricsCAD execution, private/customer DWGs, GitHub Actions, release, installer, signing, V26, or CI workflow work.
- No Source Reconcile geometry, dependency-closure, quantity, invalidation ownership, unit-policy, builder, persistence-format, or ordinary command-failure rollback redesign.
- No Curtain command integration or issue `#987`; any reusable coordinator seam remains unhooked there under this claim.
- No broad global Undo framework for unrelated QS3D mutations.

## Validation plan

- Add deterministic source/static coverage proving marker mutation occurs inside the same native transaction as Source Reconcile CAD changes, semantic before/after snapshots register only after successful commit, and failure rollback remains unchanged.
- Cover first reconcile, consecutive reconciles, Undo/Redo marker transitions, branch-after-Undo behavior, stale/replaced-project refusal, per-document isolation, lifecycle cleanup, and no implicit project load/create from Undo handling through a CAD-independent state-machine seam where practical.
- Run focused/new preflights, existing Source Reconcile preflights, strict manual-CI policy gate, available managed build/tests, and V25 adapter compile against installed references only if possible; do not run BricsCAD.
- Request the existing guarded LOCAL-004 runner be rerun on the exact merged SHA by the local owner.

## Coordination

The ACTIVE LOCAL-004 claim `2026-08-13-codex-local004-source-reconcile-runtime.md` explicitly excludes production Source Reconcile/Undo integration and hands production defects to a remote source-fix lane. Its additive probe/runner remain owned by `codex-local-root-20260813`. Open PR `#975` is UI-only. Issue `#987` is the analogous Curtain blocker and is intentionally not implemented by this lane.

Parent lane `/root` explicitly delegated this successor pass after `/root/fix_source_reconcile_undo` moved to the separate Curtain `#987` work. The successor reserves only `SourceReconcileUndoCoordinator.cs` and deterministic Source Reconcile Undo preflight/model coverage for the event-intent correction; it will not edit the LOCAL-004 probe, runner, qualification docs or inbox.

## Completion condition

The corrected production fix and regressions are merged to current `main`, the exact fixed-SHA LOCAL-004 V25 runner passes the full reconcile/failure/refusal/multi-DWG/Undo/Redo sequence, issue `#1005` is closed, and this claim is `COMPLETED` with exact SHAs and executed validation. Source/static/build evidence alone must not promote or close this claim again.

## Completion record

- Claim-only commit: `c4a9649a0a58a37dcfb27755958f7a2e60fb08fc`; claim PR `#1006`; claim merge SHA `bf1b4e3ac39b2880080eb0b6579afaf15b1c969d`.
- Implementation commit: `685bbca98bf296946cbaabbe7252851d2582daff`; source PR `#1007`; source merge SHA `1c957ae2dc022db8cafbfcf0de91d9d47a53e68f`.
- Production issue `#1005` closed only after source PR `#1007` merged. The closing comment requests the guarded LOCAL-004 V25 rerun on exact main SHA `1c957ae2dc022db8cafbfcf0de91d9d47a53e68f`.
- Executed locally without GitHub Actions or BricsCAD runtime: installed-reference V25 `Release|x64` adapter build PASS with `0 warnings / 0 errors`; Core smoke executable `ALL PASS`; strict manual-CI and generic preflight PASS; all `731` auto-discovered feature preflights PASS.
- The additive LOCAL-004 probe/runner/gate remained unchanged. Licensed native Undo/Redo, rollback, multi-DWG and cold-reopen evidence remains `PENDING_LOCAL`; no source/static/build result is promoted to `LOCAL_PASS`.

## Reactivation record

- Exact candidate `1c957ae2dc022db8cafbfcf0de91d9d47a53e68f` failed on licensed BricsCAD `V25.2.10 x64` after the second reported-successful `QS3DSYNCSOURCE` and before native Undo: `failure_phase=verify_final_reconcile`, `failure_code=SEMANTIC_SOURCE_MISMATCH`.
- The first reconcile/rebuild passed; the guarded sequence then exercised an intentional `INSUNITS` reconcile failure, generated/ambiguous refusal paths, and a document-B switch before the final reconcile. Cleanup evidence remained all true.
- Issue `#1005` was reopened and must remain open until the corrected exact main SHA passes the existing guarded runner. This remote lane will audit the production pending-transition/command-ended/rebase boundary and add deterministic/static regression coverage without changing the additive LOCAL-004 probe or runner.

## Latest candidate record

- The remaining false desync was bounded to the ended-only native command observer: the exact local failure reported sticky `DESYNCHRONIZED` history before its first explicit runner Undo, while production accepted any terminal event named `UNDO`, `REDO` or `MREDO` as restoration authority.
- Successor claim PR `#1032` merged as `ddd5beddd02549b6676f004af492ca6668961058`. Source commit `161b1a8a1448e937f1bdb34c4921ed2b63056ef2` and source PR `#1039` merged as exact rerun candidate `b48503307c28ae8abbc5e324e53c581915f51a23`.
- The correction requires a same-command WillStart/Ended pair while the exact tracked document remains active; cancellation, failure, mismatch, duplicate starts, project forget and detach clear intent. A matched native Undo/Redo reaching an unknown revision remains sticky and fail-closed.
- Remote validation without BricsCAD, private data or Actions: focused Source Reconcile coherence/reconcile/single-bind/runtime-probe gates PASS; strict manual-CI and generic preflight PASS; Core smoke executable `ALL PASS`; installed-reference V25 `Release|x64` build PASS with `0 warnings / 0 errors`.
- The LOCAL-004 probe, runner and qualification/inbox docs remain unchanged. Issue `#1005` remains `OPEN`; exact-SHA rerun request is recorded in issue comment `#issuecomment-5279201302`, and claim status remains `ACTIVE` pending the licensed result.

## State-classification successor pass

- Exact candidate `b48503307c28ae8abbc5e324e53c581915f51a23` still produced the same pre-final `DESYNCHRONIZED` / `MULTIPLE` tuple before the runner's first explicit Undo. Active-document WillStart/Ended pairing therefore does not distinguish every internal BricsCAD command pair.
- This successor pass reserves the coordinator's sticky state classification and deterministic gate. Read-only observer refusals, plus a failed semantic restore followed by successful rollback, will leave `CurrentRevision` unchanged rather than poison history; the live native marker mismatch continues to block all mutation fail-closed until the marker returns to that current revision.
- Sticky desynchronization remains reserved for the only semantically uncertain state: both target restore and recovery rollback fail. Intent pairing remains as defense-in-depth. LOCAL-004 probe/runner/inbox/qualification surfaces, BricsCAD runtime, private data and Actions remain excluded.

## State-classification candidate record

- Claim refinement PR `#1043` merged as `559c5f2ea955f839e502f5f8b9f527a4275649b3`. Implementation commit `025de3d505c79aa0ae1f06b2d348e96964860a6b` and source PR `#1045` merged as exact rerun candidate `9017a49a7d595a8828e5a2b8f1b42d1515884f1c`.
- Deterministic coverage proves read-only refusals and successfully recovered restore failures preserve the canonical project and `CurrentRevision`; persistent marker mismatch blocks transitions, marker return permits safe retry/rebase, and combined restore/recovery failure remains sticky.
- Focused Source Reconcile gates, strict manual-CI and generic preflight PASS; installed-reference V25 `Release|x64` build PASS with `0 warnings / 0 errors`. Full Core smoke is blocked on unchanged main by separately owned `WorkspaceCurtainOwnerSelectionSmoke` `HANDLE:00D2`; this lane did not modify that surface.
- Exact-SHA rerun request is issue comment `#issuecomment-5279358976`. Issue `#1005` and this claim remain `OPEN` / `ACTIVE` / `PENDING_LOCAL`; no runtime result is promoted.

## Sanitized-cause and nested-command successor pass

- Exact candidate `f42171d3f9dab336fa8874547a3016271977546d` still failed on licensed BricsCAD `V25.2.10 x64` at `verify_final_reconcile` / `SEMANTIC_SOURCE_MISMATCH`. The sanitized tuple remained `BOTH_SOURCES`, owner `NONE`, generated `RETAINED_ALL`, canonical project/revision/native marker `UNCHANGED`, history `DESYNCHRONIZED` before and after, and entry class `MULTIPLE`; cleanup remained all true. The runner had not yet issued its first deliberate native Undo.
- Baseline `main` for this pass is `53e467f29731a79a2452c3633860b02a295ca257`. The pass reserves only `src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs`, `scripts/_guard_bases/source-reconcile-undo-coherence.py`, and this claim. The LOCAL-004 probe/runner/gate, inbox/qualification docs, private data, BricsCAD execution and GitHub Actions remain excluded.
- Before changing behavior, the coordinator will classify every desynchronized observation through a strict sanitized allowlist that contains no document, project, revision or marker values. Deterministic coverage will distinguish lost/replaced committed history, failed restore recovery, and live history-affinity/cache mismatches instead of collapsing them into an unproved sticky-flag diagnosis.
- Source audit will then admit a behavior change only for a proven bounded false-positive path. In particular, native Undo/Redo intent must be a top-level user command for the tracked active document; a same-name command pair nested under another registered document command must not inspect, restore or poison Source Reconcile history. Genuine top-level Undo/Redo marker mismatch and failed semantic recovery remain fail-closed and sticky as currently required.

## Nested-command candidate record

- Source commit `caf443af204e97f9877e3566ea5018583d1eea85` merged through PR `#1122` as exact licensed-rerun candidate `032cb6e3923772cf40755fb62fe891ea2a239010`.
- The coordinator now tracks all attached-document command depth, arms Undo/Redo only at a globally top-level active-document boundary, and retains suppression when a document detaches with an unfinished outer command until the next ordinary stable command. This rejects the document-B-close/internal-A-Undo path without weakening genuine top-level marker/revision or combined restore/recovery fail-closed behavior.
- Desynchronized state now carries a strict sanitized cause allowlist (`COMMIT_HISTORY_LOST`, `RESTORE_RECOVERY_FAILED`, `HISTORY_AFFINITY_MISMATCH`, `CACHE_PROJECT_MISMATCH`, or `NONE`) without exposing document/project identifiers, revisions, marker values, paths, handles, entry counts or semantic values.
- Focused Source Reconcile gates, strict manual-CI and generic preflight PASS; Core smoke `ALL PASS`; installed-reference V25 `Release|x64` build PASS with `0 warnings / 0 errors`. The aggregate discovered `785` gates but was blocked only by two current-main Curtain frame method-isolation gates outside this lane.
- Sanitized rerun request is issue comment `#issuecomment-5289750420`. Issue `#1005` and this claim remain `OPEN` / `ACTIVE` / `PENDING_LOCAL`; the LOCAL-004 probe/runner/qualification docs, private data, BricsCAD runtime and GitHub Actions remain untouched.

## Sanitized-cause projection continuation

- Exact source candidate `032cb6e3923772cf40755fb62fe891ea2a239010` failed unchanged on licensed BricsCAD V25; issue evidence is `#issuecomment-5289769355`. The global command-depth and detach suppression therefore did not intercept the event or state that precedes the final reconcile.
- Baseline `main` for this continuation is `8480fdfb8bfb26bb5195a07e179579f3c6dbff52`. It reserves only `SourceReconcileUndoCoordinator.cs`, the Source Reconcile coherence guard base, and this claim. Production reconcile/restore behavior and every LOCAL-004 probe/runner/gate/qualification surface remain unchanged.
- The source-only sanitized snapshot will project persistent cause-bearing history as `DESYNCHRONIZED`, while document/project affinity or cache mismatch is projected as `NONE` because no matching history exists for the supplied canonical pair. Both values already belong to the unchanged runner allowlist. This makes the next exact run distinguish a true sticky setter from an observational affinity mismatch without exposing or adding IDs, revisions, marker values, paths, handles, entry counts or semantic values.
- This pass is diagnostic only. It must not clear sticky history, rebind project/document identity, weaken unknown-marker/revision refusal, or infer runtime correctness. Issue `#1005` remains open pending the new exact evidence.

## Sanitized-cause projection candidate

- Diagnostic source commit `07669e9a38c35e04951daa878d60582f828c45e3` merged through PR `#1130` as exact rerun candidate `1c90ddd7ba2cfe2cd56279b09f8feb2365e24ea7`.
- On that candidate, `DESYNCHRONIZED` uniquely projects the live `RESTORE_RECOVERY_FAILED` cause. `NONE` with the unchanged actual entry class projects commit-history loss or document/project affinity/cache mismatch, proving that the live restore/recovery setter did not cause the observed coarse failure.
- Focused Source Reconcile gates, strict manual-CI and generic preflight PASS; Core smoke `ALL PASS`; installed-reference V25 `Release|x64` build PASS with `0 warnings / 0 errors`.
- Exact rerun request and interpretation are recorded in issue comment `#issuecomment-5289798352`. Issue `#1005` and this claim remain `OPEN` / `ACTIVE` / `PENDING_LOCAL`; no runtime result is inferred.

## Native-drawing affinity continuation

- Exact diagnostic candidate `1c90ddd7ba2cfe2cd56279b09f8feb2365e24ea7` returned `NONE` before and after with `MULTIPLE` entries; issue evidence is `#issuecomment-5289814648`. Project/revision/native marker remained unchanged and generated output remained retained. This excludes live `RESTORE_RECOVERY_FAILED`.
- `COMMIT_HISTORY_LOST` marks only a transition-owned object after that object is already absent from or replaced in the history dictionary, so it cannot produce the current dictionary's `MULTIPLE` entry class. Canonical project replacement cannot explain the tuple either: every project-cache remove/replace path calls `SourceReconcileUndoCoordinator.Forget` first and would remove the history instead of retaining multiple entries.
- Baseline `main` is `d321660c632b2c66cf0cefe78c9c0ecea93bb198`. This pass reserves only `SourceReconcileUndoCoordinator.cs`, the focused coherence guard base, and this claim. It will treat managed `Document` wrappers as the same drawing only when their exact native `Database` reference is identical; cached canonical `ProjectState` reference, ProjectId, semantic stamp, native marker and backing-store checks remain unchanged and fail closed.
- The LOCAL-004 probe/runner/gates/docs, project-cache lifecycle, private data, BricsCAD execution and GitHub Actions remain excluded. Issue `#1005` stays open until the unchanged exact-SHA matrix passes.
