# MCP shared interactive-modal admission

Issue: #5504
Carrier: `agent/gpt56sol-20260903-shared-interactive-modal/issue-5504`

## Contract

Plugin-owned foreground interactive UI must not appear underneath an active MCP CAD writer. `McpCadMutationCoordinator.EnterInteractiveModal(...)` owns the same process-global `MutationGate` used by mutations/native-command preparation for the entire interactive lifetime.

Admission fails closed when the current logical flow already owns a mutation, prepared native command, or interactive modal; when a global explicit writer lease or pending native command exists; or when BricsCAD reports `CMDACTIVE` modal bit 8. Writer/native and CAD-modal conditions are sampled before acquisition and revalidated after acquisition to close races.

OAuth consent uses this semantic admission directly. It must not use `EnterMutation(...)` to represent a UI prompt. Do not mechanically wrap MessageBox/WPF calls that are already inside mutation/native scopes; nested acquisition is intentionally rejected to prevent semaphore self-deadlock.

## Hosted CI acceptance

- `scripts/preflight-mcp-interactive-modal-admission.py` passes.
- `scripts/preflight-mcp-oauth-cad-interaction.py` passes with `EnterInteractiveModal` and rejects OAuth `EnterMutation` regression.
- Reservation-v2, generic MCP guards, aggregate feature guards, package integrity, and V25 build/core required checks are green on the exact PR head.

## Licensed BricsCAD foreground qualification

Hosted CI is source/build evidence only. A licensed foreground BricsCAD run remains `LOCAL_ONLY` until separately exercised with:

1. Show OAuth consent while no writer/native/modal state is active; verify prompt appears and mutations cannot enter until it closes.
2. Hold an explicit writer lease; request OAuth consent; verify `interaction_required` and no prompt.
3. Run/prepare a native mutation; request OAuth consent; verify `interaction_required` and no prompt.
4. Put BricsCAD in a `CMDACTIVE` bit-8 modal state; request OAuth consent; verify `interaction_required` and no retained writer gate.
5. Close/cancel the prompt and verify subsequent mutation/native command and writer acquire can enter normally.
6. Verify a dialog path already executing inside a mutation does not attempt nested interactive-modal admission.

Do not report foreground runtime PASS from hosted CI alone.
