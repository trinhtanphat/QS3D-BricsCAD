#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServer.cs"
ACCOUNT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
FALLBACK = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareOnboarding.cs"
CONNECTOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpConnectorRibbonCommands.cs"
RESOLVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpPublicEndpointResolver.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def main() -> int:
    errors: list[str] = []
    try:
        server = read(SERVER)
        account = read(ACCOUNT)
        fallback = read(FALLBACK)
        connector = read(CONNECTOR)
        resolver = read(RESOLVER)
        plugin = read(PLUGIN)
    except FileNotFoundError as exc:
        print("ERROR: missing", exc)
        return 1

    required = {
        "resolver provider precedence": (resolver, "McpCloudflareAccountTunnelManager.PublicMcpUrl"),
        "resolver quick/token precedence": (resolver, "McpCloudflareTunnelManager.PublicMcpUrl"),
        "HTTPS-only public endpoint": (resolver, "Uri.UriSchemeHttps"),
        "loopback public rejection": (resolver, "uri.IsLoopback"),
        "literal public-address validation": (resolver, "IPAddress.TryParse(uri.Host"),
        "private/link-local literal rejection": (resolver, "IsPrivateOrLocalAddress"),
        "configured fallback snapshot": (resolver, "ConfiguredEnvironmentFallback"),
        "provider publication isolated from fallback": (resolver, "NormalizeCandidate(ConfiguredEnvironmentFallback)"),
        "canonical MCP path": (resolver, 'path = "/mcp"'),
        "process endpoint synchronization": (resolver, "EnvironmentVariableTarget.Process"),
        "startup endpoint publication": (plugin, "McpPublicEndpointResolver.Resolve()"),
        "connector resolver use": (connector, "McpPublicEndpointResolver.Resolve()"),
        "copy URL command": (connector, 'CommandMethod("QS3DMCPCOPYURL"'),
        "copy token command": (connector, 'CommandMethod("QS3DMCPCOPYTOKEN"'),
        "copy config command": (connector, 'CommandMethod("QS3DMCPCOPYCONFIG"'),
        "legacy settings hide bearer value": (connector, "Bearer token: [hidden; use QS3DMCPCOPYTOKEN]"),
        "generated guide starts from Agent Center": (connector, "1. Open MCP Agent Center from TOOL > MCP (AI)."),
        "generated guide uses click-first installer": (connector, "2. Click the automatic cloudflared install/update button if needed."),
        "live Cloudflare tunnel list": (account, 'RunCommand(executable, "tunnel list"'),
        "exact tunnel-name comparison": (account, "string.Equals(parts[1], name, StringComparison.OrdinalIgnoreCase)"),
        "missing tunnel credential fail-closed": (account, "máy này thiếu credential"),
        "DNS conflict fail-closed": (account, "QS3D không tự bỏ qua xung đột DNS"),
        "hostname-scoped ingress": (account, '"ingress:\\r\\n"'),
        "account setup verified one-click installer": (account, "McpCloudflaredBootstrapper.BeginInstall"),
        "Quick Tunnel URL polling": (account, "DispatcherTimer"),
        "Quick Tunnel bounded poll": (account, "_quickUrlPollTicks >= 20"),
        "named tunnel output bound to process owner": (account, "HandleRunLine(process, args.Data, false)"),
        "named stale-process output rejection": (account, "if (!ReferenceEquals(_process, process)) return;"),
        "fallback process owner before exit events": (fallback, "EnableRaisingEvents = false"),
        "fallback process exit cleanup": (fallback, "HandleProcessExit(Process process)"),
        "fallback output bound to process owner": (fallback, "HandleLine(process, args.Data, discoverQuickUrl)"),
        "fallback stale-process output rejection": (fallback, "if (!ReferenceEquals(_process, process)) return;"),
        "foreground ESC fallback": (server, "TrySendEscapeFallback()"),
        "emergency-stop latch": (server, "_automationStopped = true"),
        "CAD dispatch timeout cancellation": (server, "Interlocked.Exchange(ref item.Cancelled, 1)"),
        "bounded MCP sessions": (server, "MaxSessions"),
        "bounded MCP clients": (server, "MaxConcurrentClients"),
    }
    for label, (text, token) in required.items():
        if token not in text:
            errors.append(f"missing {label}: {token}")

    if 'IndexOf("already exists"' in account:
        errors.append("Cloudflare DNS conflict must not be silently accepted via 'already exists'")
    if '"Bearer token: " + McpEmbeddedServer.GetBearerToken()' in connector:
        errors.append("legacy settings must not render the raw bearer token; use explicit copy action")
    if '"1. Run QS3DMCPACCOUNTSETUP.' in connector:
        errors.append("generated guide must not make a typed BricsCAD setup command the default path")

    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start("):
        if forbidden in server:
            errors.append(f"network MCP server exposes forbidden OS execution token: {forbidden}")

    if errors:
        print("Production MCP hardening preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: MCP production source uses one validated HTTPS endpoint resolver, isolated user "
        "fallback state, live/exact Cloudflare tunnel identity checks, fail-closed DNS conflict "
        "handling, owner-bound named/Quick Tunnel output, verified click-first redacted onboarding, "
        "bounded network/session surfaces and BricsCAD-confined emergency recovery."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
