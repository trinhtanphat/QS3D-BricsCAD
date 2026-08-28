#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServer.cs"
ACCOUNT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
FALLBACK = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareOnboarding.cs"
AGENT_CENTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpAgentControlCenter.cs"
BOOTSTRAPPER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflaredBootstrapper.cs"
PUBLIC_ENDPOINT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPublicEndpointResolver.cs"
RIBBON_OVERRIDE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "McpRibbonCommandOverride.cs"


def main() -> int:
    paths = (SERVER, ACCOUNT, FALLBACK, AGENT_CENTER, BOOTSTRAPPER, PUBLIC_ENDPOINT, RIBBON_OVERRIDE)
    for path in paths:
        if not path.is_file():
            print("ERROR: missing", path.relative_to(ROOT))
            return 1

    text = SERVER.read_text(encoding="utf-8")
    account = ACCOUNT.read_text(encoding="utf-8")
    fallback = FALLBACK.read_text(encoding="utf-8")
    agent_center = AGENT_CENTER.read_text(encoding="utf-8")
    bootstrapper = BOOTSTRAPPER.read_text(encoding="utf-8")
    endpoint = PUBLIC_ENDPOINT.read_text(encoding="utf-8")
    ribbon_override = RIBBON_OVERRIDE.read_text(encoding="utf-8")
    errors: list[str] = []

    required = {
        "direct line creation": '"cad_create_line"',
        "direct circle creation": '"cad_create_circle"',
        "direct polyline creation": '"cad_create_polyline"',
        "direct text creation": '"cad_create_text"',
        "single entity inspection": '"cad_entity_inspect"',
        "entity transform": '"cad_entity_transform"',
        "entity delete": '"cad_entity_delete"',
        "layer management": '"cad_layer"',
        "command catalog": '"cad_command_catalog"',
        "bounded command sequencing": '"cad_command_sequence"',
        "view state": '"cad_view_state"',
        "idle wait": '"cad_wait_idle"',
        "BricsCAD-only mouse": '"cad_ui_click"',
        "BricsCAD-only typing": '"cad_ui_type"',
        "BricsCAD-only named keys": '"cad_ui_key"',
        "emergency stop": '"cad_agent_stop"',
        "explicit resume": '"cad_agent_resume"',
        "audit tail": '"cad_audit_tail"',
        "mutation confirmation": "confirmMutation=true",
        "window-relative click guard": "GetClientRect(hwnd, out rect)",
        "client-to-screen mapping": "ClientToScreen(hwnd, ref point)",
        "foreground process verification": "GetWindowThreadProcessId",
        "foreground window verification": "RequireSameForegroundCadWindow",
        "foreground acquisition": "RequireForegroundCadWindow",
        "Unicode SendInput": "SendUnicodeText(hwnd, text)",
        "mouse SendInput": "SendMouse(down)",
        "emergency foreground fallback": "TrySendEscapeFallback",
        "Win32 input API": 'DllImport("user32.dll"',
        "CAD application context": "ExecuteInApplicationContext",
        "bounded CAD wait": "ManualResetEventSlim",
        "timed-out CAD work cancellation": "queued work was cancelled when possible",
        "transactional native entities": "transaction.Commit()",
        "mutation audit": "mcp-agent-audit.jsonl",
        "bounded audit rotation": "MaxAuditBytes",
        "allowlisted CAD commands": "AllowedCadCommands",
        "QS3D command allowlist": '"^QS3D[A-Za-z0-9_]*$"',
        "script control-char rejection": "forbidden control characters",
        "script blank-terminator anti-chain guard": "inputs may not continue after a blank command terminator",
        "script known-command anti-injection guard": "inputs may not inject another CAD/QS3D command",
        "tool arguments object scoping": "TryExtractToolCall",
        "tool arguments object parser": "TryExtractObjectProperty",
        "top-level JSON member parser": "TryFindTopLevelPropertyValue",
        "top-level tool-name extraction": 'ExtractTopLevelString(parameters, "name")',
        "top-level arguments presence check": 'HasTopLevelProperty(parameters, "arguments")',
        "top-level mutation confirmation": 'ExtractTopLevelBoolean(body, "confirmMutation")',
        "duplicate top-level target rejection": "duplicate top-level JSON property",
        "HTTP transfer-encoding rejection": "Transfer-Encoding is not supported; use Content-Length",
        "duplicate critical-header rejection": "Duplicate security-sensitive HTTP header",
        "MCP session termination": 'request.Method == "DELETE"',
        "MCP protocol/session binding": "MCP-Protocol-Version does not match the initialized session",
        "bounded clients": "MaxConcurrentClients",
        "bounded sessions": "MaxSessions",
    }
    for label, token in required.items():
        if token not in text:
            errors.append(f"missing {label}: {token}")

    for command in (
        '"HATCH"', '"DIMLINEAR"', '"BLOCK"', '"XREF"', '"LAYOUT"',
        '"MVIEW"', '"PLOT"', '"SAVEAS"', '"OPEN"', '"UNDO"',
    ):
        if command not in text:
            errors.append(f"missing full-drawing command capability: {command}")

    mutation_routes = (
        'case "cad_create_line": return RequireMutation(arguments, "cad_create_line",',
        'case "cad_create_circle": return RequireMutation(arguments, "cad_create_circle",',
        'case "cad_create_polyline": return RequireMutation(arguments, "cad_create_polyline",',
        'case "cad_create_text": return RequireMutation(arguments, "cad_create_text",',
        'case "cad_entity_transform": return RequireMutation(arguments, "cad_entity_transform",',
        'case "cad_entity_delete": return RequireMutation(arguments, "cad_entity_delete",',
        'case "cad_layer": return RequireMutation(arguments, "cad_layer",',
        'case "cad_command_sequence": return RequireMutation(arguments, "cad_command_sequence",',
        'case "qs3d_run_command": return RequireMutation(arguments, "qs3d_run_command",',
        'case "cad_ui_click": return RequireMutation(arguments, "cad_ui_click",',
        'case "cad_ui_type": return RequireMutation(arguments, "cad_ui_type",',
        'case "cad_ui_key": return RequireMutation(arguments, "cad_ui_key",',
    )
    for route in mutation_routes:
        if route not in text:
            errors.append(f"mutation bypasses confirmation/stop gate: {route}")
    if 'if (!ExtractBoolean(body, "confirmMutation"))' in text:
        errors.append("mutation confirmation is regex-scoped instead of top-level JSON scoped")

    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start(", "mouse_event("):
        if forbidden in text:
            errors.append(f"forbidden remote execution/legacy input surface in MCP server: {forbidden}")

    if "McpCloudflareTunnelManager.StopForHostShutdown();" not in account:
        errors.append("browser-login named tunnel does not stop fallback tunnel before start")
    if fallback.count("McpCloudflareAccountTunnelManager.StopForHostShutdown();") < 2:
        errors.append("token/Quick fallback does not stop browser-login tunnel before start")
    if "BeginOutputReadLine" not in account or "BeginErrorReadLine" not in account or "process.WaitForExit();" not in account:
        errors.append("browser-login cloudflared command output is not asynchronously drained before process disposal")
    if "ingress:" not in account or "http_status:404" not in account:
        errors.append("browser-login named tunnel lacks hostname-scoped ingress + fail-closed 404 rule")
    if "EnableRaisingEvents = false" not in fallback or "process.EnableRaisingEvents = true;" not in fallback or "return IsRunning;" not in fallback:
        errors.append("fallback cloudflared ownership is not established before exit events are enabled")
    if "private static void HandleProcessExit(Process process)" not in fallback:
        errors.append("fallback cloudflared lacks idempotent owned-process exit cleanup")
    stale_quick_cleanup = "if (ReferenceEquals(_process, process))\n                {\n                    _process = null;\n                    _quickBaseUrl = string.Empty;\n                    _quickMode = false;"
    if stale_quick_cleanup not in fallback:
        errors.append("fallback Quick Tunnel exit can leave a stale trycloudflare public URL")

    center_required = (
        '[CommandMethod("QS3DMCPAGENTCENTER"',
        "McpCloudflaredBootstrapper.BeginInstall",
        "McpCloudflareAccountSetupWindow",
        "McpPublicEndpointResolver.Resolve()",
        "RunReadOnlySelfTest",
        'InvokeControlTool("cad_agent_stop"',
        'InvokeControlTool("cad_cancel_command"',
        'InvokeControlTool("cad_agent_resume"',
        "OpenChatGpt",
        "ThreadPool.QueueUserWorkItem",
        "Interlocked.CompareExchange(ref _localOperationActive",
        "Dispatcher.BeginInvoke",
        "DispatcherTimer",
        "StartQuickUrlPolling",
        "StopQuickUrlPolling",
        "_quickUrlPollTicks >= 20",
    )
    for token in center_required:
        if token not in agent_center:
            errors.append(f"Agent Center missing click-first capability: {token}")
    for forbidden in ("powershell.exe", "cmd.exe"):
        if forbidden in agent_center:
            errors.append(f"Agent Center exposes forbidden shell dependency: {forbidden}")

    bootstrap_required = (
        "cloudflared-windows-amd64.exe",
        "WinVerifyTrust",
        "CreateFromSignedFile",
        "Cloudflare",
        'PathEnvironment = "QS3D_CLOUDFLARED_PATH"',
        "backupCreated",
        "ProviderFlags = 0",
    )
    for token in bootstrap_required:
        if token not in bootstrapper:
            errors.append(f"verified cloudflared bootstrap missing: {token}")

    endpoint_required = (
        "McpCloudflareAccountTunnelManager.PublicMcpUrl",
        "McpCloudflareTunnelManager.PublicMcpUrl",
        "Uri.UriSchemeHttps",
        "uri.IsLoopback",
        "IPAddress.TryParse(uri.Host",
        "IsPrivateOrLocalAddress",
        'path = "/mcp"',
    )
    for token in endpoint_required:
        if token not in endpoint:
            errors.append(f"public MCP endpoint contract missing: {token}")

    for token in (
        '[Prefix + "MCP_SETTINGS"] = "QS3DMCPAGENTCENTER"',
        '[Prefix + "AI_DASHBOARD"] = "QS3DMCPAGENTCENTER"',
        '[Prefix + "MCP_DOCS"] = "QS3DMCPDOCSHTTP"',
        '[Prefix + "MCP_CONNECTION"] = "QS3DMCPCHECKHTTP"',
    ):
        if token not in ribbon_override:
            errors.append(f"Ribbon MCP route missing: {token}")

    if errors:
        print("Full MCP CAD agent preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: embedded MCP exposes direct transactional CAD creation/editing, bounded "
        "allowlisted advanced command workflows, top-level-scoped mutation confirmation, "
        "foreground-process-confined SendInput, emergency stop/resume with ESC fallback, "
        "idle/status observation, rotating local audit evidence, bounded HTTP/session handling, "
        "mutually-exclusive Cloudflare tunnel modes, verified one-click cloudflared bootstrap, "
        "unified click-first Agent Center with bounded Quick URL discovery, single HTTPS public "
        "endpoint resolution and mutation confirmation without arbitrary shell execution."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
