# BricsCAD V25 Basic Drawing UI source-document affinity

## Scope

Lane C03. `BasicDrawingCommands` owns the exact source `Document` captured when `QS3DDRAWLINE`, `QS3DDRAWRECT`, or `QS3DDRAWCIRCLE` starts. Process-wide Workspace status must not be published for that command after another MDI document becomes active.

## Native commit truth

`AppendEntity` owns the native document lock/transaction and returns only after `transaction.Commit()`. `FinalizeSuccess` is post-commit presentation/synchronization. A failure in implied-selection, regen, editor output, or Workspace status publication must not be treated as CAD rollback.

## Deterministic / REMOTE_SAFE verification

Run:

```text
python scripts/preflight-v25-basic-drawing-ui-source-affinity.py
```

The guard requires `Report` to keep editor output on the captured source document while routing process-wide Workspace status through an exact source-document/active-MDI identity fence. The helper must remain presentation-only and must not acquire CAD/project mutation ownership, dispatch commands, or retain work through the dispatcher.

Run aggregate feature source guards, deterministic smoke, and the BricsCAD V25 build against admitted locked reference generations. These results are REMOTE_SAFE evidence only.

## Licensed BricsCAD V25 / LOCAL_ONLY

In a licensed V25 host, start each Basic Drawing command from DWG A and exercise an A -> B MDI switch after native entity commit but before success/UI-sync reporting completes. Verify:

- the committed entity remains in A;
- source editor output remains best-effort against A;
- B does not receive A's Workspace success/warning/failure status;
- a presentation failure does not report or imply CAD rollback.

Do not claim `LOCAL_PASS` unless these timing scenarios are exercised in a real licensed BricsCAD V25 runtime.
