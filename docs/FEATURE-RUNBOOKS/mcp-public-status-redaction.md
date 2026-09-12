# MCP public status redaction

## Scope

Issue #6481 owns the network-public MCP status/diagnostic publication boundary. Internal diagnostic stores retain raw troubleshooting detail; only public success/error payloads are sanitized.

## Public boundaries

- `agent_status` sanitizes `McpAgentExperience.LastError` before ToolSuccess publication.
- `qs3d_status` and `qs3d_domain_status` sanitize exception-derived `context.reason` and `lastError.message` before JSON publication.
- Embedded MCP V1/V2 public error wrappers delegate to the same `McpPublicTextSanitizer` authority.

## Sanitizer contract

The shared authority bounds public text to 512 characters, redacts credentials/tokens and local paths, removes control characters, and preserves JSON-RPC numeric codes plus MCP code/lane/repair schema. It does not replay or retry mutations.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-public-status-redaction.py
python scripts/preflight-mcp-public-error-redaction.py
python scripts/preflight-mcp-production-correctness.py
python scripts/preflight-mcp-capability-lanes.py
python scripts/preflight-repository-health.py
git diff --check
```
