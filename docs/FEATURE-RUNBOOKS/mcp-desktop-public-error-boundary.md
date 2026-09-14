# MCP desktop public error boundary

Issue #7009 hardens desktop-control and desktop-sequence public error publication. Exception-derived text exposed to Agent Experience or tool-facing failures must pass through `McpPublicTextSanitizer`; internal exception objects remain chained for diagnostics. Consent generation, emergency-stop/cancel ownership, sequence fail-fast/no-rollback semantics, thread/application context and writer ownership are unchanged. No retry/replay or second writer is introduced.

Runtime classification: REMOTE_SAFE for source/static/preflight. Licensed BricsCAD desktop-input/UI timing is LOCAL_ONLY / NO_RESULT unless exercised in a licensed host.
