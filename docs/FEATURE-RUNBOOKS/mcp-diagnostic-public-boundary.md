# MCP diagnostic public boundary

## Ownership
Lane C04 owns the persisted diagnostics/audit-tail publication boundary in `McpDiagnosticHub.Record`.

## Invariant
Diagnostic producers may retain useful exception context internally, but any text persisted into the shared audit stream must pass both the existing diagnostics redaction and `McpPublicTextSanitizer.Sanitize(...)` before serialization. The same audit stream is retrievable through the MCP `cad_audit_tail` tool, so persisted text is a public boundary.

Do not change source/severity/event metadata, audit sequence allocation, file rotation, append ownership, fail-soft behavior, CAD application/document context, retry policy, or mutation semantics as part of this guard.

## Verification
Run:

```text
python scripts/preflight-mcp-diagnostic-public-boundary.py
python scripts/preflight-mcp-production-correctness.py
python scripts/preflight-mcp-capability-lanes.py
python scripts/preflight.py
git diff --check
```

Licenced BricsCAD runtime evidence remains LOCAL_ONLY / NO_RESULT unless a real native host is exercised.
