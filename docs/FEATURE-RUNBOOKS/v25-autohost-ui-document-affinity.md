# BricsCAD V25 Auto Host UI document-affinity runbook

Issue: #6262  
Lane: C03  
Runtime split: REMOTE_SAFE static/deterministic/V25 compile vs LOCAL_ONLY licensed BricsCAD timing.

## Defect and invariant

`QS3DAUTOLINKHOSTS` captures the source `Document` that owns the selection, project mutation and editor reporting. `PaletteCoordinator.RefreshProject()` and `PaletteCoordinator.SetStatus(...)` are process-global Workspace publication surfaces: they operate on whichever document is active when publication occurs. Before this fix, completion/error from DWG A could therefore refresh or overwrite the Workspace/status belonging to DWG B after an A→B MDI switch.

The invariant is:

1. Source editor reporting remains bound to the captured `Document`.
2. Every process-global Palette/Workspace mutation must re-check exact native document identity with `ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument)` immediately before publication.
3. A stale source document silently skips global refresh/status. Switching documents is not a post-commit UI failure and must not manufacture a rollback/error result after native/project mutation already succeeded.
4. Actual exceptions from Palette publication while the source document is still active retain the existing post-commit warning/error handling.
5. This change does not acquire CAD transactions/document locks, change project mutation ownership, alter rollback, or dispatch commands.

## REMOTE_SAFE validation

Run the dedicated deterministic source guard:

```text
python scripts/preflight-v25-autohost-ui-document-affinity.py
```

It requires a reusable exact-document identity fence, requires completion/error paths to route global Workspace/status publication through that fence, and rejects direct `PaletteCoordinator.RefreshProject()` / `SetStatus(...)` calls from those paths.

Run repository aggregate feature guards and the normal CI lane. The final merge authority is fresh required CI on the exact candidate head. V25 compilation is valid only when CI/build uses the repository's admitted BricsCAD V25 reference workflow; a remote green compile is `REMOTE_SAFE` evidence, not a licensed native-runtime PASS.

## LOCAL_ONLY licensed BricsCAD scenario

Do not record `LOCAL_PASS` unless this is executed in an actual licensed BricsCAD V25 host.

1. Open two distinct drawings/projects, A and B, and make A active.
2. In A, select deterministic captured Door/WallOpening elements and run `QS3DAUTOLINKHOSTS`.
3. Arrange an A→B MDI switch at the boundary after A's project/native work is complete but before completion/error Workspace publication (debug breakpoint or deterministic timing harness is acceptable).
4. Verify B's Workspace/project browser/status is not refreshed or overwritten by A's completion/error message.
5. Verify A's editor reporting remains associated with A and that any mutation already committed in A remains the actual operation result.
6. Switch back to A and explicitly refresh if needed; verify the Workspace then reflects A's canonical project state.
7. Repeat an error-path case and verify stale A error status cannot overwrite active B.

Expected classification until that host exercise is performed: `LOCAL_ONLY / NO_RESULT`.

## Self-review checklist

- exact source-document affinity before each global UI publication;
- active-document switch between refresh and status is safe because each helper re-checks independently;
- no retained document/project wrappers are introduced;
- no new subscribe/unsubscribe or modeless lifetime ownership is introduced;
- no transaction, cancellation or rollback semantics change;
- stale-document skip is not reported as a failed commit;
- Palette exceptions while still active remain contained by the existing post-commit warning/error path;
- no MCP runtime/transport, installer/release, or Quantity engine ownership crosses into this carrier.
