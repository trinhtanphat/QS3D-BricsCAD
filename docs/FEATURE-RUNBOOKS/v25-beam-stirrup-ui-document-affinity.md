# BricsCAD V25 Beam Stirrup palette UI document affinity

## Scope

C03 carrier for issue #6191. This guard covers process-wide Workspace/project-palette feedback emitted by `BeamStirrupCommands` after a command has captured a source BricsCAD `Document`.

- Lane: C03
- Lane-Key: issue-6191
- Ownership-Key: v25-beam-stirrup-workspace-document-affinity-v1
- Canonical carrier: `agent/c03-20260908-beam-stirrup-ui-affinity/issue-6191-beam-stirrup-ui-affinity`

## Invariant

Native editor output remains associated with the captured source document. Process-wide palette refresh/status publication must fail closed unless that exact source `Document` is still `Application.DocumentManager.MdiActiveDocument` at the individual mutation point.

Health and ordinary `Report` status publication remain best-effort. `FinalizeUi` deliberately retains its existing outer UI-sync failure semantics: project-palette refresh and status exceptions are not silently converted into best-effort success there.

The affinity helpers do not acquire CAD locks or transactions, mutate project state, dispatch commands, or queue deferred work.

## REMOTE_SAFE deterministic guard

Run:

```text
python scripts/preflight-v25-beam-stirrup-ui-document-affinity.py
```

The guard verifies exact reference identity, centralization of `PaletteCoordinator.RefreshProject` / `SetStatus`, source-document fences before each process-wide mutation, preservation of finalization ordering and exception semantics, and absence of CAD/project mutation ownership inside the presentation helpers.

Repository preflight, deterministic smoke and the locked/admitted-reference BricsCAD V25 compile are also required on the exact candidate head before merge.

## LOCAL_ONLY licensed BricsCAD V25 exercise

This is not satisfied by hosted CI. In licensed BricsCAD V25:

1. Open two drawings A and B with the QS3D Workspace visible.
2. Start a Beam Stirrup health/authoring path from A and use a controlled breakpoint/fault-injection window to switch active MDI to B immediately before status/finalization publication.
3. Verify A-originated Beam Stirrup status/project refresh does not overwrite the Workspace state for B.
4. Verify source editor output remains directed to A where the existing command path owns A.
5. Repeat across success, validation failure and UI-sync failure paths.

Do not record `LOCAL_PASS` without this licensed native exercise.

## Compatibility review

- No geometry/stirrup builder semantics changed.
- No project mutation/transaction ownership changed.
- No recapture of a replacement active document is permitted.
- Stale process-wide UI is suppressed rather than redirected to a different drawing.
- Existing Health/Report best-effort status behavior and `FinalizeUi` outer warning behavior are preserved.
