# MCP unified diagnostics + host-wide theme

Issue: #4750  
Lane-Key: `issue-4750`  
Ownership-Key: `v25.mcp.diagnostics-theme-hostwide`

## Goal

Keep ChatGPT's MCP boundary bounded while making two production behaviors first-class:

1. ChatGPT can inspect recent MCP, QS3D and BricsCAD diagnostics without arbitrary local-file access.
2. The MCP Agent Center `System` / `Dark` / `Light` selector controls the whole working host, not only the Agent Center popup.

## Diagnostics architecture

`McpDiagnosticHub` appends sanitized JSONL events to the existing MCP audit file (`McpCadAgentRuntime.AuditFilePath`). This intentionally reuses the established `cad_audit_tail` read surface instead of adding a generic filesystem reader.

Automatic sources include:

- MCP transport `LastError` changes and OAuth MCP activity changes.
- QS3D optional-service startup warnings.
- Unhandled runtime exceptions and unobserved task exceptions.
- Active BricsCAD document command failures/cancellations.
- QS3D command starts/ends so ChatGPT can correlate a requested command with its terminal state.

All diagnostic messages are bounded and redact authorization bearer values plus token/secret/password-like assignments before persistence.

### ChatGPT on-demand snapshot

For a richer point-in-time bundle:

1. Call MCP `qs3d_run_command` with command `QS3DDIAGNOSTICSSNAPSHOT` and `confirmMutation=true`.
2. Optionally call `cad_wait_idle`.
3. Call `cad_audit_tail` with a suitable bounded limit.

The snapshot adds:

- MCP state from `McpEmbeddedServer.Describe()`.
- Current host `COLORTHEME` and `CMDACTIVE`.
- Current host-wide theme mode/effective mode.
- Existing QS3D project audit summary plus up to the latest 25 validated project audit events.

This preserves the existing mutation confirmation, OAuth/bearer validation, audit, pause/emergency-stop and no-remote-shell security model.

## Theme architecture

`Qs3dThemeCoordinator` owns one persisted mode:

- `system`: resolve Windows `AppsUseLightTheme`; update again when Windows user preferences change.
- `dark`: effective dark.
- `light`: effective light.

The effective mode is applied to both sides of the host:

### BricsCAD

The coordinator updates BricsCAD `COLORTHEME`:

- `0` for dark.
- `1` for light.

System mode resolves Windows first, then writes the corresponding effective BricsCAD value.

### QS3D WPF

The coordinator tracks loaded QS3D WPF elements and updates the canonical `Theme.xaml` color/brush keys in-place where possible, including background, text, borders, accents, semantic colors and system selection resources. It also maps known hard-dark QS3D/Start Center brushes on already-loaded visual trees so the change is not limited to resource-based dialogs.

Future WPF surfaces are themed through a class-level `FrameworkElement.Loaded` hook.

### MCP Agent Center

The existing Agent Center buttons remain `System`, `Dark`, and `Light`. A class-level button click hook recognizes those three buttons only when their owning window is `McpAgentControlCenterWindow`, then propagates the selection to `Qs3dThemeCoordinator`. The popup can keep its own rendering code while the selected mode now reaches the full host.

## Remote theme commands

The following bounded QS3D commands are available through the existing confirmed `qs3d_run_command` tool:

- `QS3DTHEMESYSTEM`
- `QS3DTHEMEDARK`
- `QS3DTHEMELIGHT`
- `QS3DTHEMESTATUS`

Remote theme changes therefore still require `confirmMutation=true`; no new unconfirmed mutation path is introduced. `QS3DTHEMESTATUS` records the current theme state into the unified diagnostic tail.

## Qualification

Hosted acceptance:

```powershell
python scripts/preflight-mcp-diagnostics-theme.py
python scripts/preflight-all.py
```

Required fresh CI on the exact PR head:

- preflight / all discovered feature guards
- deterministic Core smoke
- locked BricsCAD V25 compile references
- V25 plugin build

Local production qualification still requires licensed BricsCAD with the built QS3D DLL loaded. Validate all three theme modes against visible BricsCAD ribbon/panels/dialogs, QS3D palettes/windows, and the Agent Center. Then trigger one QS3D command failure and one diagnostics snapshot and verify that ChatGPT can retrieve the sanitized entries through `cad_audit_tail`.
