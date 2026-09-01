#!/usr/bin/env python3
"""Source guard for QS3D MCP 2026-07-28 stateless compatibility.

Hosted CI can prove the transport contract and V25 compilation. Real ChatGPT +
Cloudflare + licensed BricsCAD qualification remains a local integration boundary.
"""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
EMBEDDED = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def fail(message: str) -> None:
    print("ERROR: modern MCP protocol preflight failed: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        fail(label + " is missing: " + needle)


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        fail(label + " must not contain: " + needle)


def main() -> int:
    if not EMBEDDED.is_file():
        fail("embedded MCP transport source is missing: " + str(EMBEDDED.relative_to(ROOT)))

    embedded = EMBEDDED.read_text(encoding="utf-8")

    for needle in (
        'ModernProtocolVersion = "2026-07-28"',
        'ProtocolVersion = "2025-11-25"',
        'PreviousProtocolVersion = "2025-06-18"',
        'LegacyProtocolVersion = "2025-03-26"',
        'ServerVersion = "embedded-7"',
        '"server/discover"',
        "HandleModernRequest",
        "TryValidateModernRoutingHeaders",
        "TryValidateModernRequestMeta",
        '"Mcp-Method"',
        '"Mcp-Name"',
        '"io.modelcontextprotocol/protocolVersion"',
        '"io.modelcontextprotocol/clientCapabilities"',
        '"io.modelcontextprotocol/clientInfo"',
        r'\"io.modelcontextprotocol/serverInfo\"',
        '\\"resultType\\"',
        '\\"ttlMs\\"',
        '\\"cacheScope\\"',
        "ModernServerInfoMeta",
        "ModernProtocolHeader",
        "ToolAnnotations",
        '\\"readOnlyHint\\"',
        '\\"destructiveHint\\"',
        '\\"idempotentHint\\"',
        '\\"openWorldHint\\"',
        "WithToolAnnotations",
    ):
        require(embedded, needle, "2026-07-28 stateless contract")

    # Modern requests must be routed before legacy session validation, otherwise a
    # current ChatGPT client is incorrectly rejected for not supplying Mcp-Session-Id.
    modern_route = embedded.find("if (modernRequest)")
    legacy_session = embedded.find("TryValidateSession(request.Headers")
    if modern_route < 0 or legacy_session < 0 or modern_route >= legacy_session:
        fail("modern stateless dispatch must happen before legacy session validation")

    discover_route = embedded.find('string.Equals(method, "server/discover"')
    if discover_route < 0:
        fail("server/discover route is missing")

    # Final 2026-07-28 behavior carries request identity/capabilities per request and
    # stamps server identity in result._meta rather than discover.serverInfo.
    meta_validation = embedded.find("private static bool TryValidateModernRequestMeta")
    meta_stamp = embedded.find("private static string ModernServerInfoMeta")
    tools_list = embedded.find("private static string ToolsListResponse")
    if meta_validation < 0 or meta_stamp < 0 or tools_list < 0:
        fail("modern per-request metadata helpers are incomplete")
    require(embedded[meta_validation:meta_stamp], '"io.modelcontextprotocol/protocolVersion"', "modern protocol-version envelope validation")
    require(embedded[meta_validation:meta_stamp], '"io.modelcontextprotocol/clientCapabilities"', "modern client-capabilities envelope validation")
    require(embedded[meta_stamp:tools_list], r'\"io.modelcontextprotocol/serverInfo\"', "modern response server identity stamp")

    modern_handler = embedded[embedded.find("private static void HandleModernRequest"):meta_validation]
    if '\\"serverInfo\\":' in modern_handler:
        fail("modern discover must not keep serverInfo in the result body; use result._meta")
    require(modern_handler, "ModernServerInfoMeta()", "modern discover/ping server identity metadata")

    # Every tool is annotated for ChatGPT action scanning. Desktop descriptors are
    # enriched at the embedded boundary so the desktop runtime remains independently bounded.
    tool_annotations = embedded[embedded.find("private static string Tool("):embedded.find("private static string Numeric(")]
    for needle in ('readOnlyHint', 'destructiveHint', 'idempotentHint', 'openWorldHint', 'desktop_'):
        require(tool_annotations, needle, "full MCP tool annotation surface")

    # Keep legacy compatibility explicitly present while modern traffic remains
    # stateless. Do not delete the old session machinery until its deprecation window.
    for needle in (
        'string.Equals(method, "initialize"',
        '"Mcp-Session-Id"',
        "TryCreateSession",
        "TryValidateSession",
    ):
        require(embedded, needle, "legacy MCP compatibility")

    # Browser-origin acceptance must be exact, never a broad chatgpt.com prefix or
    # arbitrary HTTPS origin exception.
    require(embedded, "IsChatGptOrigin(uri)", "exact ChatGPT Origin compatibility")
    require(embedded, 'string.Equals(origin.DnsSafeHost, "chatgpt.com", StringComparison.OrdinalIgnoreCase)', "exact ChatGPT host match")
    require(embedded, "origin.IsDefaultPort", "default HTTPS port restriction")
    reject(embedded, 'origin.DnsSafeHost.EndsWith("chatgpt.com"', "over-broad ChatGPT host match")
    reject(embedded, 'origin.ToString().StartsWith("https://chatgpt.com"', "over-broad ChatGPT Origin prefix")

    # Routing metadata is security-sensitive and must reject duplicates just like auth.
    critical_start = embedded.find("private static bool IsCriticalSingletonHeader")
    critical_end = embedded.find("private static bool IsHttpFieldName", critical_start)
    if critical_start < 0 or critical_end <= critical_start:
        fail("unable to isolate critical singleton header guard")
    critical = embedded[critical_start:critical_end]
    require(critical, '"Mcp-Method"', "Mcp-Method duplicate rejection")
    require(critical, '"Mcp-Name"', "Mcp-Name duplicate rejection")

    print("PASS embedded MCP 2026-07-28 stateless metadata + full tool annotations + legacy compatibility contract")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
