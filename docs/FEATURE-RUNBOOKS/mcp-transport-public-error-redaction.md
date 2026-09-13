# MCP transport public error redaction

Transport/onboarding UI and process-global Agent Experience sinks must never publish raw exception text. Exception-derived public text is passed through `McpPublicTextSanitizer`; fixed user-facing context remains outside the sanitized fragment. Internal diagnostics may retain structured details only in their existing bounded diagnostic channel.

The change does not add retries, replay, a second writer, or alter transport ownership. Restart/bootstrap failures remain fail-soft and user-visible without exposing local paths, tokens, provider command lines, or runtime internals.

## Validation
Run `python scripts/preflight-mcp-transport-public-error-redaction.py`, `python scripts/preflight-mcp-production-correctness.py`, `python scripts/preflight-mcp-capability-lanes.py`, and `git diff --check`.

Licensed BricsCAD provider/tunnel/UI timing remains LOCAL_ONLY / NO_RESULT until exercised in the host.
