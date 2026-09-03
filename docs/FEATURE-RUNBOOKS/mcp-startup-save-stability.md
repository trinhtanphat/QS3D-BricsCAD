# MCP startup / save stability

Issue: #5441

This lane is infrastructure-only. It must not change A00-A13/A0X, drawing handles, geometry, annotations, registers, or project-specific content.

## Startup contract

When the V25 plugin loads, QS3D now attempts to Resume local desktop control for the current BricsCAD process after the embedded MCP server starts. The existing local emergency boundaries remain authoritative: Esc x2, Pause, and host shutdown revoke desktop control.

Tunnel startup is independent from optional Agent Center UI augmentation. A ribbon/palette augmentation failure therefore cannot skip `McpTransportCoordinator.TryAutoStartPreferred()`.

## Existing contracts re-verified

- `cad_command_state` remains a read-only view/status tool and is excluded from `MutationTools`; callers must not be forced to send `confirmMutation=true`.
- The merged screen-update race fix remains in place: view mutations gate on `CMDACTIVE`, and the view runtime must not force `REGEN` or `UpdateScreen` recovery.
- `cad_save` remains distinct from true `cad_save_as`; it must not use SaveAs-over-current-path semantics.

## Runtime qualification

Source CI can verify admission/routing contracts but cannot prove the licensed BricsCAD current-document save implementation or GPU/display stack. After installing the exact built V25 plugin, verify:

1. BricsCAD startup shows Desktop Resume ON without manual click.
2. The selected persistent OpenAI or Cloudflare Named tunnel starts even if an optional Agent Center augmenter fails.
3. `cad_command_state` succeeds with `{}`.
4. While `CMDACTIVE=0`, `cad_view_zoom_extents` does not produce the generic screen-update popup. While a command is active, it fails closed instead.
5. Exercise `cad_save` and bounded QSAVE after reopen. If native `eCantOpenFile` remains, do not label save PASS; use Ctrl+S as the operational fallback and capture the exact runtime diagnostics for the follow-up native-save lane.
