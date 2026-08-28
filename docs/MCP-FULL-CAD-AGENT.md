# QS3D full CAD agent over MCP

Status: SOURCE_READY / PENDING_LOCAL

The QS3D BricsCAD plugin embeds the MCP endpoint directly. A second MCP repository, Node runtime, PowerShell setup, or command-shell workflow is not required for end users.

## End-user path

1. Open BricsCAD with the QS3D plugin loaded.
2. Open `TOOL > MCP (AI) > Cài đặt MCP`.
3. If Cloudflare Tunnel is not installed, use the installer button and complete the normal Windows installer UI.
4. Click `Đăng nhập Cloudflare`; credentials are entered only in Cloudflare's browser page. QS3D never asks for or stores the Cloudflare password.
5. Enter the public hostname to use for QS3D, then click the automatic create/connect action.
6. Copy the displayed public MCP URL and bearer token, open ChatGPT Apps/Developer Mode, create the custom MCP app, then scan tools.
7. Future BricsCAD starts reuse the named-tunnel configuration and attempt to reconnect automatically.

Quick Tunnel remains a one-click test fallback only. Production use should use the persistent named tunnel.

## Agent tool model

The MCP surface follows an API-first rule. ChatGPT should use direct CAD database operations when a direct tool exists, use the bounded BricsCAD command-line tool for advanced/native workflows, and use mouse/keyboard only when the operation genuinely requires UI interaction.

Direct native tools include line, circle, polyline and text creation; entity move/rotate/scale/delete; layer create/current; selection/database/view inspection; command-active wait; and existing QS3D command dispatch.

`cad_command_sequence` is restricted to an explicit BricsCAD allowlist. It covers advanced drawing and editing plus hatch, dimensions, blocks, xrefs, layout/viewports, plot, open/save, undo/redo and cleanup workflows. It does not expose a general shell or arbitrary process launch.

UI fallback is also constrained. `cad_ui_click` receives BricsCAD-client-relative pixels and refuses coordinates outside the BricsCAD client rectangle. `cad_ui_type` and `cad_ui_key` target the current BricsCAD process window. They are not browser/desktop-wide remote-control tools.

Every mutating MCP tool requires `confirmMutation=true`. `cad_agent_stop` immediately disables autonomous mutation/UI tools and sends two ESC characters; `cad_agent_resume` requires explicit mutation confirmation. Mutations are recorded to `%APPDATA%\QS3D\mcp-agent-audit.jsonl`; typed text content is not written to the audit log.

## Full-drawing workflow

A capable ChatGPT client can now run a drawing loop such as:

`inspect drawing -> establish layers/styles -> create/modify geometry -> hatch/annotate/dimension -> blocks/xrefs -> layout/viewports -> verify database/view state -> correct with move/trim/undo -> save -> plot/export`.

Direct tools should be preferred for deterministic geometry. `cad_command_sequence` supplies the remaining native command-line workflows. Mouse/keyboard fallback is intended for UI-only dialogs or ribbon/palette actions that have no stable API/command-line path.

## Security boundary

The MCP listener binds to `127.0.0.1:8765` only. Public access is through the configured tunnel. `/mcp` requires the QS3D bearer token. The server has bounded request sizes, session IDs, mutation confirmation, a BricsCAD-only UI target, an explicit CAD command allowlist, emergency stop and a local audit file. It does not expose PowerShell, `cmd.exe`, arbitrary shell execution, or arbitrary process launch.

Cloudflare username/password are provider-owned credentials and are entered only in Cloudflare's browser login flow. QS3D stores only the resulting local Cloudflare certificate/tunnel configuration required by `cloudflared`.

## LOCAL_ONLY qualification

Hosted CI can prove source guards, Core tests and managed-reference plugin compilation, but it cannot prove real Windows desktop input or a real ChatGPT-to-Cloudflare-to-BricsCAD session. Final runtime qualification therefore remains `PENDING_LOCAL` until a local agent tests the exact source SHA.

Required local matrix:

- Load the exact V25 plugin in licensed BricsCAD V25 and the exact V26 plugin in licensed BricsCAD V26.
- Verify embedded `/healthz`, MCP initialize, initialized notification, `tools/list`, bearer rejection/acceptance and session handling.
- Complete browser Cloudflare login from the QS3D wizard without terminal use; create/reuse the named tunnel and public hostname; restart BricsCAD and verify automatic reconnect.
- Connect ChatGPT Web custom MCP to the public `/mcp` endpoint and verify tool discovery.
- Read active document, selection, database snapshot and view state.
- Create line/circle/polyline/text and verify native entities/layers/handles in the real DWG.
- Transform/delete entities and prove transaction consistency plus audit entries.
- Exercise command-sequence coverage for hatch, dimension, block/insert, layout/viewport, save and plot on a disposable drawing.
- Exercise BricsCAD-window-only mouse click, Unicode typing and named-key input; verify coordinates outside the BricsCAD client are rejected.
- Trigger `cad_agent_stop`, prove subsequent mutation/UI requests are refused, then resume explicitly.
- Save/reopen the disposable DWG and verify the resulting drawing survives the full round trip.
- Never use customer/private DWGs or expose bearer tokens, Cloudflare credentials, private paths or unsanitized screenshots in committed evidence.

Until this matrix passes on an exact source SHA, report it as `PENDING_LOCAL`, never `LOCAL_PASS`.
