# MCP legacy tunnel public-error boundary

Fallback MCP onboarding, cloudflared bootstrap, connector commands/probes, and OpenAI Secure Tunnel expose status or error text to BricsCAD users or remote/operator-visible diagnostics.

## Contract

- Exception-derived text crossing those public boundaries is sanitized through `McpPublicTextSanitizer`.
- Internal exception identity may remain available through the exception object itself; public strings must not expose raw stack/path/secret-bearing detail.
- Existing transport selection, process ownership, cancellation, retry/backoff, tool/protocol schema, error codes, and mutation ownership are unchanged.
- This change adds no retry, replay, second writer, native CAD mutation, or fail-open recovery behavior.
- Failure remains failure: sanitization must never convert a failed start/probe/install into a success result.

## Validation

Run `python scripts/preflight-mcp-legacy-tunnel-public-error-boundary.py`, `python scripts/preflight-mcp-production-correctness.py`, `python scripts/preflight-mcp-capability-lanes.py`, repository health, and `git diff --check`.

Licensed BricsCAD, Cloudflare, and OpenAI tunnel timing remains `LOCAL_ONLY / NO_RESULT` until exercised on an exact candidate.
