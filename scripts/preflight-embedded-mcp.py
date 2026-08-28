#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServer.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpConnectorRibbonCommands.cs"
TOKEN_ONBOARDING = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareOnboarding.cs"
ACCOUNT_ONBOARDING = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
OVERRIDE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "McpRibbonCommandOverride.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"


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
    override = read(OVERRIDE, errors)
    coordinator = read(COORDINATOR, errors)
    plugin = read(PLUGIN, errors)

    require(server, "internal static class McpEmbeddedServer", errors, "embedded server")
    require(server, "new TcpListener(IPAddress.Loopback, Port)", errors, "loopback-only listener")
    require(server, 'TokenFileName = "mcp-bearer-token.txt"', errors, "local bearer token file")
    require(server, 'BearerEnvironment = "QS3D_MCP_BEARER_TOKEN"', errors, "bearer environment override")
    require(server, 'PublicUrlEnvironment = "QS3D_MCP_PUBLIC_URL"', errors, "public URL display override")
    require(server, "ExecuteInApplicationContext", errors, "BricsCAD application-context dispatch")
    require(server, "ManualResetEventSlim", errors, "bounded CAD dispatch wait")

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

    expected_routes = {
        'Prefix + "MCP_SETTINGS"': "QS3DMCPACCOUNTSETUP",
        'Prefix + "MCP_DOCS"': "QS3DMCPDOCSHTTP",
        'Prefix + "AI_DASHBOARD"': "QS3DAIDASHBOARDHTTP",
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
    ):
        require(commands, f'[CommandMethod("{command}"', errors, f"CommandMethod {command}")

    # Default UX: browser authentication, then hidden cloudflared provisioning. No token/pw copy.
    require(account_onboarding, '[CommandMethod("QS3DMCPACCOUNTSETUP"', errors, "browser-login setup command")
    require(account_onboarding, "McpCloudflareAccountSetupWindow", errors, "browser-login setup window")
    require(account_onboarding, '"tunnel login"', errors, "Cloudflare browser login")
    require(account_onboarding, '"tunnel create " + TunnelName', errors, "automatic tunnel creation")
    require(account_onboarding, '"tunnel route dns "', errors, "automatic DNS route")
    require(account_onboarding, '"credentials-file: \\""', errors, "local tunnel config credentials")
    require(account_onboarding, '"url: " + OriginUrl', errors, "local MCP origin route")
    require(account_onboarding, "Cloudflare login:", errors, "visible authentication status")
    require(account_onboarding, "không hỏi và không lưu mật khẩu Cloudflare", errors, "provider-owned password entry")
    require(account_onboarding, "OpenChatGpt", errors, "ChatGPT browser handoff")
    require(account_onboarding, "StartQuickTunnel", errors, "one-click Quick Tunnel fallback")
    forbid(account_onboarding, "powershell.exe", errors, "PowerShell setup dependency")
    forbid(account_onboarding, "cmd.exe", errors, "cmd setup dependency")

    # Advanced fallback remains available for users who prefer dashboard-issued remote tokens.
    require(token_onboarding, "ProtectedData.Protect", errors, "DPAPI fallback tunnel token protection")
    require(token_onboarding, "DataProtectionScope.CurrentUser", errors, "per-Windows-user fallback secret scope")
    require(token_onboarding, 'startInfo.EnvironmentVariables["TUNNEL_TOKEN"]', errors, "fallback token outside process command line")
    require(token_onboarding, "trycloudflare.com", errors, "Quick Tunnel test URL discovery")
    forbid(token_onboarding, "powershell.exe", errors, "fallback PowerShell setup dependency")
    forbid(token_onboarding, "cmd.exe", errors, "fallback cmd setup dependency")

    # The probe methods live inside escaped C# JSON string literals.
    for source_token, label in (
        ('\\"method\\":\\"initialize\\"', "Ribbon protocol initialize probe"),
        ('\\"method\\":\\"notifications/initialized\\"', "Ribbon protocol initialized notification"),
        ('\\"method\\":\\"tools/list\\"', "Ribbon protocol tools/list probe"),
    ):
        require(commands, source_token, errors, label)
    require(commands, 'request.Headers["MCP-Protocol-Version"]', errors, "Ribbon protocol version header")
    require(commands, 'response.Headers["Mcp-Session-Id"]', errors, "Ribbon MCP session capture")
    require(commands, "McpEmbeddedServer.GetBearerToken()", errors, "Ribbon bearer authentication")
    require(commands, "cloudflared tunnel --url http://127.0.0.1:8765", errors, "Cloudflare tunnel guide")
    require(commands, "no second repository", errors, "single-repository guide")

    if errors:
        print("Embedded MCP preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: QS3D embeds authenticated MCP and provides a zero-shell browser-login Cloudflare "
        "wizard that creates/reuses a named tunnel, routes DNS, persists config, auto-starts with "
        "BricsCAD, keeps provider passwords out of QS3D, and retains Quick Tunnel as test fallback."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
