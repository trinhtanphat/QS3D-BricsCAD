# Curtain 3D post-commit idle refresh

Lane-Key: `issue-5277`
Reservation-Protocol: `v2`

## Defect boundary

`QS3DCURTAIN3D` commits its native host/frame/panel transaction before `FinalizeUi`. The normal-success UI path previously called `Editor.Regen()` and then queued `QS3DVIEW3D` with `SendStringToExecute`. Those are two forced graphics/view updates immediately after a committed geometry batch and can collide with BricsCAD's own repaint/command loop, surfacing the native `Screen update was interrupted because of unknown error.` dialog.

## Contract

- Normal-success `FinalizeUi` updates the project palette, status text and command-line message only.
- It does not call `Editor.Regen()`.
- It does not automatically queue `QS3DVIEW3D`.
- BricsCAD owns the normal idle repaint after the committed native transaction.
- Manual `QS3DVIEW3D` remains available when the user explicitly wants the 3D framing command.
- Pre-commit atomic rollback retains the existing best-effort `TryRegen(document)` recovery path because that path repairs a failed/rolled-back visual state rather than stacking a second normal-success refresh.

## Deterministic validation

Run:

```text
python scripts/preflight-curtain3d-postcommit-idle-refresh.py
python scripts/preflight-all.py
```

The focused guard isolates `CurtainWallBuildCommands.FinalizeUi`, rejects forced `Editor.Regen()` / automatic `QS3DVIEW3D` there, preserves the palette/status/message synchronization, and requires rollback-only `TryRegen` to remain.

## Runtime evidence boundary

Hosted CI can prove source structure and locked V25 compilation, but it cannot prove the absence of a native BricsCAD graphics-driver/runtime dialog. Licensed V25/V26 reproduction with a representative Curtain 3D selection remains `LOCAL_ONLY` evidence and must be tied to the exact candidate SHA if recorded.