#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TUNNEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
SERVER_V2 = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
LOCAL_HEADER = "X-QS3D-MCP-Local-Authorization"


def fail(message: str) -> None:
    print(f"ERROR: OpenAI MCP tunnel Content-Type/local-auth preflight failed: {message}", file=sys.stderr)
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
    local_authorization = '            yaml.AppendLine("    " + LocalTunnelAuthorizationHeader + ": env:" + LocalBearerEnvironment);'
    content_type = '            yaml.AppendLine("    Content-Type: application/json");'
    discovery_headers = '            yaml.AppendLine("  discovery_extra_headers:");'

    require(tunnel, 'private const string LocalBearerEnvironment = "QS3D_TUNNEL_MCP_AUTH";',
            "local bearer environment variable contract changed")
    require(tunnel, f'private const string LocalTunnelAuthorizationHeader = "{LOCAL_HEADER}";',
            "dedicated collision-safe local-auth header constant is missing")
    require(config, extra_headers, "mcp.extra_headers generation is missing")
    require(config, local_authorization, "dedicated local bearer env reference is missing")
    require(config, content_type, "forwarded MCP POSTs are not forced to application/json")
    require(config, discovery_headers, "discovery_extra_headers generation is missing")

    if config.count(local_authorization) != 2:
        fail("dedicated local bearer header must be emitted exactly once for runtime and once for discovery")
    first_local = config.index(local_authorization)
    second_local = config.index(local_authorization, first_local + 1)
    if not (config.index(extra_headers) < first_local < config.index(content_type) < config.index(discovery_headers) < second_local):
        fail("runtime/discovery dedicated local-auth and Content-Type header ordering is invalid")
    if config.count(content_type) != 1:
        fail("Content-Type forwarding header must be emitted exactly once")
    if "Authorization: env:" in config:
        fail("local bearer must not share connector/OAuth Authorization")
    if "Authorization: Bearer " in config or "McpEmbeddedServer.GetBearerToken()" in config:
        fail("generated YAML must not persist or directly construct the real bearer token")

    require(tunnel, 'startInfo.EnvironmentVariables[LocalBearerEnvironment] = "Bearer " + McpEmbeddedServer.GetBearerToken();',
            "local bearer must remain child-environment injected")
    require(server, f'private const string LocalTunnelAuthorizationHeader = "{LOCAL_HEADER}";',
            "embedded MCP dedicated local-auth header constant is missing")
    require(server, 'string.Equals(name, LocalTunnelAuthorizationHeader, StringComparison.OrdinalIgnoreCase)',
            "dedicated local-auth header must be a security-sensitive singleton")
    require(server, 'private static bool IsValidLocalTunnelAuthorization(',
            "provider-scoped local tunnel auth helper is missing")
    require(server, 'McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel',
            "dedicated local-auth helper is not scoped fail-closed to OpenAI Secure Tunnel")
    require(server, 'headers.TryGetValue(LocalTunnelAuthorizationHeader, out authorization)',
            "embedded MCP does not read the dedicated local-auth header")
    require(server, 'if (!TryExtractBearerToken(authorization, out token)) return false;',
            "malformed dedicated Bearer must fail closed")
    require(server, 'return ConstantTimeEquals(token, GetBearerToken());',
            "dedicated local bearer is not compared in constant time")
    require(server, 'var trustedOpenAiTunnelRequest = IsValidLocalTunnelAuthorization(request.Headers);',
            "POST handling does not derive trusted tunnel state from validated local auth")
    require(server, 'var hasJsonContentType =',
            "embedded MCP no longer derives exact JSON Content-Type state")
    require(server, 'request.Headers.TryGetValue("Content-Type", out contentType)',
            "embedded MCP no longer reads Content-Type on POST")
    require(server, '&& IsJsonContentType(contentType);',
            "embedded MCP exact media-type parsing was removed")
    require(server, 'if (!hasJsonContentType && !trustedOpenAiTunnelRequest)',
            "Content-Type compatibility is not limited to authenticated OpenAI tunnel requests")
    require(server, 'private static bool IsJsonContentType(string contentType)',
            "embedded MCP exact JSON media-type parser is missing")
    require(server, 'WriteResponse(stream, 415, "Unsupported Media Type", "{\\"error\\":\\"Content-Type application/json is required\\"}", null);',
            "embedded MCP must still return HTTP 415 for missing/wrong Content-Type outside the trusted tunnel exception")

    print("PASS: OpenAI Secure MCP Tunnel forwards application/json; authenticated local tunnel requests tolerate connector rewrites while non-tunnel media-type enforcement remains strict.")


if __name__ == "__main__":
    main()
