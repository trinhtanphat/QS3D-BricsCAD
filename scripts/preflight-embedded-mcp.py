#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServer.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpConnectorRibbonCommands.cs"
TOKEN_ONBOARDING = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareOnboarding.cs"
ACCOUNT_ONBOARDING = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
AGENT_CENTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpAgentControlCenter.cs"
BOOTSTRAPPER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflaredBootstrapper.cs"
PUBLIC_ENDPOINT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPublicEndpointResolver.cs"
OVERRIDE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "McpRibbonCommandOverride.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
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
    commands = read(COMMANDS, errors)
    token_onboarding = read(TOKEN_ONBOARDING, errors)
    account_onboarding = read(ACCOUNT_ONBOARDING, errors)
    agent_center = read(AGENT_CENTER, errors)
    bootstrapper = read(BOOTSTRAPPER, errors)
    public_endpoint = read(PUBLIC_ENDPOINT, errors)
    override = read(OVERRIDE, errors)
    coordinator = read(COORDINATOR, errors)
    plugin = read(PLUGIN, errors)
    integration_doc = read(INTEGRATION_DOC, errors)

    require(server, "internal static class McpEmbeddedServer", errors, "embedded server")
    require(server, "new TcpListener(IPAddress.Loopback, Port)", errors, "loopback-only listener")
    require(server, 'TokenFileName = "mcp-bearer-token.txt"', errors, "local bearer token file")
    require(server, 'BearerEnvironment = "QS3D_MCP_BEARER_TOKEN"', errors, "bearer environment override")
    require(server, 'PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL"', errors, "public URL display override")
    require(server, "ExecuteInApplicationContext", errors, "BricsCAD application-context dispatch")
    require(server, "ManualResetEventSlim", errors, "bounded CAD dispatch wait")
    require(server, "ConstantTimeEquals", errors, "constant-time bearer comparison")
    require(server, "MaxConcurrentClients", errors, "bounded concurrent MCP clients")
    require(server, "MaxSessions", errors, "bounded MCP sessions")
    require(server, "Transfer-Encoding is not supported", errors, "HTTP request framing guard")
    require(server, 'request.Method == "DELETE"', errors, "MCP session termination")

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

    require(server, "confirmMutation=true", errors, "explicit mutation gate")
    require(server, '"^QS3D[A-Za-z0-9_]*$"', errors, "QS3D-only command allowlist")
    require(server, "SendStringToExecute(command + \"\\n\"", errors, "guarded QS3D command dispatch")
    forbid(server, "Process.Start(", errors, "arbitrary process launch from network server")
    forbid(server, "PowerShell", errors, "arbitrary PowerShell surface")
    forbid(server, "cmd.exe", errors, "arbitrary cmd surface")
    forbid(server, "mouse_event(", errors, "legacy global mouse injection")

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
    require(plugin, "McpCloudflareAccountTunnelManager.TryAutoStart();", errors, "browser-auth named-tunnel auto-start")
    require(plugin, "TryCleanup(McpCloudflareAccountTunnelManager.StopForHostShutdown);", errors, "browser-auth tunnel teardown")
    require(plugin, "TryCleanup(McpCloudflareTunnelManager.StopForHostShutdown);", errors, "quick/token tunnel teardown")
    require(plugin, "TryCleanup(McpEmbeddedServer.Stop);", errors, "embedded MCP teardown")
    require(plugin, 'ReportOptionalStartupFailure("MCP server", ex)', errors, "fail-soft MCP startup")

    for command in (
        "QS3DMCPSETTINGSHTTP",
        "QS3DMCPDOCSHTTP",
        "QS3DMCPCHECKHTTP",
        "QS3DAIDASHBOARDHTTP",
        "QS3DMCPSTART",
        "QS3DMCPSTOP",
        "QS3DMCPCOPYURL",
        "QS3DMCPCOPYTOKEN",
        "QS3DMCPCOPYCONFIG",
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
    require(agent_center, 'InvokeControlTool("cad_agent_resume"', errors, "click explicit resume")
    require(agent_center, "OpenChatGpt", errors, "ChatGPT browser handoff")
    require(agent_center, "ThreadPool.QueueUserWorkItem", errors, "non-blocking Agent Center MCP operation")
    require(agent_center, "Interlocked.CompareExchange(ref _localOperationActive", errors, "serialized observation self-test")
    forbid(agent_center, "powershell.exe", errors, "PowerShell user workflow")
    forbid(agent_center, "cmd.exe", errors, "cmd user workflow")

    # Public URLs are normalized once: HTTPS only, public literal address, canonical /mcp.
    require(public_endpoint, "internal static class McpPublicEndpointResolver", errors, "public endpoint resolver")
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
    forbid(token_onboarding, "powershell.exe", errors, "fallback PowerShell setup dependency")
    forbid(token_onboarding, "cmd.exe", errors, "fallback cmd setup dependency")

    # The local Ribbon probe exercises a real MCP lifecycle including one tools/call and cleanup.
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

    # The user-facing integration note must match the canonical issue-4352 click-first contract.
    require(integration_doc, "Status: SOURCE_READY / PENDING_LOCAL", errors, "integration doc qualification status")
    require(integration_doc, "Canonical issue: #4352", errors, "integration doc canonical issue")
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
        "PASS: QS3D embeds authenticated MCP plus a unified click-first Agent Center with "
        "verified one-click cloudflared bootstrap, provider-browser login, named/Quick tunnel "
        "management, a single HTTPS /mcp endpoint resolver, ChatGPT copy/open actions, "
        "read-only MCP self-test and emergency controls without PowerShell/CMD user setup."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
