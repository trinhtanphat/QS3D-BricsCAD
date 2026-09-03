# MCP modal-safe writer recovery implementation plan

Issue: #5454

## Goal

Prevent CAD mutation writer ownership from being acquired or retained while BricsCAD is already in a modal/dialog state, while preserving the existing process-global single-writer boundary.

## Root cause

`McpCadMutationCoordinator.Prepare` and `EnterMutation` currently acquire `MutationGate` before entering CAD context to inspect `CMDACTIVE`. A blocked CAD/UI transition can therefore leave the writer gate occupied before modal state has been rejected.

## Implementation

1. Add a CAD-context modal preflight that runs before `MutationGate` acquisition.
2. Re-check modal state after acquiring the gate to close the preflight/acquire race.
3. Never wait/retry for UI recovery while the writer gate is held.
4. Return a stable `interaction_required:` marker for modal state; do not synthesize ESC/close/dialog input.
5. Extend `cad_command_state` with `modal`, `busyKind`, and `interactionRequired` fields.
6. Preserve existing view-level `CMDACTIVE` fail-closed checks.

## Verification

Hosted/source:

```text
python scripts/preflight-mcp-modal-writer-recovery.py
python scripts/preflight-mcp-view-extents-modal-safety.py
```

Required PR exact-head checks must be green before merge. Licensed BricsCAD interactive behavior remains LOCAL_ONLY runtime evidence and must not be inferred from hosted CI.
