# MCP embedded listener LastError boundary

Issue: #7096

The embedded listener stores only canonical public-safe error text in LastError before Describe()/Agent Center status publication.

## Safety invariants
- Listener/request failures are sanitized and bounded before publication.
- Internal exception objects are not serialized through LastError.
- No retry, replay, writer, document, or CAD mutation semantics are changed.
- Real socket/provider/Agent Center timing remains LOCAL_ONLY / NO_RESULT unless exercised.

## Verification
Run `python scripts/preflight-mcp-embedded-listener-last-error.py`, `python scripts/preflight-mcp-production-correctness.py`, `python scripts/preflight-mcp-capability-lanes.py`, and generic `python scripts/preflight.py`.
