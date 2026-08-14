# Agent work claim — reference Workspace tree registration resilience

- Agent: `chatgpt-web-gpt56sol-reference-tree-registration-resilience`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `344f6466d985ba58336c41379289af03141a4cff`

## Goal

Prevent the presentation-only Workspace reference-tree class-handler registration from poisoning `WorkspacePanel` type initialization or permanently latching a failed registration attempt.

## Reserved paths

- `src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceTreeRegistration.cs`
- `scripts/preflight-reference-workspace-tree-registration-resilience.py`
- this claim file

Read-only surfaces:

- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs`
- `scripts/preflight-blt-reference-ui-parity.py`

## Evidence

`WorkspacePanel.ReferenceTreeRegistration` runs as a static field initializer and directly calls `ReferenceWorkspaceTreeAugmenter.EnsureRegistered()`. The current implementation sets `_registered = true` before `EventManager.RegisterClassHandler(...)` and lets any exception escape. Therefore a WPF registration exception can both fail `WorkspacePanel` type initialization and leave the augmenter logically registered even though the class handler was never installed. The BLT Home file-actions claim currently reserves QuickWorkflow/preflight-blt, so this lane deliberately does not touch them.

## Fix boundary

- Make registration return success/failure and never let presentation-only registration exceptions escape into `WorkspacePanel` type initialization.
- Set the registered latch only after `RegisterClassHandler` succeeds so a later caller can retry after a transient failure.
- Keep the same `WorkspacePanel.Loaded` class-handler mechanism and tree content; no project mutation or startup scheduling changes.
- Make the static field initializer expose the actual registration result rather than unconditional `true`.
- Add a new focused static guard; do not edit the currently reserved BLT parity guard.

## Validation

Remote source/read-back only. Local WPF/BricsCAD execution and GitHub Actions are `NOT_RUN` unless independent evidence arrives.
