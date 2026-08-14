# Work claim — LOCAL-004 post-Undo marker discriminator

- Status: `COMPLETED`
- Agent: `gpt56sol-source-reconcile-desync-agent`
- Registered: `2026-08-14T16:51:35+07:00`
- Completed: `2026-08-14T18:50:37+07:00`
- Baseline main SHA: `5f13d2fd6d02db7b3989f4ea097e00c55805606a`
- Implementation merge SHA: `c43b71c3e454b0e19698ec2ab8538214e365048a`
- Implementation PR: `#1315`
- Priority: `LOCAL-004 / issue #1005 — distinguish native marker rollback before another production change`

## Reserved scope

Add one automation-only, privacy-safe discriminator that classifies the Source Reconcile native revision marker after the guarded native Undo and Redo steps relative to the already captured pre-final and post-final opaque marker tokens. Publish only existing bounded classes such as `ADVANCED`, `UNCHANGED`, and `MISSING_OR_INVALID` through the LOCAL-004 session-one result marker.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SourceReconcilePostUndoMarkerProbeCommands.cs`
- `scripts/test-bricscad-v25-source-reconcile.ps1`
- `scripts/preflight-source-reconcile-runtime-probe.py`
- This claim file and the existing LOCAL-004 claim coordination note

The discriminator is intentionally isolated in an additive automation-only command class rather than widening the existing large runtime probe. It consumes only `SourceReconcileUndoCoordinator.CaptureSanitizedState` and `SanitizedDiagnosticSnapshot.CompareMarkerTo`; the opaque native revision never leaves that production-owned snapshot type.

## Excluded scope

- `SourceReconcileUndoCoordinator`, `SourceReconcileService`, native marker storage, history/observer behavior, or any other production source
- LOCAL-004 semantic/native acceptance criteria, drawing/private-data handling, or supporting LOCAL documentation
- BricsCAD execution, private data, GitHub Actions, packaging, signing, or release

## Validation plan

- Run `scripts/preflight-source-reconcile-runtime-probe.py`.
- Run the related Source Reconcile generic/manual/static gates that do not execute BricsCAD.
- Build Core and the installed-reference V25 adapter if the local toolchain is available without launching BricsCAD.
- Audit the emitted and runner-accepted keys for an exact sanitized allowlist and forbidden raw revision/identity/path/handle leakage.

## Coordination

The ACTIVE LOCAL-004 runtime claim owns the broader licensed matrix and originally created these surfaces. Its owner `/root` explicitly delegated this narrow automation-only discriminator to `/root/fix_source_reconcile_desync`; the matching split is recorded in that claim. The local owner retains all BricsCAD execution and result ownership. No open PR overlapped this edit when the claim was registered. This continuation deliberately precedes any proposed production marker undo-recording change.

## Implementation — merged 2026-08-14

- PR `#1315` merged the implementation to `main` at exact SHA `c43b71c3e454b0e19698ec2ab8538214e365048a`.
- A dedicated automation-only helper captures private pre-final/post-final `SanitizedDiagnosticSnapshot` instances in memory and classifies the current native marker after guarded Undo/Redo relative to both baselines.
- Only four bounded fields are published: `post_undo_marker_vs_pre_final_state`, `post_undo_marker_vs_post_final_state`, `post_redo_marker_vs_pre_final_state`, and `post_redo_marker_vs_post_final_state`.
- The helper atomically augments the existing session-one marker only after `QS3DSRTSESSION1` has published a valid PASS marker with the exact nonce/schema/boundary. The runner validates all four fields against `ADVANCED / UNCHANGED / MISSING_OR_INVALID` and retains the sanitized `phase_marker` in local metadata for handoff even when cold reopen reproduces `NATIVE_UNDO_SEMANTIC_DIVERGENCE`.
- Production Undo coordinator/service/history/marker behavior and LOCAL-004 acceptance criteria were not changed by this lane.
- Static PR patch audit found no production-boundary bypass or raw revision/project/path/handle emission. No commit-status checks were reported for the exact candidate; this delegated lane did not dispatch GitHub Actions or execute licensed BricsCAD because both are explicitly owned by the root LOCAL-004 claim.

## Handoff / remaining acceptance

The delegated source-safe discriminator work is complete. The root LOCAL-004 owner must build and rerun the licensed BricsCAD V25 matrix from an exact clean SHA containing `c43b71c3e454b0e19698ec2ab8538214e365048a`, then publish the four-field discriminator tuple to issue `#1005`. Issue `#1005` remains open until that receipt proves the correct production fix or full Undo/Redo coherence.
