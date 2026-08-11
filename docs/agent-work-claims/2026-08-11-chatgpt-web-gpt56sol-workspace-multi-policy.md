# Work claim — Workspace multi-selection policy unification

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T19:30:00+07:00`
- Baseline main SHA: `0296f6f31e28a598474875805b934edc26c98e60`
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
- No changes to the active agent-registration protocol lane.

## Validation plan

- Review final source diff against the latest `main`.
- Require multi-selection read-only classification to delegate to `SemanticPropertyEditPolicy.IsEditablePropertyKey`.
- Extend the existing Workspace property-palette preflight so a local duplicate denylist cannot silently return.
- Do not dispatch GitHub Actions/build/release.

## Coordination

The only visible active neighboring claim at registration time owns the agent-registration protocol and explicitly excludes product source changes. This lane is non-overlapping. Recheck pushed claims and latest `main` before implementation and merge.

## Completion condition

The multi-selection adapter delegates editability to the canonical Core policy, the regression preflight enforces that single source of truth, the coherent implementation is merged to current `main`, and this claim is marked `COMPLETED` with exact pushed SHA and validation boundary.
