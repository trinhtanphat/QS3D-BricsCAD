# MCP embedded/public runtime error boundary

The embedded MCP protocol and first-run UI expose public failure text to remote clients or end users. Exception-derived text must cross those boundaries only through `McpPublicTextSanitizer`; internal bounded diagnostics may retain structured failure identity.

This contract covers JSON-RPC errors, tool errors and emergency/cancel fallback payloads in the embedded compatibility servers plus direct Agent Experience publication from first-run UI. It does not add retries, replay, a second writer, or alter mutation ownership. Cancellation/emergency-stop remains fail-soft and protocol error codes/schema remain unchanged.

Validation: `python scripts/preflight-mcp-embedded-public-error-boundary.py`, `python scripts/preflight-mcp-production-correctness.py`, `python scripts/preflight-mcp-capability-lanes.py`, repository health, and `git diff --check`.

Licensed BricsCAD request/response timing and visible first-run UI remain LOCAL_ONLY / NO_RESULT until exercised in-host.