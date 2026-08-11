# Agent work claim — QS3DBUILD3D source-selection handoff

- Agent: `chatgpt-web-gpt56sol-build3d-source-selection-handoff-20260811-2216`
- Started: `2026-08-11T22:16:00+07:00`
- Status: `ACTIVE`
- Task ID / title: `QS3DBUILD3D source-selection handoff after read-only preflight`
- Source / user driver: follow-up validation of the completed PICKFIRST-preflight fix under the user's `continue all` request.
- Baseline main SHA: `c5e1a22e1806fd80299830e0fa4843fd18454d0a`

## Objective

Preserve the read-only preflight introduced for `QS3DBUILD3D` while restoring the explicit source-selection handoff required by the existing native builders. `WallSolidBuilder` and the other canonical builders consume `Editor.SelectImplied()` during actual native dispatch; therefore Build3D must set resolved semantic source IDs only after preflight/regeneration succeeds and immediately before `BuildCategory`, not during source validation.

## Expected path surfaces

- `src/QS3D.BricsCAD.V25/Build3DCommands.cs`
- `scripts/preflight-build3d-canonical.py`
- this claim file for close-out

## Explicit exclusions

- native builder internals/topology
- PlanTo3D, Direct Draw, Create Similar
- Core geometry/rules/QTO
- Workspace, persistence, installer/licensing and other agents' claims

## Dependencies / risks / merge constraints

- Follow-up is necessary because direct-handle preflight alone leaves generated-host selections active, while native builders read `SelectImplied()`.
- Keep all preflight early-return branches free of source-selection mutation.
- Rebase source IDs only after semantic regeneration succeeds and directly before native `BuildCategory` dispatch.
- Preserve successful generated-solid selection in `FinalizeUi`.
- Main is highly concurrent; refresh before writes, never force-push, and do not claim BricsCAD runtime proof.

## Validation gates

- direct-handle preflight remains present;
- `SetImpliedSelection(sourceIds.ToArray())` occurs after `RegenerateDirtySubset(...)` and before `BuildCategory(...)`;
- no source-selection mutation occurs before wall/source validation;
- canonical static gate enforces this ordering plus generated selection after success;
- no GitHub Actions dispatch.

## Exact completion condition

Canonical Build3D keeps PICKFIRST unchanged across preflight early returns, supplies resolved source IDs to the implied-selection-based native builders only at dispatch time, the regression gate locks the ordering, current `main` is verified, and this claim is marked `COMPLETED` with exact SHAs.