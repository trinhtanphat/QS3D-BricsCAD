#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
SERVER = V25 / "McpEmbeddedServerV2.cs"
RUNTIME = V25 / "McpCadAgentRuntime.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"
LEGACY_SERVER = V25 / "McpEmbeddedServer.cs"
COMMANDS = V25 / "McpConnectorRibbonCommands.cs"
TOKEN_ONBOARDING = V25 / "McpCloudflareOnboarding.cs"
ACCOUNT_ONBOARDING = V25 / "McpCloudflareAccountOnboarding.cs"
AGENT_CENTER = V25 / "McpAgentControlCenter.cs"
BOOTSTRAPPER = V25 / "McpCloudflaredBootstrapper.cs"
PUBLIC_ENDPOINT = V25 / "McpPublicEndpointResolver.cs"
OVERRIDE = V25 / "Ribbon" / "McpRibbonCommandOverride.cs"
COORDINATOR = V25 / "Ribbon" / "RibbonInitializationCoordinator.cs"
PLUGIN = V25 / "PluginEntry.cs"
V25_PROJECT = V25 / "QS3D.BricsCAD.V25.csproj"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"
INTEGRATION_DOC = ROOT / "docs" / "CHATGPT-MCP-INTEGRATION.md"


def require(text: str, token: str, errors: list[str], label: str) -> None:
    if token not in text:
        errors.append(f"missing {label}: {token}")


def forbid(text: str, token: str, errors: list[str], label: str) -> None:
    if token in text:
        errors.append(f"forbidden {label}: {token}")


def read(path: Path, errors: list[str]) -> str:
    if not path.is_file():
        errors.append(f"missing file: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def main() -> int:
    errors: list[str] = []
    server = read(SERVER, errors)
    runtime = read(RUNTIME, errors)
    domain = read(DOMAIN, errors)
    legacy_server = read(LEGACY_SERVER, errors)
    commands = read(COMMANDS, errors)
    token_onboarding = read(TOKEN_ONBOARDING, errors)
    account_onboarding = read(ACCOUNT_ONBOARDING, errors)
    agent_center = read(AGENT_CENTER, errors)
    bootstrapper = read(BOOTSTRAPPER, errors)
    public_endpoint = read(PUBLIC_ENDPOINT, errors)
    override = read(OVERRIDE, errors)
    coordinator = read(COORDINATOR, errors)
    plugin = read(PLUGIN, errors)
    v25_project = read(V25_PROJECT, errors)
    v26_project = read(V26_PROJECT, errors)
    integration_doc = read(INTEGRATION_DOC, errors)

    # Active transport is the modular V2 source. The monolith may stay in history only.
    require(server, "internal static class McpEmbeddedServer", errors, "embedded V2 server")
    require(server, "new TcpListener(IPAddress.Loopback, port)", errors, "loopback-only bounded listener")
    require(server, "SocketError.AddressAlreadyInUse", errors, "occupied-port recovery")
    require(server, "MaxPortAttempts", errors, "bounded port recovery attempts")
    require(server, 'TokenFileName = "mcp-bearer-token.txt"', errors, "local bearer token file")
    require(server, 'BearerEnvironment = "QS3D_MCP_BEARER_TOKEN"', errors, "bearer environment override")
    require(server, "ConstantTimeEquals", errors, "constant-time bearer comparison")
    require(server, "MaxConcurrentClients", errors, "bounded concurrent MCP clients")
    require(server, "MaxSessions", errors, "bounded MCP sessions")
    require(server, "Transfer-Encoding is not supported", errors, "HTTP request framing guard")
    require(server, "IsJsonContentType", errors, "exact JSON media-type parser")
    forbid(server, 'contentType.StartsWith("application/json"', errors, "JSON media-type prefix acceptance")
    require(server, 'request.Method == "DELETE"', errors, "MCP session termination")
    require(server, "McpCadAgentRuntime.Call(tool, arguments)", errors, "CAD runtime delegation")

    for token in (
        '"initialize"',
        '"notifications/initialized"',
        '"tools/list"',
        '"tools/call"',
        '"Mcp-Session-Id"',
        '"MCP-Protocol-Version"',
    ):
        require(server, token, errors, "MCP protocol surface")

    for tool in (
        "connector_info",
        "qs3d_status",
        "cad_active_document",
        "cad_selection",
        "cad_database_snapshot",
        "cad_entity_inspect",
        "qs3d_run_command",
        "cad_cancel_command",
    ):
        require(server, f'"{tool}"', errors, f"MCP tool {tool}")

    # CAD/editor behavior remains behind McpCadAgentRuntime; QS3D business command
    # validation/execution is owned by the dedicated domain runtime after capability split.
    require(runtime, "ExecuteInApplicationContext", errors, "BricsCAD application-context dispatch")
    require(runtime, "ManualResetEventSlim", errors, "bounded CAD dispatch wait")
    require(runtime, 'McpTopLevelJson.ExtractBoolean(body, "confirmMutation")', errors, "top-level mutation gate")
    require(runtime, '"^QS3D[A-Za-z0-9_]*$"', errors, "QS3D-only command allowlist contract")
    require(runtime, 'case "qs3d_run_command": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));', errors, "QS3D domain delegation")
    require(domain, "Regex.IsMatch(command, McpCadAgentRuntime.Qs3dCommandPattern", errors, "guarded QS3D command validation")
    require(domain, "SendStringToExecute(command + \"\\n\"", errors, "guarded QS3D command dispatch")
    require(domain, "McpCadAgentRuntime.EnsureCurrentMutationRunning();", errors, "QS3D mutation epoch recheck")
    require(runtime, "CadWorkQueued = 0", errors, "CAD dispatch queued state")
    require(runtime, "CadWorkRunning = 1", errors, "CAD dispatch running state")
    require(runtime, "CadWorkCancelledBeforeStart = 2", errors, "CAD dispatch cancelled-before-start state")
    require(runtime, "Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued)", errors, "atomic CAD start claim")
    require(runtime, "Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued)", errors, "atomic timeout cancellation")
    require(runtime, "completion is uncertain", errors, "timeout completion uncertainty")
    require(runtime, "Do not retry automatically", errors, "timeout no-auto-retry contract")

    for text, surface in ((server, "network MCP transport"), (runtime, "CAD runtime"), (domain, "QS3D domain runtime")):
        forbid(text, "Process.Start(", errors, f"arbitrary process launch from {surface}")
        forbid(text, "powershell.exe", errors, f"PowerShell surface in {surface}")
        forbid(text, "cmd.exe", errors, f"cmd surface in {surface}")
        forbid(text, "mouse_event(", errors, f"legacy global mouse injection in {surface}")

    require(v25_project, '<Compile Remove="McpEmbeddedServer.cs" />', errors, "V25 legacy-server exclusion")
    require(v26_project, "..\\QS3D.BricsCAD.V25\\McpEmbeddedServer.cs", errors, "V26 legacy-server exclusion")
    if not legacy_server:
        errors.append("legacy MCP source unexpectedly missing; historical source should remain excluded, not silently renamed")

    expected_routes = {
        'Prefix + "MCP_SETTINGS"': "QS3DMCPAGENTCENTER",
        'Prefix + "MCP_DOCS"': "QS3DMCPDOCSHTTP",
        'Prefix + "AI_DASHBOARD"': "QS3DMCPAGENTCENTER",
        'Prefix + "MCP_CONNECTION"': "QS3DMCPCHECKHTTP",
    }
    for button, command in expected_routes.items():
        require(override, f'[{button}] = "{command}"', errors, f"embedded ribbon route {button}")

    binder_index = coordinator.find("BltToolRibbonCommandBinder.TryInitialize()")
    override_index = coordinator.find("McpRibbonCommandOverride.TryInitialize()")
    fallback_index = coordinator.find("RibbonCommandParameterFallback.TryInitialize()")
    if min(binder_index, override_index, fallback_index) < 0:
        errors.append("cannot verify MCP ribbon override ordering")
    elif not (binder_index < override_index < fallback_index):
        errors.append("MCP ribbon override must run after legacy TOOL binder and before command fallback")
    require(coordinator, "McpRibbonCommandOverride.Reset()", errors, "ribbon override teardown")

    require(plugin, "McpEmbeddedServer.Start();", errors, "embedded MCP startup")
    require(plugin, "McpTransportCoordinator.TryAutoStartPreferred();", errors, "preferred MCP transport auto-start")
    require(plugin, "TryCleanup(McpTransportCoordinator.StopAllForHostShutdown);", errors, "provider-aware MCP transport teardown")
    require(plugin, "TryCleanup(McpEmbeddedServer.Stop);", errors, "embedded MCP teardown")
    require(plugin, 'ReportOptionalStartupFailure("MCP server", ex)', errors, "fail-soft MCP startup")
    require(plugin, 'ReportOptionalStartupFailure("MCP tunnel autostart", ex)', errors, "fail-soft preferred MCP tunnel autostart")
    require(plugin, "McpTransportAgentCenterAugmenter.Start();", errors, "transport Agent Center startup")
    require(plugin, 'ReportOptionalStartupFailure("MCP transport Agent Center", ex)', errors, "fail-soft transport Agent Center startup")

    for command in (
        "QS3DMCPSETTINGSHTTP", "QS3DMCPDOCSHTTP", "QS3DMCPCHECKHTTP", "QS3DAIDASHBOARDHTTP",
        "QS3DMCPSTART", "QS3DMCPSTOP", "QS3DMCPCOPYURL", "QS3DMCPCOPYTOKEN", "QS3DMCPCOPYCONFIG",
    ):
        require(commands, f'[CommandMethod("{command}"', errors, f"CommandMethod {command}")

    # Default UX: one unified click-first Agent Center. Provider credentials stay in browser.
    require(agent_center, '[CommandMethod("QS3DMCPAGENTCENTER"', errors, "unified Agent Center command")
    require(agent_center, "McpAgentControlCenterWindow", errors, "unified Agent Center window")
    require(agent_center, "McpCloudflaredBootstrapper.BeginInstall", errors, "one-click verified cloudflared install")
    require(agent_center, "McpPublicEndpointResolver.Resolve()", errors, "single validated public endpoint")
    require(agent_center, "RunReadOnlySelfTest", errors, "read-only end-to-end self-test")
    require(agent_center, 'InvokeControlTool("cad_agent_stop"', errors, "click emergency stop")
    require(agent_center, 'InvokeControlTool("cad_cancel_command"', errors, "click command cancel")
    require(agent_center, "McpDesktopControlSession.ResumeFromLocalUser()", errors, "local-only explicit resume")
    require(agent_center, "OpenChatGpt", errors, "ChatGPT browser handoff")
    require(agent_center, "ThreadPool.QueueUserWorkItem", errors, "non-blocking Agent Center MCP operation")
    require(agent_center, "Interlocked.CompareExchange(ref _localOperationActive", errors, "serialized observation self-test")
    require(agent_center, "StartQuickUrlPolling", errors, "Agent Center Quick URL refresh")
    forbid(agent_center, "powershell.exe", errors, "PowerShell user workflow")
    forbid(agent_center, "cmd.exe", errors, "cmd user workflow")

    # Public URLs are normalized once: HTTPS only, public literal address, canonical /mcp.
    require(public_endpoint, 'PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL"', errors, "public URL fallback")
    require(public_endpoint, "Uri.UriSchemeHttps", errors, "HTTPS-only public endpoint")
    require(public_endpoint, "uri.IsLoopback", errors, "loopback public endpoint rejection")
    require(public_endpoint, "IPAddress.TryParse(uri.Host", errors, "literal public-address validation")
    require(public_endpoint, "IsPrivateOrLocalAddress", errors, "private/link-local literal rejection")
    require(public_endpoint, 'path = "/mcp"', errors, "canonical public MCP path")
    require(public_endpoint, "McpCloudflareAccountTunnelManager.PublicMcpUrl", errors, "named-tunnel precedence")
    require(public_endpoint, "McpCloudflareTunnelManager.PublicMcpUrl", errors, "fallback-tunnel precedence")

    # Managed cloudflared bootstrap downloads only the official Windows binary and verifies it.
    require(bootstrapper, "cloudflared-windows-amd64.exe", errors, "official cloudflared Windows binary")
    require(bootstrapper, "WinVerifyTrust", errors, "Authenticode verification")
    require(bootstrapper, "CreateFromSignedFile", errors, "signer certificate inspection")
    require(bootstrapper, "Cloudflare", errors, "Cloudflare signer constraint")
    require(bootstrapper, 'PathEnvironment = "QS3D_CLOUDFLARED_PATH"', errors, "managed cloudflared persistence")
    require(bootstrapper, "backupCreated", errors, "failed replacement rollback")
    require(bootstrapper, "ProviderFlags = 0", errors, "normal Windows signer-chain verification")
    forbid(bootstrapper, "powershell.exe", errors, "PowerShell installer dependency")
    forbid(bootstrapper, "cmd.exe", errors, "cmd installer dependency")

    # Browser-auth setup remains the account-owned named-tunnel implementation behind Agent Center.
    require(account_onboarding, '[CommandMethod("QS3DMCPACCOUNTSETUP"', errors, "browser-login setup command")
    require(account_onboarding, "McpCloudflareAccountSetupWindow", errors, "browser-login setup window")
    require(account_onboarding, '"tunnel login"', errors, "Cloudflare browser login")
    require(account_onboarding, '"tunnel create " + TunnelName', errors, "automatic tunnel creation")
    require(account_onboarding, '"tunnel route dns "', errors, "automatic DNS route")
    require(account_onboarding, "credentials-file:", errors, "local tunnel config credentials")
    require(account_onboarding, '"    service: " + OriginUrl', errors, "hostname-scoped local MCP ingress")
    require(account_onboarding, "http_status:404", errors, "fail-closed unmatched tunnel ingress")
    require(account_onboarding, "WriteCanonicalConfig", errors, "canonical named-tunnel config regeneration")
    require(account_onboarding, "Cloudflare login:", errors, "visible authentication status")
    require(account_onboarding, "không hỏi và không lưu mật khẩu Cloudflare", errors, "provider-owned password entry")
    require(account_onboarding, "OpenChatGpt", errors, "ChatGPT browser handoff")
    require(account_onboarding, "StartQuickTunnel", errors, "one-click Quick Tunnel fallback")
    require(account_onboarding, "BeginOutputReadLine", errors, "async cloudflared stdout drain")
    require(account_onboarding, "BeginErrorReadLine", errors, "async cloudflared stderr drain")
    forbid(account_onboarding, "powershell.exe", errors, "PowerShell setup dependency")
    forbid(account_onboarding, "cmd.exe", errors, "cmd setup dependency")

    # Advanced fallback remains available for dashboard-issued remote tokens.
    require(token_onboarding, "ProtectedData.Protect", errors, "DPAPI fallback tunnel token protection")
    require(token_onboarding, "DataProtectionScope.CurrentUser", errors, "per-Windows-user fallback secret scope")
    require(token_onboarding, 'startInfo.EnvironmentVariables["TUNNEL_TOKEN"]', errors, "fallback token outside process command line")
    require(token_onboarding, "trycloudflare.com", errors, "Quick Tunnel test URL discovery")
    require(token_onboarding, '"tunnel --no-autoupdate --url " + OriginUrl', errors, "Quick Tunnel loopback route")
    require(token_onboarding, "HandleProcessExit(Process process)", errors, "fallback owned-process exit cleanup")
    require(token_onboarding, "_quickBaseUrl = string.Empty;", errors, "ephemeral Quick URL cleanup")
    require(token_onboarding, "McpCloudflaredBootstrapper.BeginInstall", errors, "advanced GUI verified cloudflared install")
    require(token_onboarding, "DispatcherTimer", errors, "advanced GUI asynchronous Quick URL refresh")
    require(token_onboarding, "StartQuickUrlPolling", errors, "advanced GUI Quick URL polling")
    require(token_onboarding, "_quickUrlPollTicks >= 20", errors, "advanced GUI bounded Quick URL polling")
    forbid(token_onboarding, 'Button("Cài Cloudflare Tunnel", (_, __) => McpCloudflareTunnelManager.OpenCloudflaredDownloadPage())', errors, "manual cloudflared install button in advanced GUI")
    forbid(token_onboarding, "powershell.exe", errors, "fallback PowerShell setup dependency")
    forbid(token_onboarding, "cmd.exe", errors, "fallback cmd setup dependency")

    # Local Ribbon probe exercises a real MCP lifecycle including one tools/call and cleanup.
    for source_token, label in (
        ('\\"method\\":\\"initialize\\"', "Ribbon protocol initialize probe"),
        ('\\"method\\":\\"notifications/initialized\\"', "Ribbon protocol initialized notification"),
        ('\\"method\\":\\"tools/list\\"', "Ribbon protocol tools/list probe"),
        ('\\"method\\":\\"tools/call\\"', "Ribbon protocol tools/call probe"),
        ('\\"name\\":\\"connector_info\\"', "Ribbon connector_info call"),
    ):
        require(commands, source_token, errors, label)
    require(commands, 'request.Headers["MCP-Protocol-Version"]', errors, "Ribbon protocol version header")
    require(commands, 'response.Headers["Mcp-Session-Id"]', errors, "Ribbon MCP session capture")
    require(commands, "McpEmbeddedServer.GetBearerToken()", errors, "Ribbon bearer authentication")
    require(commands, 'Send(endpoint, "DELETE"', errors, "Ribbon session cleanup")
    require(commands, "No second repository", errors, "single-repository guide")
    require(commands, "McpPublicEndpointResolver.Resolve()", errors, "commands use validated public endpoint")

    # User-facing integration note must match the current layered click-first contract.
    require(integration_doc, "Status: SOURCE_TRACKED / PENDING_LOCAL", errors, "integration doc qualification status")
    require(integration_doc, "Parent MCP issue: #4352", errors, "integration doc parent MCP issue")
    require(integration_doc, "Desktop/guided-control extension: #4629", errors, "integration doc desktop extension issue")
    require(integration_doc, "QS3DMCPAGENTCENTER", errors, "integration doc Agent Center path")
    require(integration_doc, "McpPublicEndpointResolver", errors, "integration doc public endpoint contract")
    require(integration_doc, "PENDING_LOCAL", errors, "integration doc local qualification boundary")
    forbid(integration_doc, "```powershell", errors, "terminal-first integration instructions")
    forbid(integration_doc, "cloudflared tunnel --url", errors, "manual Quick Tunnel command in end-user integration doc")

    if errors:
        print("Embedded MCP preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: active modular MCP v2 provides bounded authenticated loopback transport with exact "
        "JSON media-type handling; McpCadAgentRuntime owns atomic CAD dispatch/mutation while "
        "McpQs3dDomainRuntime owns bounded QS3D business command execution. Provider-aware MCP "
        "transport startup/teardown, click-first onboarding, canonical HTTPS /mcp resolution, Ribbon "
        "routing and sanitized local protocol controls remain wired while the legacy monolith is "
        "excluded from V25/V26 compilation."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
