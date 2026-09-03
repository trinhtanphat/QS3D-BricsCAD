# MCP modal-safe writer recovery

Issue: #5454

## Source contract

- A BricsCAD modal/dialog state (`CMDACTIVE` bit 8) is sampled before attempting to acquire the process-global mutation writer.
- The modal state is sampled again after acquisition before mutation begins, preventing a race between preflight and writer ownership.
- Modal failures return a bounded `interaction_required` signal. MCP does not press Escape, close windows, inject keystrokes, or force-dismiss arbitrary dialogs.
- No modal wait/retry loop is permitted while the writer lease is held.
- `cad_command_state` exposes structured `modal`, `busyKind`, and `interactionRequired` fields without exposing command-line history or prompt text.
- Existing emergency-stop epoch, writer token, preview and document-fingerprint contracts remain authoritative.

## Verification boundary

Hosted CI/source guards prove control-flow and response-contract invariants only. A licensed BricsCAD session is still required to demonstrate real modal/dialog behavior, foreground recovery and post-recovery mutation at runtime.

Run source guards:

```text
python scripts/preflight-mcp-modal-writer-recovery.py
python scripts/preflight-mcp-view-extents-modal-safety.py
```
