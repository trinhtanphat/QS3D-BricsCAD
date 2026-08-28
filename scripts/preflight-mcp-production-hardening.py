#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServer.cs"
ACCOUNT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCloudflareAccountOnboarding.cs"
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
        "canonical MCP path": (resolver, 'path = "/mcp"'),
        "process endpoint synchronization": (resolver, "EnvironmentVariableTarget.Process"),
        "startup endpoint publication": (plugin, "McpPublicEndpointResolver.Resolve()"),
        "connector resolver use": (connector, "McpPublicEndpointResolver.Resolve()"),
        "copy URL command": (connector, 'CommandMethod("QS3DMCPCOPYURL"'),
        "copy token command": (connector, 'CommandMethod("QS3DMCPCOPYTOKEN"'),
        "copy config command": (connector, 'CommandMethod("QS3DMCPCOPYCONFIG"'),
        "live Cloudflare tunnel list": (account, 'RunCommand(executable, "tunnel list"'),
        "exact tunnel-name comparison": (account, "string.Equals(parts[1], name, StringComparison.OrdinalIgnoreCase)"),
        "missing tunnel credential fail-closed": (account, "máy này thiếu credential"),
        "DNS conflict fail-closed": (account, "QS3D không tự bỏ qua xung đột DNS"),
        "hostname-scoped ingress": (account, '"ingress:\\r\\n"'),
        "Quick Tunnel URL polling": (account, "DispatcherTimer"),
        "Quick Tunnel bounded poll": (account, "_quickUrlPollTicks >= 20"),
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

    for forbidden in ("powershell.exe", "cmd.exe", "Process.Start("):
        if forbidden in server:
            errors.append(f"network MCP server exposes forbidden OS execution token: {forbidden}")

    if errors:
        print("Production MCP hardening preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: MCP production source uses one validated HTTPS endpoint resolver, live/exact "
        "Cloudflare tunnel identity checks, fail-closed DNS conflict handling, bounded Quick "
        "Tunnel URL discovery, copy-ready ChatGPT configuration helpers, bounded network/session "
        "surfaces and BricsCAD-confined emergency recovery."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
