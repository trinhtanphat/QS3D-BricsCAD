#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
SERVER = V25 / "McpEmbeddedServerV2.cs"
FALLBACK = V25 / "McpCloudflareOnboarding.cs"
ACCOUNT = V25 / "McpCloudflareAccountOnboarding.cs"


def require_tokens(errors: list[str], text: str, prefix: str, required: dict[str, str]) -> None:
    for label, token in required.items():
        if token not in text:
            errors.append(f"{prefix} missing {label}: {token}")


def main() -> int:
    for path in (SERVER, FALLBACK, ACCOUNT):
        if not path.is_file():
            print("ERROR: missing", path.relative_to(ROOT))
            return 1

    server = SERVER.read_text(encoding="utf-8")
    fallback = FALLBACK.read_text(encoding="utf-8")
    account = ACCOUNT.read_text(encoding="utf-8")
    errors: list[str] = []

    require_tokens(errors, server, "MCP server", {
        "preferred loopback port": "PreferredPort = 8765",
        "bounded fallback attempts": "MaxPortAttempts",
        "selected port state": "_boundPort",
        "preferred-port state": "IsPreferredPort",
        "bounded listener helper": "StartLoopbackListener",
        "address-in-use recovery": "SocketError.AddressAlreadyInUse",
        "loopback-only listener": "new TcpListener(IPAddress.Loopback, port)",
        "native screenshot dispatch": 'string.Equals(tool, "desktop_screenshot", StringComparison.Ordinal)',
        "native screenshot result helper": "ScreenshotToolSuccess",
        "screenshot source field": 'ExtractString(raw, "pngBase64")',
        "native image content": '\\"type\\":\\"image\\"',
        "native image data": '\\"data\\":\\"',
        "native image MIME": '\\"mimeType\\":\\"image/png\\"',
        "bounded screenshot metadata": '\\"structuredContent\\":{\\"data\\":',
        "metadata object rebuild": 'var metadata = "{\\"scope\\":\\""',
    })

    if "return ToolSuccess(McpCadAgentRuntime.Call(tool, arguments));" in server:
        errors.append("MCP server still sends every runtime result through text-only ToolSuccess")
    if server.count('"pngBase64"') > 1:
        errors.append("MCP screenshot response appears to duplicate pngBase64 instead of emitting image data once")

    require_tokens(errors, fallback, "Quick/token tunnel", {
        "dynamic origin": "McpEmbeddedServer.Endpoint.GetLeftPart(UriPartial.Authority)",
        "dynamic host header": "McpEmbeddedServer.Endpoint.Authority",
        "token fallback-port guard": "McpEmbeddedServer.IsPreferredPort",
    })
    if 'private const string OriginUrl = "http://127.0.0.1:8765"' in fallback:
        errors.append("Quick Tunnel origin is still pinned to port 8765")
    if "--http-host-header 127.0.0.1:8765" in fallback:
        errors.append("Quick Tunnel host header is still pinned to port 8765")

    require_tokens(errors, account, "Account Named Tunnel", {
        "dynamic origin": "McpEmbeddedServer.Endpoint.GetLeftPart(UriPartial.Authority)",
        "canonical config uses selected origin": '"    service: " + OriginUrl',
    })
    if 'private const string OriginUrl = "http://127.0.0.1:8765"' in account:
        errors.append("Account Named Tunnel origin is still pinned to port 8765")

    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    print("PASS MCP port recovery + native screenshot image result contract")
    return 0


if __name__ == "__main__":
    sys.exit(main())
