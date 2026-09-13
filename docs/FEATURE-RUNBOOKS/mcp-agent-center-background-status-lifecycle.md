# C04 Agent Center background status lifecycle

## Contract
- Background local operations never publish raw exception text.
- A window close invalidates outstanding background publications.
- Worker threads release serialization slots before UI publication.
- Process-global agent status is published on the surviving UI generation only.
- No retry, replay, or second writer is introduced by status handling.

## Validation
Run `python scripts/preflight-mcp-agent-center-background-status-lifecycle.py` plus broad MCP production/capability preflights. Licensed BricsCAD close/reopen, provider switching and tunnel timing remain LOCAL_ONLY / NO_RESULT until executed in a licensed host.
