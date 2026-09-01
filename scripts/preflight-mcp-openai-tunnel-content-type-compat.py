#!/usr/bin/env python3
from pathlib import Path
import hmac
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
LOCAL_HEADER = "X-QS3D-MCP-Local-Authorization"


def fail(message: str) -> None:
    print(f"ERROR: OpenAI tunnel Content-Type compatibility regression failed: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        fail(message)


def bearer_token(value: str | None) -> str | None:
    if not value or not value.lower().startswith("bearer "):
        return None
    token = value[7:].strip()
    return token or None


def is_valid_local_tunnel_authorization_model(
    headers: dict[str, str], *, openai_provider: bool, local_token: str
) -> bool:
    if not openai_provider:
        return False
    candidate = bearer_token(headers.get(LOCAL_HEADER))
    return candidate is not None and hmac.compare_digest(candidate, local_token)


def authorize_model(
    headers: dict[str, str], *, openai_provider: bool, local_token: str, oauth_tokens: set[str]
) -> bool:
    if LOCAL_HEADER in headers and openai_provider:
        return is_valid_local_tunnel_authorization_model(
            headers, openai_provider=openai_provider, local_token=local_token
        )

    authorization = bearer_token(headers.get("Authorization"))
    if authorization is None:
        return False
    if hmac.compare_digest(authorization, local_token):
        return True
    return authorization in oauth_tokens


def has_json_content_type(headers: dict[str, str]) -> bool:
    value = headers.get("Content-Type", "")
    return value.split(";", 1)[0].strip().lower() == "application/json"


def simulate_post(
    headers: dict[str, str],
    body: str,
    *,
    openai_provider: bool,
    local_token: str,
    oauth_tokens: set[str],
) -> int:
    if not authorize_model(
        headers,
        openai_provider=openai_provider,
        local_token=local_token,
        oauth_tokens=oauth_tokens,
    ):
        return 401

    trusted_openai_tunnel_request = is_valid_local_tunnel_authorization_model(
        headers, openai_provider=openai_provider, local_token=local_token
    )
    if not has_json_content_type(headers) and not trusted_openai_tunnel_request:
        return 415

    if not body.strip():
        return 400
    try:
        parsed = json.loads(body)
    except json.JSONDecodeError:
        return 400
    if not isinstance(parsed, dict):
        return 400
    return 200 if parsed.get("jsonrpc") == "2.0" and parsed.get("method") == "initialize" else 400


def verify_behavior() -> None:
    local = "LOCAL_TOKEN_0123456789"
    oauth = "OAUTH_TOKEN_987654321"
    valid_body = json.dumps(
        {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": "2025-03-26",
                "capabilities": {},
                "clientInfo": {"name": "content-type-compat", "version": "1"},
            },
        },
        separators=(",", ":"),
    )

    # 1. A correctly authenticated OpenAI local tunnel request must survive a
    # connector-rewritten or missing Content-Type and reach JSON-RPC handling.
    trusted_missing = {LOCAL_HEADER: f"Bearer {local}"}
    if simulate_post(
        trusted_missing,
        valid_body,
        openai_provider=True,
        local_token=local,
        oauth_tokens=set(),
    ) != 200:
        fail("trusted OpenAI local tunnel request with missing Content-Type was rejected")

    trusted_rewritten = {
        LOCAL_HEADER: f"Bearer {local}",
        "Content-Type": "text/plain; charset=utf-8",
    }
    if simulate_post(
        trusted_rewritten,
        valid_body,
        openai_provider=True,
        local_token=local,
        oauth_tokens=set(),
    ) != 200:
        fail("trusted OpenAI local tunnel request with rewritten Content-Type was rejected")

    # 2. A wrong local tunnel token remains an authentication failure, even if
    # connector Authorization happens to contain a valid OAuth token.
    wrong_local = {
        LOCAL_HEADER: "Bearer WRONG_LOCAL_TOKEN",
        "Authorization": f"Bearer {oauth}",
        "Content-Type": "text/plain",
    }
    if simulate_post(
        wrong_local,
        valid_body,
        openai_provider=True,
        local_token=local,
        oauth_tokens={oauth},
    ) != 401:
        fail("wrong dedicated local tunnel token did not remain HTTP 401")

    # 3. Content-Type compatibility is not a parser bypass: authenticated local
    # tunnel traffic with garbage body must still be rejected by body/JSON parsing.
    trusted_garbage = {
        LOCAL_HEADER: f"Bearer {local}",
        "Content-Type": "text/plain",
    }
    if simulate_post(
        trusted_garbage,
        "not-json-at-all",
        openai_provider=True,
        local_token=local,
        oauth_tokens=set(),
    ) != 400:
        fail("trusted local tunnel Content-Type compatibility bypassed JSON/body validation")

    # 4. OAuth/non-tunnel traffic keeps the strict media-type boundary.
    oauth_wrong_content_type = {
        "Authorization": f"Bearer {oauth}",
        "Content-Type": "text/plain",
    }
    if simulate_post(
        oauth_wrong_content_type,
        valid_body,
        openai_provider=False,
        local_token=local,
        oauth_tokens={oauth},
    ) != 415:
        fail("OAuth/non-tunnel request with wrong Content-Type no longer returns HTTP 415")


def verify_source_contract() -> None:
    server = SERVER.read_text(encoding="utf-8")

    require(
        server,
        "private static bool IsValidLocalTunnelAuthorization(",
        "active server is missing the provider-scoped local tunnel validation helper",
    )
    require(
        server,
        "McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel",
        "local tunnel helper is not scoped fail-closed to the OpenAI provider",
    )
    require(
        server,
        "headers.TryGetValue(LocalTunnelAuthorizationHeader, out authorization)",
        "local tunnel helper does not read the dedicated local-auth header",
    )
    require(
        server,
        "TryExtractBearerToken(authorization, out token)",
        "local tunnel helper does not validate the Bearer scheme",
    )
    require(
        server,
        "ConstantTimeEquals(token, GetBearerToken())",
        "local tunnel helper does not compare the local token in constant time",
    )
    require(
        server,
        "var trustedOpenAiTunnelRequest = IsValidLocalTunnelAuthorization(request.Headers);",
        "POST handling does not derive trusted OpenAI tunnel state from validated local auth",
    )
    require(
        server,
        "var hasJsonContentType =",
        "POST handling does not explicitly derive JSON Content-Type state",
    )
    require(
        server,
        "if (!hasJsonContentType && !trustedOpenAiTunnelRequest)",
        "strict Content-Type 415 gate is not limited to untrusted/non-tunnel requests",
    )
    require(
        server,
        'WriteResponse(stream, 415, "Unsupported Media Type",',
        "HTTP 415 behavior was removed instead of compatibility-scoped",
    )


def main() -> None:
    verify_behavior()
    verify_source_contract()
    print(
        "PASS: authenticated OpenAI local tunnel requests tolerate connector Content-Type rewrites while auth, parser, and non-tunnel 415 boundaries remain strict."
    )


if __name__ == "__main__":
    main()
