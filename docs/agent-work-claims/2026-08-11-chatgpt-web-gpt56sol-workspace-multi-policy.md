# Work claim — Workspace multi-selection policy unification

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-workspace-1930`
- Registered: `2026-08-11T19:30:00+07:00`
- Completed: `2026-08-11T19:44:00+07:00`
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

## Completion

- `315b26440c35bbe1af244254aa3e68a7ed4c7b45` — `fix(workspace): unify multi-selection edit policy`
  - removed the Workspace-local source/identity/ownership denylist;
  - multi-selection read-only classification now delegates to `SemanticPropertyEditPolicy.IsEditablePropertyKey`;
  - existing stale-project, exact CAD-selection, and `SemanticSelectionBulkEditService` mutation boundaries were preserved.
- `3f16ab84f3a0d03901cd17b1eb9a447d805fb7ff` — `test(workspace): guard shared multi-selection policy`
  - requires the shared Core policy call;
  - rejects reintroduction of the previous duplicated Workspace denylist markers;
  - preserves existing bulk atomicity/stale-selection source contracts.

No GitHub Actions, build, release, WPF rendering, NETLOAD, or BricsCAD V25 runtime qualification was executed or claimed in this remote lane.
