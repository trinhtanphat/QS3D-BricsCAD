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

    require(oauth, 'TokenEndpointAuthMethod = "none"', "public-client token authentication")
    require(oauth, 'AuthorizationCodeGrant = "authorization_code"', "authorization-code grant")
    require(oauth, 'RefreshTokenGrant = "refresh_token"', "refresh-token grant")
    require(oauth, "ValidatePkceVerifier", "PKCE verifier validation")
    require(oauth, "ComputePkceChallenge", "PKCE S256 challenge verification")
    require(oauth, "ConstantTimeEquals", "constant-time secret comparison")
    require(oauth, "HMACSHA256", "signed opaque OAuth tokens")
    require(oauth, "RandomNumberGenerator.Create", "cryptographic randomness")

    # ChatGPT long-lived OAuth connectivity requests optional offline_access. It belongs
    # to authorization-server discovery only; protected-resource metadata/challenges
    # continue to advertise the MCP permission qs3d:mcp and never offline_access.
    require(oauth, 'OfflineAccessScope = "offline_access"', "offline-access scope declaration")
    require(oauth, '\\"scopes_supported\\":[\\\"' + '" + RequiredScope + "' + '\\\",\\\"' + '" + OfflineAccessScope + "' + '\\\"]', "authorization-server offline_access discovery")
    require(oauth, "TryNormalizeAuthorizationScope", "offline-access authorization scope parser")
    require(oauth, "HasOfflineAccess", "offline-access grant detection")
    require(oauth, "includeRefreshToken", "refresh-token issuance gate")
    require(oauth, "requested refresh scope exceeds the original grant", "refresh-scope preservation")

    protected_marker = 'string.Equals(path, "/.well-known/oauth-protected-resource"'
    authorization_marker = 'string.Equals(path, "/.well-known/oauth-authorization-server"'
    protected_start = oauth.find(protected_marker)
    authorization_start = oauth.find(authorization_marker, protected_start + 1)
    if protected_start < 0 or authorization_start < 0 or authorization_start <= protected_start:
        fail("unable to isolate protected-resource metadata branch")
    protected_resource_block = oauth[protected_start:authorization_start]
    require(
        protected_resource_block,
        '\\"scopes_supported\\":[\\\"' + '" + RequiredScope + "' + '\\\"]',
        "protected-resource qs3d:mcp-only scope discovery",
    )
    reject(protected_resource_block, "OfflineAccessScope", "protected-resource metadata")
    reject(protected_resource_block, "offline_access", "protected-resource metadata")

    require(oauth, 'ChatGptCallbackPrefix = "https://chatgpt.com/connector/oauth/"', "ChatGPT callback allowlist")
    require(oauth, "IsAllowedChatGptRedirect", "redirect allowlist function")
    require(oauth, "redirect_uris", "DCR redirect registration")
    reject(oauth, "StartsWith(\"https://chatgpt.com/\"", "over-broad ChatGPT redirect allowlist")

    require(oauth, "TryParseJsonStringArray(rawRedirects", "redirect URI array parsing")
    require(oauth, "TryParseJsonStringArray(rawGrantTypes", "grant type array parsing")
    require(oauth, "TryParseJsonStringArray(rawResponseTypes", "response type array parsing")
    require(oauth, "maxItemLength", "DCR per-item length bound")
    reject(oauth, "++maxItems", "mutable DCR array count bound")

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

    require(oauth, "ConsumedAuthorizationCodes", "authorization-code replay cache")
    require(oauth, "ProcessNonce", "authorization-code process binding")
    require(oauth, "TryAdd", "single-use authorization-code consumption")

    require(oauth, "ConsumedRefreshTokens", "refresh-token replay cache")
    require(oauth, "CleanupConsumedRefreshTokens();", "bounded refresh-token replay-cache cleanup")
    require(oauth, "ConsumedRefreshTokens.TryAdd(HashForCache(refresh), expiry)", "single-use refresh-token consumption")
    require(oauth, '"refresh token was already used"', "refresh-token replay rejection")
    require(oauth, "fields.Length != 7", "process-bound refresh-token payload")

    require(oauth, "ParseFormEncoded", "strict query/form parser")
    require(oauth, "duplicate OAuth parameter", "duplicate parameter rejection")
    require(oauth, "UTF8Encoding(false, true)", "strict UTF-8 decoding")

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

    require(consent, "ExecuteInApplicationContext", "BricsCAD application-context consent")
    require(consent, "MessageBox.Show", "visible local consent prompt")
    require(consent, "SemaphoreSlim", "single concurrent consent prompt")
    require(consent, "ConsentTimeoutMilliseconds", "bounded consent timeout")
    require(consent, "YesNo", "explicit approve/deny choice")
    reject(consent, "Process.Start", "consent UI process-launch boundary")

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

    # Remote ChatGPT connectivity is different from local/tunnel readiness. Accept only
    # absent/loopback Origin or the exact origin of the currently validated public MCP
    # resource, and record only successful OAuth MCP traffic (never legacy self-test bearer).
    require(embedded, "IsAllowedOrigin(request.Headers, publicMcpUrl)", "public-resource-aware Origin gate")
    require(embedded, "IsSameOriginAsPublicMcp", "exact public MCP Origin comparison")
    require(embedded, "LastOAuthMcpActivityUtc", "privacy-safe OAuth MCP activity timestamp")
    require(embedded, "LastOAuthMcpMethod", "privacy-safe OAuth MCP activity method")
    require(embedded, "LastOAuthMcpPublicUrl", "OAuth MCP resource binding")
    require(embedded, "RecordOAuthMcpActivity", "OAuth MCP activity recorder")
    require(embedded, "out bool oauthAccessToken", "legacy-bearer versus OAuth authorization distinction")

    for modern_only in ("Convert.ToHexString", "RandomNumberGenerator.GetBytes(", "CryptographicOperations.FixedTimeEquals"):
        reject(oauth, modern_only, "net48 compatibility")

    if re.search(r"Console\.(Write|WriteLine).*token", oauth, re.IGNORECASE):
        fail("OAuth source must not print token material")

    print("PASS embedded MCP OAuth 2.1/DCR source + transport contract")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
