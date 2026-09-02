# MCP BricsCAD Screen-Update Safety

Issue: #5307

## Symptom

BricsCAD can display the modal message:

`Screen update was interrupted, because of unknown error.`

This message is a graphics/display failure envelope. In the QS3D direct-view path, the actionable race was that `cad_view_zoom_extents`, `cad_view_fit_entities`, and `cad_view_set` were serialized against other MCP mutations but could still reach `Editor.GetCurrentView` / `Editor.SetCurrentView` while an unrelated BricsCAD command was active outside the MCP writer coordinator.

## Source fix

Direct view mutations now fail closed unless `CMDACTIVE == 0`:

- `cad_view_zoom_extents` checks before `Database.UpdateExt(false)` and again on the actual view-apply path;
- `cad_view_fit_entities` checks before entering the view workflow and on the actual view-apply path;
- `cad_view_set` checks before obtaining the current view and immediately before `Editor.SetCurrentView`;
- `ApplyExtents` checks before obtaining the current view and immediately before `Editor.SetCurrentView`.

The failure tells the caller to wait for `cad_wait_idle` / `CMDACTIVE=0` and retry. It does not sleep inside the graphics mutation, force `REGEN`, call `UpdateScreen`, or auto-dismiss the BricsCAD popup.

## Invariants

- Direct view tools remain confirmed mutations.
- They remain serialized through the process-global `McpCadMutationCoordinator` single-writer lane.
- The popup observer remains passive diagnostics only.
- This fix does not claim that every instance of the generic BricsCAD message is caused by QS3D; GPU/driver or native BricsCAD graphics failures can produce the same message independently.

## Verification

Run:

`python scripts/preflight-mcp-screen-update-safety.py`

Then qualify on licensed BricsCAD V25/V26:

1. start a manual/native command and issue each direct view mutation; it must fail closed rather than alter the view;
2. return to `CMDACTIVE=0` and repeat; the view mutation must succeed;
3. stress view mutations around save/autosave/native command transitions;
4. confirm no `Screen update was interrupted` popup is produced by the guarded QS3D path;
5. inspect popup/audit diagnostics if BricsCAD still emits the generic message, then investigate renderer/driver/native graphics state separately.

Hosted/source CI proves the admission contract only. Live graphics qualification remains LOCAL_ONLY.