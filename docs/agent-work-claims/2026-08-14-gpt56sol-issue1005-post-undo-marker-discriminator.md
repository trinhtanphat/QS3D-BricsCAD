# Work claim — LOCAL-004 post-Undo marker discriminator

- Status: `ACTIVE`
- Agent: `gpt56sol-source-reconcile-desync-agent`
- Registered: `2026-08-14T16:51:35+07:00`
- Baseline main SHA: `5f13d2fd6d02db7b3989f4ea097e00c55805606a`
- Priority: `LOCAL-004 / issue #1005 — distinguish native marker rollback before another production change`

## Reserved scope

Add one automation-only, privacy-safe discriminator that classifies the Source Reconcile native revision marker after the guarded native Undo and Redo steps relative to the already captured pre-final and post-final opaque marker tokens. Publish only existing bounded classes such as `ADVANCED`, `UNCHANGED`, and `MISSING_OR_INVALID` through the LOCAL-004 result marker.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SourceReconcileRuntimeProbeCommands.cs`
- `scripts/test-bricscad-v25-source-reconcile.ps1`
- `scripts/preflight-source-reconcile-runtime-probe.py`
- This claim file and the existing LOCAL-004 claim coordination note

## Excluded scope

- `SourceReconcileUndoCoordinator`, `SourceReconcileService`, native marker storage, history/observer behavior, or any other production source
- LOCAL-004 workflow order, semantic/native acceptance criteria, drawing/private-data handling, or supporting LOCAL documentation
- BricsCAD execution, private data, GitHub Actions, packaging, signing, or release

## Validation plan

- Run `scripts/preflight-source-reconcile-runtime-probe.py`.
- Run the related Source Reconcile generic/manual/static gates that do not execute BricsCAD.
- Build Core and the installed-reference V25 adapter if the local toolchain is available without launching BricsCAD.
- Audit the emitted and runner-accepted keys for an exact sanitized allowlist and forbidden raw revision/identity/path/handle leakage.

## Coordination

The ACTIVE LOCAL-004 runtime claim owns the broader licensed matrix and originally created these surfaces. Its owner `/root` explicitly delegated this narrow automation-only discriminator to `/root/fix_source_reconcile_desync`; the matching split is recorded in that claim. The local owner retains all BricsCAD execution and result ownership. No open PR overlaps this edit. This continuation deliberately precedes any proposed production marker undo-recording change.

## Completion condition

The three-file probe/runner/gate change is merged to current `main`, this claim is marked `COMPLETED`, and the exact merge SHA is handed off for the licensed unchanged LOCAL-004 matrix. Issue #1005 remains open pending that runtime discriminator.
