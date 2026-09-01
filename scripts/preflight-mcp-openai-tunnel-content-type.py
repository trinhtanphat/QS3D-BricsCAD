#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TUNNEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
SERVER_V2 = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def fail(message: str) -> None:
    print(f"ERROR: OpenAI MCP tunnel Content-Type preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        fail(message)


def main() -> None:
    tunnel = TUNNEL.read_text(encoding="utf-8")
    server = SERVER_V2.read_text(encoding="utf-8")

    start = tunnel.find("        private static void WriteRuntimeConfig(string tunnelId, Uri localEndpoint)")
    end = tunnel.find("        private static bool ProbeReady()", start)
    if start < 0 or end < 0:
        fail("cannot isolate WriteRuntimeConfig")
    config = tunnel[start:end]

    extra_headers = '            yaml.AppendLine("  extra_headers:");'
    authorization = '            yaml.AppendLine("    Authorization: env:" + LocalBearerEnvironment);'
    content_type = '            yaml.AppendLine("    Content-Type: application/json");'
    discovery_headers = '            yaml.AppendLine("  discovery_extra_headers:");'

    require(tunnel, 'private const string LocalBearerEnvironment = "QS3D_TUNNEL_MCP_AUTH";',
            "local bearer environment variable contract changed")
    require(config, extra_headers, "mcp.extra_headers generation is missing")
    require(config, authorization, "Authorization must remain an env reference in mcp.extra_headers")
    require(config, content_type, "forwarded MCP POSTs are not forced to application/json")
    require(config, discovery_headers, "discovery_extra_headers generation is missing")

    if not (config.index(extra_headers) < config.index(authorization) < config.index(content_type) < config.index(discovery_headers)):
        fail("Content-Type must be emitted inside mcp.extra_headers before discovery_extra_headers")
    if config.count(content_type) != 1:
        fail("Content-Type forwarding header must be emitted exactly once")
    if "Authorization: Bearer " in config or "McpEmbeddedServer.GetBearerToken()" in config:
        fail("generated YAML must not persist the real bearer token")

    require(server, 'if (!request.Headers.TryGetValue("Content-Type", out contentType)',
            "embedded MCP must still require Content-Type on POST")
    require(server, '!contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)',
            "embedded MCP must still reject non-JSON Content-Type")
    require(server, 'WriteResponse(stream, 415, "Unsupported Media Type", "{\\"error\\":\\"Content-Type application/json is required\\"}", null);',
            "embedded MCP must still return HTTP 415 for missing/wrong Content-Type")

    print("PASS: OpenAI Secure MCP Tunnel forwards application/json without weakening embedded MCP admission.")


if __name__ == "__main__":
    main()
