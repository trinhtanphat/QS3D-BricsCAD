# MCP startup / save stability

Issue: #5441

This lane is infrastructure-only. It must not change A00-A13/A0X, drawing handles, geometry, annotations, registers, or project-specific content.

## Startup contract

When the V25 plugin loads, QS3D attempts to Resume local desktop control for the current BricsCAD process after the embedded MCP server starts. The existing local emergency boundaries remain authoritative: Esc x2, Pause, and host shutdown revoke desktop control.

Tunnel startup is independent from optional Agent Center UI augmentation. A ribbon/palette augmentation failure therefore cannot skip `McpTransportCoordinator.TryAutoStartPreferred()`.

## Current-document save contract

Both public current-document save surfaces (`cad_save` and bounded `QSAVE`) now share `McpNativeCurrentDocumentSave` instead of writing the already-open DWG through `Database.Save()` or `Database.SaveAs()`.

The helper queues exactly one native `_.QSAVE` attempt from BricsCAD application context and waits for `CommandEnded`, `CommandCancelled`, or `CommandFailed` outside that CAD callback. This keeps the UI/application context free to execute the host command while `McpCadMutationCoordinator` retains the single-writer pending barrier until terminal command completion.

Before queueing, the helper requires a rooted active-document path, rejects read-only documents, and requires `CMDACTIVE=0`. After terminal success it verifies that the same active document/path remains current and waits for persistent DBMOD bits `(1 | 4 | 32)` to clear; residual window/view DBMOD bits do not turn a successful save into a false failure.

A timeout is completion-uncertain. The caller is explicitly told **not to retry automatically**; the native-command writer barrier remains authoritative until the host eventually reports a terminal event. True `cad_save_as` remains a separate path-changing operation using `Database.SaveAs` and its own bounded persistent-content DBMOD confirmation.

## Existing contracts re-verified

- `cad_command_state` remains a read-only view/status tool and is excluded from `MutationTools`; callers must not be forced to send `confirmMutation=true`.
- The merged screen-update race fix remains in place: view mutations gate on `CMDACTIVE`, and the view runtime must not force `REGEN` or `UpdateScreen` recovery.
- Closed-curve extrusion continues to build a database-resident Region before `Solid3d.CreateExtrudedSolid`.
- Boolean operations continue to pass a transient cloned operand to the kernel rather than a database-resident tool solid.
- Legacy Solid3d inspection remains bounded and does not force synchronous `GeometricExtents` on the inspection path.

## Runtime qualification

Protected source CI verifies reservation/admission, source contracts, deterministic tests, and compilation against trusted BricsCAD V25 references. It does **not** prove a live licensed BricsCAD save/display session.

After installing the exact built V25 plugin, perform the LOCAL_ONLY qualification:

1. BricsCAD startup shows Desktop Resume ON without a manual click.
2. The selected persistent OpenAI or Cloudflare Named tunnel starts even if an optional Agent Center augmenter fails.
3. `cad_command_state` succeeds with `{}`.
4. While `CMDACTIVE=0`, `cad_view_zoom_extents` does not produce the generic screen-update popup. While a command is active, it fails closed instead.
5. Reopen a real DWG, make a persistent edit, run `cad_save`, and verify terminal native QSAVE completion plus clean persistent DBMOD state without `eCantOpenFile`.
6. Repeat through bounded `QSAVE`; confirm it shares the same native lifecycle and does not issue a second blind attempt after any completion-uncertain timeout.
7. Verify `cad_save_as` still performs a real path transition and the new active filename is reported.

Until those licensed runtime steps pass on the exact built binary, keep the live BricsCAD save result `PENDING_LOCAL`; do not represent static CI as runtime evidence.
