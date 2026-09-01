#!/usr/bin/env python3
from pathlib import Path
import hmac
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
TUNNEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOpenAiSecureTunnel.cs"
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
PROJECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "QS3D.BricsCAD.V25.csproj"
LOCAL_HEADER = "X-QS3D-MCP-Local-Authorization"
LOCAL_ENV = "QS3D_TUNNEL_MCP_AUTH"


def fail(message: str) -> None:
    print(f"ERROR: OpenAI tunnel local-auth regression failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        fail(message)


def bearer_token(value: str | None) -> str | None:
    if not value or not value.lower().startswith("bearer "):
        return None
    token = value[7:].strip()
    return token or None


def authorize_model(headers: dict[str, str], *, openai_provider: bool, local_token: str, oauth_tokens: set[str]) -> bool:
    local_authorization = headers.get(LOCAL_HEADER)
    if local_authorization is not None and openai_provider:
        candidate = bearer_token(local_authorization)
        return candidate is not None and hmac.compare_digest(candidate, local_token)

    authorization = bearer_token(headers.get("Authorization"))
    if authorization is None:
        return False
    if hmac.compare_digest(authorization, local_token):
        return True
    return authorization in oauth_tokens


def parse_security_headers(pairs: list[tuple[str, str]]) -> dict[str, str]:
    singleton = {
        "authorization",
        LOCAL_HEADER.lower(),
        "content-length",
        "content-type",
        "transfer-encoding",
        "origin",
        "mcp-session-id",
        "mcp-protocol-version",
        "mcp-method",
        "mcp-name",
    }
    parsed: dict[str, str] = {}
    for name, value in pairs:
        key = name.lower()
        if key in parsed and key in singleton:
            raise ValueError("duplicate security-sensitive HTTP header")
        parsed[key] = value
    return parsed


def simulate_initialize(headers: dict[str, str], *, openai_provider: bool, local_token: str, oauth_tokens: set[str]) -> int:
    if headers.get("Content-Type", "").split(";", 1)[0].strip().lower() != "application/json":
        return 415
    if not authorize_model(headers, openai_provider=openai_provider, local_token=local_token, oauth_tokens=oauth_tokens):
        return 401
    body = json.loads('{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"collision-regression","version":"1"}}}')
    return 200 if body.get("method") == "initialize" else 400


def verify_collision_behavior() -> None:
    local = "LOCAL_TOKEN_0123456789"
    connector = "CONNECTOR_TOKEN_987654321"

    # tunnel-client behavior: static extra headers first, connector-forwarded headers last.
    effective = {
        LOCAL_HEADER: f"Bearer {local}",
        "Content-Type": "application/json",
    }
    effective.update({"Authorization": f"Bearer {connector}"})

    if effective.get(LOCAL_HEADER) != f"Bearer {local}":
        fail("connector Authorization replaced the dedicated local credential")
    if effective.get("Authorization") != f"Bearer {connector}":
        fail("connector Authorization was not preserved independently")
    if simulate_initialize(effective, openai_provider=True, local_token=local, oauth_tokens=set()) != 200:
        fail("Authorization collision simulation did not admit initialize through the dedicated local credential")

    direct_local = {"Authorization": f"Bearer {local}", "Content-Type": "application/json"}
    if simulate_initialize(direct_local, openai_provider=True, local_token=local, oauth_tokens=set()) != 200:
        fail("direct local Authorization compatibility regressed")

    missing_local = {"Authorization": f"Bearer {connector}", "Content-Type": "application/json"}
    if simulate_initialize(missing_local, openai_provider=True, local_token=local, oauth_tokens=set()) != 401:
        fail("missing dedicated credential unexpectedly authenticated the local-tunnel case")

    wrong_local = {
        LOCAL_HEADER: "Bearer WRONG_LOCAL_TOKEN",
        "Authorization": f"Bearer {connector}",
        "Content-Type": "application/json",
    }
    if simulate_initialize(wrong_local, openai_provider=True, local_token=local, oauth_tokens={connector}) != 401:
        fail("wrong dedicated credential fell back to connector Authorization")

    malformed_local = {
        LOCAL_HEADER: local,
        "Authorization": f"Bearer {connector}",
        "Content-Type": "application/json",
    }
    if simulate_initialize(malformed_local, openai_provider=True, local_token=local, oauth_tokens={connector}) != 401:
        fail("malformed dedicated credential fell back to connector Authorization")

    public_misuse = {
        LOCAL_HEADER: f"Bearer {local}",
        "Authorization": "Bearer NOT_OAUTH",
        "Content-Type": "application/json",
    }
    if simulate_initialize(public_misuse, openai_provider=False, local_token=local, oauth_tokens={connector}) != 401:
        fail("non-OpenAI/public path used the dedicated header to bypass existing auth")

    public_with_valid_oauth = dict(public_misuse)
    public_with_valid_oauth["Authorization"] = f"Bearer {connector}"
    if simulate_initialize(public_with_valid_oauth, openai_provider=False, local_token=local, oauth_tokens={connector}) != 200:
        fail("non-OpenAI path no longer honors its existing Authorization contract")

    try:
        parse_security_headers([(LOCAL_HEADER, f"Bearer {local}"), (LOCAL_HEADER, "Bearer OTHER")])
    except ValueError:
        pass
    else:
        fail("duplicate/conflicting dedicated local-auth headers were not rejected")


def verify_source_contract() -> None:
    tunnel = TUNNEL.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    project = PROJECT.read_text(encoding="utf-8")

    require(project, '<Compile Remove="McpEmbeddedServer.cs" />', "legacy monolith is no longer explicitly excluded from compilation")
    require(tunnel, f'private const string LocalTunnelAuthorizationHeader = "{LOCAL_HEADER}";', "OpenAI tunnel dedicated-header constant is missing")
    require(server, f'private const string LocalTunnelAuthorizationHeader = "{LOCAL_HEADER}";', "active embedded MCP dedicated-header constant is missing")

    config_start = tunnel.find("        private static void WriteRuntimeConfig(string tunnelId, Uri localEndpoint)")
    config_end = tunnel.find("        private static bool ProbeReady()", config_start)
    if config_start < 0 or config_end < 0:
        fail("cannot isolate OpenAI WriteRuntimeConfig")
    config = tunnel[config_start:config_end]
    env_header = 'yaml.AppendLine("    " + LocalTunnelAuthorizationHeader + ": env:" + LocalBearerEnvironment);'
    if config.count(env_header) != 2:
        fail("dedicated local env header must be emitted exactly once for runtime and once for discovery")
    if "Authorization: env:" in config:
        fail("OpenAI tunnel still collides with connector Authorization")
    require(config, 'yaml.AppendLine("    Content-Type: application/json");', "JSON Content-Type forwarding regressed")
    require(tunnel, f'private const string LocalBearerEnvironment = "{LOCAL_ENV}";', "local bearer environment contract changed")
    require(tunnel, 'startInfo.EnvironmentVariables[LocalBearerEnvironment] = "Bearer " + McpEmbeddedServer.GetBearerToken();', "local bearer is no longer injected only through child environment")
    if "Authorization: Bearer " in config or "GetBearerToken()" in config:
        fail("generated YAML contains or directly constructs a bearer secret")

    require(server, 'string.Equals(name, LocalTunnelAuthorizationHeader, StringComparison.OrdinalIgnoreCase)', "dedicated header is not a security-sensitive singleton")
    require(server, 'private static bool IsValidLocalTunnelAuthorization(', "embedded MCP local tunnel validation helper is missing")
    require(server, 'McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel', "dedicated header is not scoped fail-closed to OpenAI Secure Tunnel")
    require(server, 'headers.TryGetValue(LocalTunnelAuthorizationHeader, out authorization)', "embedded MCP does not read the dedicated local header")
    require(server, 'if (!TryExtractBearerToken(authorization, out token)) return false;', "malformed dedicated Bearer does not fail closed")
    require(server, 'return ConstantTimeEquals(token, GetBearerToken());', "dedicated bearer is not compared in constant time")
    require(server, 'headers.TryGetValue("Authorization", out authorization)', "direct/OAuth Authorization compatibility was removed")
    require(server, 'McpOAuthAuthorizationServer.TryValidateAccessToken(headers, publicMcpUrl, GetBearerToken())', "OAuth access-token validation was removed")


def main() -> None:
    verify_collision_behavior()
    verify_source_contract()
    print("PASS: OpenAI tunnel local-origin auth is collision-safe, provider-scoped, singleton, and preserves existing Authorization paths.")


if __name__ == "__main__":
    main()
