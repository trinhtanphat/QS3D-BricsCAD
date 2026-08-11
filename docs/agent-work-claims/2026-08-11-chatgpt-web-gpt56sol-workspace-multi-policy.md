# Work claim — Workspace multi-selection policy unification

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-1930`
- Registered: `2026-08-11T19:30:00+07:00`
- Baseline main SHA: `44dae5d5f3a6184cadf93d27661d1b71dc9bc860`
- Priority: continue-all source hardening after the Workspace semantic edit-policy batch

## Reserved scope

Unify Workspace multi-selection read-only/editability classification with the canonical Core `SemanticPropertyEditPolicy` so the multi-selection inspector no longer maintains a parallel denylist that can drift from single-selection/Core mutation rules.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs`
- `scripts/preflight-workspace-property-palette.py`
- read-only inspection of `src/QS3D.Core/Services/SemanticPropertyEditPolicy.cs`

## Excluded scope

- No changes to Core edit-policy semantics or key classification.
- No single-selection Property Inspector behavior changes beyond preserving current shared-policy behavior.
- No BricsCAD V25/WPF runtime qualification, Direct Draw, revisions, persistence, release, CI, or local-only gates.
- No changes to the active agent-registration protocol lane or the separately reserved Direct Draw Create Similar lane.

## Validation plan

- Review final source diff against the latest `main`.
- Require multi-selection read-only classification to delegate to `SemanticPropertyEditPolicy.IsEditablePropertyKey`.
- Extend the existing Workspace property-palette preflight so a local duplicate denylist cannot silently return.
- Do not dispatch GitHub Actions/build/release.

## Coordination

At registration time the agent-registration protocol claim excludes product source changes, and the concurrently published Create Similar claim reserves Direct Draw command/ownership surfaces. Neither overlaps this Workspace multi-selection policy lane. Recheck pushed claims and latest `main` before implementation and merge.

## Completion condition

The multi-selection adapter delegates editability to the canonical Core policy, the regression preflight enforces that single source of truth, the coherent implementation is merged to current `main`, and this claim is marked `COMPLETED` with exact pushed SHA and validation boundary.
