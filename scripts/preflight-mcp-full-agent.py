#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
SERVER = V25 / "McpEmbeddedServerV2.cs"
RUNTIME = V25 / "McpCadAgentRuntime.cs"
LEGACY_SERVER = V25 / "McpEmbeddedServer.cs"
TOP_LEVEL_JSON = V25 / "McpTopLevelJson.cs"
ACCOUNT = V25 / "McpCloudflareAccountOnboarding.cs"
FALLBACK = V25 / "McpCloudflareOnboarding.cs"
AGENT_CENTER = V25 / "McpAgentControlCenter.cs"
BOOTSTRAPPER = V25 / "McpCloudflaredBootstrapper.cs"
PUBLIC_ENDPOINT = V25 / "McpPublicEndpointResolver.cs"
RIBBON_OVERRIDE = V25 / "Ribbon" / "McpRibbonCommandOverride.cs"
V25_PROJECT = V25 / "QS3D.BricsCAD.V25.csproj"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require_tokens(errors: list[str], text: str, prefix: str, required: dict[str, str]) -> None:
    for label, token in required.items():
        if token not in text:
            errors.append(f"{prefix} missing {label}: {token}")


def main() -> int:
    paths = (
        SERVER, RUNTIME, LEGACY_SERVER, TOP_LEVEL_JSON, ACCOUNT, FALLBACK,
        AGENT_CENTER, BOOTSTRAPPER, PUBLIC_ENDPOINT, RIBBON_OVERRIDE,
        V25_PROJECT, V26_PROJECT,
    )
    for path in paths:
        if not path.is_file():
            print("ERROR: missing", path.relative_to(ROOT))
            return 1

    server = SERVER.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")
    top_level_json = TOP_LEVEL_JSON.read_text(encoding="utf-8")
    account = ACCOUNT.read_text(encoding="utf-8")
    fallback = FALLBACK.read_text(encoding="utf-8")
    agent_center = AGENT_CENTER.read_text(encoding="utf-8")
    bootstrapper = BOOTSTRAPPER.read_text(encoding="utf-8")
    endpoint = PUBLIC_ENDPOINT.read_text(encoding="utf-8")
    ribbon_override = RIBBON_OVERRIDE.read_text(encoding="utf-8")
    v25_project = V25_PROJECT.read_text(encoding="utf-8")
    v26_project = V26_PROJECT.read_text(encoding="utf-8")
    errors: list[str] = []

    require_tokens(errors, server, "MCP transport", {
        "loopback listener": "IPAddress.Loopback",
        "canonical MCP path": '"/mcp"',
        "health endpoint": '"/healthz"',
        "bearer authorization": 'const string prefix = "Bearer "',
        "constant-time bearer compare": "ConstantTimeEquals",
        "32-byte generated token": "var bytes = new byte[32]",
        "bounded clients": "MaxConcurrentClients = 16",
        "bounded sessions": "MaxSessions = 128",
        "bounded body": "MaxBodyBytes = 1024 * 1024",
        "HTTP transfer-encoding rejection": "Transfer-Encoding is not supported; use Content-Length",
        "duplicate critical-header rejection": "Duplicate security-sensitive HTTP header",
        "modern MCP protocol": 'ModernProtocolVersion = "2026-07-28"',
        "modern discovery": 'string.Equals(method, "server/discover"',
        "modern method routing": '"Mcp-Method"',
        "modern tool-name routing": '"Mcp-Name"',
        "exact ChatGPT origin": 'string.Equals(origin.DnsSafeHost, "chatgpt.com", StringComparison.OrdinalIgnoreCase)',
        "MCP initialize": 'string.Equals(method, "initialize"',
        "MCP tools/list": 'string.Equals(method, "tools/list"',
        "MCP tools/call": 'string.Equals(method, "tools/call"',
        "MCP session termination": 'request.Method == "DELETE"',
        "protocol/session binding": "MCP-Protocol-Version is invalid or does not match initialized session.",
        "canonical public endpoint": "McpPublicEndpointResolver.Resolve()",
        "runtime delegation": "McpCadAgentRuntime.Call(tool, arguments)",
        "structured tool results": r'\"structuredContent\"',
        "single repository truth": r'\"singleRepository\":true',
        "full agent truth": r'\"fullCadAgent\":true',
        "modular server identity": 'ServerVersion = "embedded-7"',
        "tool arguments object scoping": "TryExtractToolCall",
        "top-level scanner dependency": "McpTopLevelJson.TryFindPropertyValue",
    })

    tool_descriptors = (
        "connector_info", "qs3d_status", "cad_active_document", "cad_selection",
        "cad_database_snapshot", "cad_entity_inspect", "cad_view_state", "cad_wait_idle",
        "cad_sysvar", "cad_create_line", "cad_create_circle", "cad_create_arc",
        "cad_create_polyline", "cad_create_text", "cad_create_mtext",
        "cad_entity_transform", "cad_entity_delete", "cad_entity_set_layer", "cad_layer",
        "cad_command_catalog", "cad_command_sequence", "qs3d_run_command",
        "cad_ui_click", "cad_ui_type", "cad_ui_key", "cad_agent_stop",
        "cad_agent_resume", "cad_audit_tail", "cad_cancel_command",
    )
    for tool in tool_descriptors:
        if f'Tool("{tool}"' not in server:
            errors.append(f"MCP transport missing tool descriptor: {tool}")

    require_tokens(errors, runtime, "MCP CAD runtime", {
        "direct line creation": 'case "cad_create_line"',
        "direct circle creation": 'case "cad_create_circle"',
        "direct arc creation": 'case "cad_create_arc"',
        "direct polyline creation": 'case "cad_create_polyline"',
        "direct DBText creation": 'case "cad_create_text"',
        "direct MText creation": 'case "cad_create_mtext"',
        "single entity inspection": 'case "cad_entity_inspect"',
        "entity transform": 'case "cad_entity_transform"',
        "entity delete": 'case "cad_entity_delete"',
        "entity layer mutation": 'case "cad_entity_set_layer"',
        "layer management": 'case "cad_layer"',
        "read-only sysvar surface": 'case "cad_sysvar"',
        "command catalog": 'case "cad_command_catalog"',
        "bounded command sequencing": 'case "cad_command_sequence"',
        "view state": 'case "cad_view_state"',
        "idle wait": 'case "cad_wait_idle"',
        "BricsCAD-only mouse": 'case "cad_ui_click"',
        "BricsCAD-only typing": 'case "cad_ui_type"',
        "BricsCAD-only named keys": 'case "cad_ui_key"',
        "emergency stop": 'case "cad_agent_stop"',
        "explicit resume": 'case "cad_agent_resume"',
        "audit tail": 'case "cad_audit_tail"',
        "top-level mutation confirmation": 'McpTopLevelJson.ExtractBoolean(body, "confirmMutation")',
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
        "CAD dispatch queued state": "CadWorkQueued = 0",
        "CAD dispatch running state": "CadWorkRunning = 1",
        "CAD dispatch cancelled-before-start state": "CadWorkCancelledBeforeStart = 2",
        "CAD dispatch atomic start claim": "Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued)",
        "CAD dispatch atomic timeout cancellation": "Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued)",
        "CAD dispatch started-work bounded handoff": "item.DetachAfterStartedTimeout();",
        "CAD dispatch started-work response deadline": "throw new CadStartedTimeoutException(item);",
        "CAD dispatch mutation writer detachment": "McpCadMutationCoordinator.DetachMutationForDeferredCompletion(writerScope)",
        "CAD dispatch terminal writer handoff": "timeout.TransferWriterScope(deferredWriterScope);",
        "CAD dispatch no-replay truth": "completion continues without replay",
        "transactional native entities": "transaction.Commit()",
        "mutation audit": "mcp-agent-audit.jsonl",
        "bounded audit rotation": "MaxAuditBytes",
        "allowlisted CAD commands": "AllowedCadCommands",
        "QS3D command allowlist": '"^QS3D[A-Za-z0-9_]*$"',
        "script control-char rejection": "forbidden control characters",
        "script blank-terminator anti-chain guard": "inputs may not continue after a blank command terminator",
        "script known-command anti-injection guard": "inputs may not inject another CAD/QS3D command",
        "privacy-safe document basename": "SafeDocumentName",
        "privacy-safe DWG sysvar": 'Path.GetFileName(text)',
        "system variable allowlist": "ReadableSystemVariables",
    })

    for command in (
        '"HATCH"', '"DIMLINEAR"', '"BLOCK"', '"XREF"', '"LAYOUT"',
        '"MVIEW"', '"PLOT"', '"SAVEAS"', '"OPEN"', '"UNDO"',
    ):
        if command not in runtime:
            errors.append(f"MCP runtime missing full-drawing command capability: {command}")

    mutation_routes = (
        'case "cad_create_line": return Mutation(',
        'case "cad_create_circle": return Mutation(',
        'case "cad_create_arc": return Mutation(',
        'case "cad_create_polyline": return Mutation(',
        'case "cad_create_text": return Mutation(',
        'case "cad_create_mtext": return Mutation(',
        'case "cad_entity_transform": return Mutation(',
        'case "cad_entity_delete": return Mutation(',
        'case "cad_entity_set_layer": return Mutation(',
        'case "cad_layer": return Mutation(',
        'case "qs3d_run_command": return Mutation(',
        'case "cad_ui_click": return Mutation(',
        'case "cad_ui_type": return Mutation(',
        'case "cad_ui_key": return Mutation(',
    )
    for route in mutation_routes:
        if route not in runtime:
            errors.append(f"MCP runtime mutation bypasses confirmation/stop gate: {route}")

    command_sequence_start = runtime.find('case "cad_command_sequence":')
    command_sequence_end = runtime.find('case "qs3d_run_command"', command_sequence_start)
    if command_sequence_start < 0 or command_sequence_end <= command_sequence_start:
        errors.append("cannot inspect MCP cad_command_sequence mutation route")
    else:
        command_sequence_route = runtime[command_sequence_start:command_sequence_end]
        for token in (
            "return Mutation(args, tool, () =>",
            "McpCadDirectModelRuntime.CanHandleCadCommandSequence(args)",
            "McpCadDirectModelRuntime.CallCadCommandSequence(args)",
            "RunCadCommandSequence(args)",
        ):
            if token not in command_sequence_route:
                errors.append(f"MCP cad_command_sequence route lost mutation/direct-fallback contract: {token}")

    if "public int Cancelled;" in runtime or "Volatile.Read(ref item.Cancelled) == 0" in runtime:
        errors.append("CAD dispatch still uses a check-then-act cancellation flag")
    if "item.Abandoned" in runtime or "public int Abandoned" in runtime:
        errors.append("CAD dispatch restored the racy abandoned completion-handle handoff")
    if "item.Done.Wait();" in runtime:
        errors.append("CAD dispatch restored an unbounded started-work completion wait")
    if "databaseHandleSeed" in runtime:
        errors.append("MCP runtime leaks database handle seed")
    if '"fileName"' in runtime and "document.Database.Filename" in runtime:
        errors.append("MCP runtime returns local drawing path instead of privacy-safe document metadata")

    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start(", "mouse_event("):
        if forbidden in server:
            errors.append(f"forbidden execution surface in MCP network transport: {forbidden}")
    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start(", "mouse_event("):
        if forbidden in runtime:
            errors.append(f"forbidden shell/process/legacy-input surface in MCP CAD runtime: {forbidden}")

    require_tokens(errors, top_level_json, "MCP JSON helper", {
        "bounded top-level property scanner": "internal static bool TryFindPropertyValue(",
        "top-level string extraction": "internal static string ExtractString(",
        "top-level boolean extraction": "internal static bool ExtractBoolean(",
        "top-level integer extraction": "internal static bool TryExtractInteger(",
        "top-level double extraction": "internal static bool TryExtractDouble(",
        "top-level presence check": "internal static bool HasProperty(",
        "duplicate top-level target rejection": "duplicate top-level JSON property",
    })

    if "McpCloudflareTunnelManager.StopForHostShutdown();" not in account:
        errors.append("browser-login named tunnel does not stop fallback tunnel before start")
    if fallback.count("McpCloudflareAccountTunnelManager.StopForHostShutdown();") < 2:
        errors.append("token/Quick fallback does not stop browser-login tunnel before start")
    if "BeginOutputReadLine" not in account or "BeginErrorReadLine" not in account or "process.WaitForExit();" not in account:
        errors.append("browser-login cloudflared command output is not asynchronously drained before disposal")
    if "ingress:" not in account or "http_status:404" not in account:
        errors.append("browser-login named tunnel lacks hostname-scoped ingress + fail-closed 404 rule")
    if "EnableRaisingEvents = false" not in fallback or "process.EnableRaisingEvents = true;" not in fallback or "return IsRunning;" not in fallback:
        errors.append("fallback cloudflared ownership is not established before exit events are enabled")
    if "private static void HandleProcessExit(Process process)" not in fallback:
        errors.append("fallback cloudflared lacks idempotent owned-process exit cleanup")

    center_required = (
        '[CommandMethod("QS3DMCPAGENTCENTER"',
        "McpCloudflaredBootstrapper.BeginInstall",
        "McpCloudflareAccountSetupWindow",
        "McpPublicEndpointResolver.Resolve()",
        "RunReadOnlySelfTest",
        'InvokeControlTool("cad_agent_stop"',
        'InvokeControlTool("cad_cancel_command"',
        "McpDesktopControlSession.ResumeFromLocalUser()",
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

    if '<Compile Remove="McpEmbeddedServer.cs" />' not in v25_project:
        errors.append("V25 build does not exclude legacy monolithic McpEmbeddedServer.cs")
    for line in v25_project.splitlines():
        stripped = line.strip()
        if '<Compile Remove="' in stripped and "Mcp" in stripped and 'McpEmbeddedServer.cs' not in stripped:
            errors.append(f"V25 build excludes active MCP source: {stripped}")

    shared_compile_prefix = r'<Compile Include="..\QS3D.BricsCAD.V25\**\*.cs"'
    shared_start = v26_project.find(shared_compile_prefix)
    if shared_start < 0:
        errors.append("V26 project no longer shares the V25 adapter source glob")
    else:
        tag_end = v26_project.find(">", shared_start)
        exclude_marker = 'Exclude="'
        exclude_start = v26_project.find(exclude_marker, shared_start)
        if exclude_start < 0 or (tag_end >= 0 and exclude_start > tag_end):
            errors.append("V26 shared-source Compile item has no explicit exclusion contract")
        else:
            exclude_start += len(exclude_marker)
            exclude_end = v26_project.find('"', exclude_start)
            if exclude_end < 0:
                errors.append("V26 shared-source Compile exclusion contract is malformed")
            else:
                exclusions = {
                    item.strip().lower()
                    for item in v26_project[exclude_start:exclude_end].split(";")
                    if item.strip()
                }
                legacy_path = r"..\QS3D.BricsCAD.V25\McpEmbeddedServer.cs".lower()
                if legacy_path not in exclusions:
                    errors.append("V26 shared-source build does not exclude legacy monolithic McpEmbeddedServer.cs")
                for exclusion in exclusions:
                    if "mcp" in exclusion and exclusion != legacy_path:
                        errors.append(
                            "V26 shared-source build excludes non-legacy MCP source/pattern: " + exclusion
                        )

    if errors:
        print("Full MCP CAD agent preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: modular embedded MCP v2 owns bounded authenticated Streamable HTTP/session routing "
        "and structured tool results; McpCadAgentRuntime owns privacy-safe transactional CAD API "
        "operations, arc/MText/entity-layer/sysvar additions, atomic timeout semantics, bounded "
        "allowlisted native command workflows, BricsCAD-process-only SendInput, emergency recovery "
        "and rotating audit evidence. Click-first verified Cloudflare onboarding and canonical HTTPS "
        "public endpoint resolution remain wired without arbitrary network-exposed shell execution."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())