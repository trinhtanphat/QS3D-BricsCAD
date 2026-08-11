# Work claim — Model Review focus exact zoom dispatch

- Status: `ACTIVE`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T22:14:00+07:00`
- Baseline main SHA: `b1b22130e2715dd3639e2e18073144f17dfe8dc9`
- Priority: continue source-safe audit after exact Opening Auto Host; remove an unnecessary asynchronous QS3D command re-entry from Model Review Focus.

## Reserved scope

Harden `QS3DFOCUS` so the already-resolved source `Document` and implied/prompted selection are zoomed in the same command execution instead of queueing `QS3DZOOMSELECTED` through `Document.SendStringToExecute`.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/ModelReviewCommands.cs`
- `src/QS3D.BricsCAD.V25/ViewportCommands.cs` only if a narrowly scoped exact zoom entry point is required
- a source/static regression gate under `tests/` that prevents `QS3DFOCUS` from re-entering `QS3DZOOMSELECTED`

## Excluded scope

- `QS3DISOLATE` / `QS3DUNISOLATE` native command behavior
- Model Health / review-window UI
- Xref, Quantity, Installer, Floor Level, Project Unit Policy, Opening Auto Host and other currently claimed lanes
- BricsCAD V25 native/runtime qualification

## Validation plan

- verify `QS3DFOCUS` retains current highlight semantics while zooming the exact current selection/document synchronously
- add/run a source-level regression gate where available
- inspect diff against current `main`; do not dispatch GitHub Actions

## Coordination

This claim is intentionally limited to the `QS3DFOCUS` -> exact zoom call chain. It does not reserve broader Model Review, viewport commands, or native isolate workflows.

## Completion condition

The claim is complete when the exact-focus implementation and regression gate are pushed/merged to `main`, the claim is marked `COMPLETED`, and any BricsCAD V25 native-runtime evidence remains explicitly LOCAL_ONLY unless produced by a local agent.
