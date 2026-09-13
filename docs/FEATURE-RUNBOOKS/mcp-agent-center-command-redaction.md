# MCP Agent Center command-boundary redaction

## Scope
This runbook covers the `QS3DMCPAGENTCENTER` command entrypoint and its public failure publication into the captured BricsCAD document editor.

## Required contract
- Capture the active managed `Document` once at command entry and fail soft when no document exists.
- Do not publish raw `Exception.Message`, stack traces, provider credentials, paths, tokens, tunnel identifiers, or transport internals to the CAD editor.
- Route exception-derived public text through `McpPublicTextSanitizer` and keep a bounded generic fallback.
- Do not retry `McpEmbeddedServer.EnsureStarted()` or window construction after an exception; the command boundary is not a replay authority.
- Do not introduce a second native/application-context writer or background callback merely to report command failure.
- Keep failure reporting best-effort and document-bound; a reporting failure must not mask the original operation with a second native mutation.

## Self-review checklist
Audit adjacent Agent Center/provider/tunnel entrypoints for raw exception publication, stale active-document reacquisition, duplicate failure toasts, wrong-thread editor calls, and schema/error-message divergence from MCP tool results.

## Validation
`python scripts/preflight-mcp-agent-center-command-redaction.py` must fail on raw exception publication and pass only when the command keeps captured-document feedback with canonical sanitization. Run MCP production/capability guards and fresh exact-head Shared + Hybrid CI before merge.

## Runtime classification
Source/static validation is `REMOTE_SAFE`. Licensed BricsCAD editor lifetime, host shutdown, MDI replacement, provider/tunnel timing, and native scheduling remain `LOCAL_ONLY / NO_RESULT` unless executed against the exact candidate SHA.
