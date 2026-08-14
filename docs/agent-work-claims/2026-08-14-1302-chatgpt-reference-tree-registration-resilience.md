# Agent work claim — reference Workspace tree registration resilience

- Agent: `chatgpt-web-gpt56sol-reference-tree-registration-resilience`
- Date: 2026-08-14
- Status: `COMPLETED`
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

`WorkspacePanel.ReferenceTreeRegistration` runs as a static field initializer and directly calls `ReferenceWorkspaceTreeAugmenter.EnsureRegistered()`. The prior implementation set `_registered = true` before `EventManager.RegisterClassHandler(...)` and let any exception escape. Therefore a WPF registration exception could both fail `WorkspacePanel` type initialization and leave the augmenter logically registered even though the class handler was never installed. The BLT Home file-actions claim reserved QuickWorkflow/preflight-blt, so this lane deliberately did not touch them.

## Result

- `b61531242c8545e7fd2060a5a9159699c112f41f` — `ReferenceWorkspaceTreeAugmenter.EnsureRegistered()` now returns a boolean, serializes registration, catches presentation-only registration failure, leaves the latch clear on failure, and sets `_registered` only after `RegisterClassHandler` succeeds.
- `f6f50a7d31b2de9cc7a2511b313a84ebe8c7def8` — the `WorkspacePanel` static registration field now exposes the actual fail-safe registration result rather than returning unconditional `true`.
- `fe5d8ede18bc1a424d89d18260dbab5628677b95` — adds an auto-discovered focused preflight protecting the success-latch ordering, no-throw static initialization boundary and retryable failure state.

The same `WorkspacePanel.Loaded` class-handler mechanism and the reference tree content remain unchanged. No project mutation, Ribbon/startup scheduling, Home-file-actions source, or LOCAL runner was changed.

## Validation

Remote read-back confirmed all three implementation surfaces on live `main`. `scripts/preflight-all.py` auto-discovers the new focused gate. Local WPF/BricsCAD execution and GitHub Actions are `NOT_RUN`; no runtime or executable PASS is fabricated.
