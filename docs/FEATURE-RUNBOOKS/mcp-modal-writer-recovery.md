# MCP modal-safe writer recovery

Issue: #5454

## Source contract

- A BricsCAD modal/dialog state (`CMDACTIVE` bit 8) is sampled before attempting to acquire the process-global mutation writer.
- The modal state is sampled again after acquisition before mutation begins, preventing a race between preflight and writer ownership.
- Modal failures return a bounded `interaction_required` signal. MCP does not press Escape, close windows, inject keystrokes, or force-dismiss arbitrary dialogs.
- No modal wait/retry loop is introduced while the writer lease is held.
- Existing `cad_command_state`/view-level modal detection remains the bounded read-side status contract; this carrier changes writer acquisition ordering rather than adding a second status schema.
- Existing emergency-stop epoch, writer token, native terminal-event barrier and document coordination contracts remain authoritative.

## Verification boundary

Hosted CI/source guards prove control-flow and response-contract invariants only. A licensed BricsCAD session is still required to demonstrate real modal/dialog behavior, foreground recovery and post-recovery mutation at runtime.

Run source guards:

```text
python scripts/preflight-mcp-modal-writer-recovery.py
python scripts/preflight-mcp-view-extents-modal-safety.py
```
