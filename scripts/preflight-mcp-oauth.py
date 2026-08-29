#!/usr/bin/env python3
"""Source guard for the embedded QS3D OAuth 2.1 / ChatGPT DCR boundary.

This is intentionally source-oriented: hosted CI can prove the security contract and
V25/V26 compilation, while real browser/Cloudflare/BricsCAD consent remains LOCAL_ONLY.
"""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
OAUTH = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOAuthAuthorizationServer.cs"
CONSENT = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpOAuthConsent.cs"
EMBEDDED = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def fail(message: str) -> None:
    print("ERROR: MCP OAuth preflight failed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(label + " is missing: " + needle)


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        fail(label + " must not contain: " + needle)


def main() -> int:
    if not OAUTH.is_file():
        fail("OAuth authorization-server source is missing: " + str(OAUTH.relative_to(ROOT)))
    if not CONSENT.is_file():
        fail("local BricsCAD consent source is missing: " + str(CONSENT.relative_to(ROOT)))
    if not EMBEDDED.is_file():
        fail("embedded MCP transport source is missing: " + str(EMBEDDED.relative_to(ROOT)))

    oauth = OAUTH.read_text(encoding="utf-8")
    consent = CONSENT.read_text(encoding="utf-8")
    embedded = EMBEDDED.read_text(encoding="utf-8")

    # OAuth/MCP discovery required by ChatGPT custom MCP. Property names appear escaped
    # inside C# JSON string literals, so assert their exact identifiers rather than an
    # unescaped quoted representation that can never occur in source text.
    for needle in (
        "/.well-known/oauth-protected-resource",
        "/.well-known/oauth-authorization-server",
        "/oauth/register",
        "/oauth/authorize",
        "/oauth/token",
        "authorization_servers",
        "authorization_endpoint",
        "token_endpoint",
        "registration_endpoint",
        "code_challenge_methods_supported",
        "S256",
    ):
        require(oauth, needle, "OAuth discovery/DCR contract")

    # The normal ChatGPT flow must be a public client with DCR + authorization code/PKCE.
    require(oauth, 'TokenEndpointAuthMethod = "none"', "public-client token authentication")
    require(oauth, 'AuthorizationCodeGrant = "authorization_code"', "authorization-code grant")
    require(oauth, 'RefreshTokenGrant = "refresh_token"', "refresh-token grant")
    require(oauth, "ValidatePkceVerifier", "PKCE verifier validation")
    require(oauth, "ComputePkceChallenge", "PKCE S256 challenge verification")
    require(oauth, "ConstantTimeEquals", "constant-time secret comparison")
    require(oauth, "HMACSHA256", "signed opaque OAuth tokens")
    require(oauth, "RandomNumberGenerator.Create", "cryptographic randomness")

    # ChatGPT long-lived OAuth connectivity requires an advertised offline/refresh scope.
    # Keep it authorization-server-only: the protected MCP resource still exposes just
    # the qs3d:mcp permission, while authorization may grant optional offline_access.
    require(oauth, 'OfflineAccessScope = "offline_access"', "offline-access scope declaration")
    require(oauth, '"scopes_supported":[\"" + RequiredScope + "\",\"" + OfflineAccessScope + "\"]', "authorization-server offline_access discovery")
    require(oauth, "TryNormalizeAuthorizationScope", "offline-access authorization scope parser")
    require(oauth, "HasOfflineAccess", "offline-access grant detection")
    require(oauth, "includeRefreshToken", "refresh-token issuance gate")
    require(oauth, "requested refresh scope exceeds the original grant", "refresh-scope preservation")

    protected_resource_block = oauth.split('if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)\n                && string.Equals(path, "/.well-known/oauth-authorization-server"', 1)[0]
    if OfflineAccessScope_literal := 'OfflineAccessScope':
        if OfflineAccessScope_literal in protected_resource_block:
            fail("protected-resource metadata must not advertise offline_access")

    # DCR is fail-closed to ChatGPT connector callbacks, never arbitrary redirect URIs.
    require(oauth, 'ChatGptCallbackPrefix = "https://chatgpt.com/connector/oauth/"', "ChatGPT callback allowlist")
    require(oauth, "IsAllowedChatGptRedirect", "redirect allowlist function")
    require(oauth, "redirect_uris", "DCR redirect registration")
    reject(oauth, "StartsWith(\"https://chatgpt.com/\"", "over-broad ChatGPT redirect allowlist")

    # Every DCR string array must be parsed as an array with immutable count/length bounds.
    require(oauth, "TryParseJsonStringArray(rawRedirects", "redirect URI array parsing")
    require(oauth, "TryParseJsonStringArray(rawGrantTypes", "grant type array parsing")
    require(oauth, "TryParseJsonStringArray(rawResponseTypes", "response type array parsing")
    require(oauth, "maxItemLength", "DCR per-item length bound")
    reject(oauth, "++maxItems", "mutable DCR array count bound")

    # Every credential is resource/client/scope bound and short-lived where appropriate.
    for needle in (
        "AccessTokenLifetime",
        "RefreshTokenLifetime",
        "AuthorizationCodeLifetime",
        "ClientRegistrationLifetime",
        "ValidatePublicMcpResource",
        "RequiredScope",
        "resource",
        "client_id",
        "scope",
    ):
        require(oauth, needle, "OAuth credential binding")

    # Authorization codes must be one-time and process-bound, closing restart/replay ambiguity.
    require(oauth, "ConsumedAuthorizationCodes", "authorization-code replay cache")
    require(oauth, "ProcessNonce", "authorization-code process binding")
    require(oauth, "TryAdd", "single-use authorization-code consumption")

    # MCP OAuth public-client refresh tokens MUST rotate. Make each refresh credential
    # one-use and process-bound so a consumed token cannot be replayed, including after a
    # BricsCAD process restart where the process nonce necessarily changes.
    require(oauth, "ConsumedRefreshTokens", "refresh-token replay cache")
    require(oauth, "CleanupConsumedRefreshTokens();", "bounded refresh-token replay-cache cleanup")
    require(oauth, "ConsumedRefreshTokens.TryAdd(HashForCache(refresh), expiry)", "single-use refresh-token consumption")
    require(oauth, '"refresh token was already used"', "refresh-token replay rejection")
    require(oauth, "fields.Length != 7", "process-bound refresh-token payload")

    # Security-sensitive query/form parsing must reject duplicates and malformed percent encoding.
    require(oauth, "ParseFormEncoded", "strict query/form parser")
    require(oauth, "duplicate OAuth parameter", "duplicate parameter rejection")
    require(oauth, "UTF8Encoding(false, true)", "strict UTF-8 decoding")

    # User authorization is local to BricsCAD; the OAuth protocol source must not harvest credentials.
    require(oauth, "McpOAuthConsent.RequestApproval", "explicit local consent")
    for forbidden in (
        "Process.Start",
        "cmd.exe",
        "powershell",
        "password",
        "client_secret_post",
        "client_secret_basic",
    ):
        reject(oauth.lower(), forbidden.lower(), "OAuth protocol security boundary")

    # Consent must be marshalled into BricsCAD's application context and bounded.
    require(consent, "ExecuteInApplicationContext", "BricsCAD application-context consent")
    require(consent, "MessageBox.Show", "visible local consent prompt")
    require(consent, "SemaphoreSlim", "single concurrent consent prompt")
    require(consent, "ConsentTimeoutMilliseconds", "bounded consent timeout")
    require(consent, "YesNo", "explicit approve/deny choice")
    reject(consent, "Process.Start", "consent UI process-launch boundary")

    # The shipping HTTP transport must preserve the raw query for /oauth/authorize,
    # dispatch OAuth discovery/DCR/token endpoints before the /mcp-only router, and accept
    # either the legacy engineering bearer or a validated OAuth access token.
    for needle in (
        "public string Query",
        "McpOAuthAuthorizationServer.TryHandle(",
        "request.Query",
        "McpOAuthAuthorizationServer.TryValidateAccessToken(",
        "McpOAuthAuthorizationServer.BuildBearerChallenge(",
        "oauthResponse.ContentType",
        "WWW-Authenticate",
    ):
        require(embedded, needle, "OAuth transport wiring")
    reject(embedded, '["WWW-Authenticate"] = "Bearer"', "legacy bare challenge after OAuth wiring")

    # Keep the implementation shareable by V25 net48 and V26 net8 source composition.
    for modern_only in ("Convert.ToHexString", "RandomNumberGenerator.GetBytes(", "CryptographicOperations.FixedTimeEquals"):
        reject(oauth, modern_only, "net48 compatibility")

    # Avoid accidental logging/serialization of the legacy static bearer key.
    if re.search(r"Console\.(Write|WriteLine).*token", oauth, re.IGNORECASE):
        fail("OAuth source must not print token material")

    print("PASS embedded MCP OAuth 2.1/DCR source + transport contract")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
