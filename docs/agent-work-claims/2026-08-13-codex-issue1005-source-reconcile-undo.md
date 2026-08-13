# Work claim — Source Reconcile native Undo/Redo semantic coherence

- Status: `COMPLETED`
- Agent: `codex-issue1005-source-reconcile-undo-20260813` (`/root/fix_source_reconcile_undo`)
- Registered: `2026-08-13T15:33:32+07:00`
- Baseline main SHA: `f926c2dcbe9a850d8be2425e940c4d4c929d324f`
- Priority: GitHub issue `#1005` / `LOCAL-004 P0` production blocker reproduced on licensed BricsCAD V25

## Reserved scope

Fix the production Source Reconcile command boundary so native Undo/Redo restores the canonical in-memory `ProjectState` snapshot corresponding to the CAD state committed by `QS3DSYNCSOURCE`. The integration must remain document/project bound, preserve the existing one-CAD-transaction invalidation and pre-commit `ProjectStateSnapshot` failure rollback, and fail closed rather than applying a semantic snapshot to a replaced project or another DWG.

The implementation will use a native transaction-bound revision marker plus an in-session, document-scoped semantic snapshot history. Native Undo/Redo command completion will observe the marker restored by BricsCAD and restore only the matching snapshot for the same cached canonical project. Lifecycle teardown/reload/forget will discard stale history.

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

## Completion condition

The focused production fix and regressions are merged to current `main`, issue `#1005` is closed, this claim is `COMPLETED` with exact SHAs and executed validation, and LOCAL-004 remains `IN_PROGRESS / PENDING_LOCAL` with an exact fixed-SHA V25 rerun requested rather than falsely promoted from source evidence.

## Completion record

- Claim-only commit: `c4a9649a0a58a37dcfb27755958f7a2e60fb08fc`; claim PR `#1006`; claim merge SHA `bf1b4e3ac39b2880080eb0b6579afaf15b1c969d`.
- Implementation commit: `685bbca98bf296946cbaabbe7252851d2582daff`; source PR `#1007`; source merge SHA `1c957ae2dc022db8cafbfcf0de91d9d47a53e68f`.
- Production issue `#1005` closed only after source PR `#1007` merged. The closing comment requests the guarded LOCAL-004 V25 rerun on exact main SHA `1c957ae2dc022db8cafbfcf0de91d9d47a53e68f`.
- Executed locally without GitHub Actions or BricsCAD runtime: installed-reference V25 `Release|x64` adapter build PASS with `0 warnings / 0 errors`; Core smoke executable `ALL PASS`; strict manual-CI and generic preflight PASS; all `731` auto-discovered feature preflights PASS.
- The additive LOCAL-004 probe/runner/gate remained unchanged. Licensed native Undo/Redo, rollback, multi-DWG and cold-reopen evidence remains `PENDING_LOCAL`; no source/static/build result is promoted to `LOCAL_PASS`.
