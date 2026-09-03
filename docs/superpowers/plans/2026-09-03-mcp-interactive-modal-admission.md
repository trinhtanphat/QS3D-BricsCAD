# MCP interactive-modal admission implementation plan

Issue: #5504
PR: #5505

## Goal

Close the plugin-owned modal serialization gap without changing the separately owned `McpCadViewStatusRuntime.cs` lane and without introducing nested semaphore reacquisition.

## TDD evidence

The test-only head `61d2218a48cd670f8271ea030c3dfc3ce66a0347` intentionally added only the Reservation-v2 claim and `scripts/preflight-mcp-interactive-modal-admission.py`. Exact-head CI passed Reservation-v2, generic MCP source guard, exclusion proof, Python syntax and package integrity, then failed the aggregate feature guard because the semantic modal API/scope and OAuth migration were absent. Core was skipped fail-closed. This is the required RED state.

## Implementation

1. Add `McpCadMutationCoordinator.EnterInteractiveModal(...)` using the existing process-global `MutationGate`.
2. Reject current-flow mutation, prepared-native, and nested-modal ownership before semaphore acquisition to prevent self-deadlock.
3. Reject explicit writer lease/pending native ownership before acquisition and re-check after acquisition.
4. Check BricsCAD modal state before acquisition and again in CAD application context after acquisition.
5. Return an `InteractiveModalScope` that retains `MutationGate` until disposal and releases it in a `finally` path.
6. Reject mutation entry from a current-flow interactive-modal scope.
7. Migrate OAuth consent to `EnterInteractiveModal("oauth_interactive_consent", ...)` while retaining its single-flight prompt gate and delayed-callback cancellation semantics.
8. Update the existing OAuth/CAD interaction guard to require the semantic API and forbid `EnterMutation` in the consent request path.

## Verification and merge

Run exact-head hosted CI. If `main` advances, non-force sync the carrier on top of live `main`, rerun exact-head CI, mark the PR ready only when required checks are green, and merge with expected-head SHA. Licensed foreground BricsCAD qualification remains LOCAL_ONLY and is not inferred from hosted CI.
