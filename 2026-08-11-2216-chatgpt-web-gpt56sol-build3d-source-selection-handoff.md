# Agent work claim — QS3DBUILD3D source-selection handoff

- Agent: `chatgpt-web-gpt56sol-build3d-source-selection-handoff-20260811-2216`
- Started: `2026-08-11T22:16:00+07:00`
- Completed: `2026-08-11T22:23:00+07:00`
- Status: `COMPLETED`
- Task ID / title: `QS3DBUILD3D source-selection handoff after read-only preflight`
- Source / user driver: follow-up validation of the completed PICKFIRST-preflight fix under the user's `continue all` request.
- Baseline main SHA: `c5e1a22e1806fd80299830e0fa4843fd18454d0a`

## Objective

Preserve the read-only preflight introduced for `QS3DBUILD3D` while restoring the explicit source-selection handoff required by the existing native builders. `WallSolidBuilder` and the canonical native builders consume `Editor.SelectImplied()` during actual native dispatch; Build3D therefore sets resolved semantic source IDs only after preflight/regeneration succeeds and immediately before `BuildCategory`, not during source validation.

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

- Follow-up was necessary because direct-handle preflight alone leaves generated-host selections active while native builders read `SelectImplied()`.
- All source/wall preflight early-return branches remain before source-selection mutation.
- Resolved source IDs are handed off after semantic regeneration succeeds and directly before native `BuildCategory` dispatch.
- Successful generated-solid selection remains in `FinalizeUi`.
- Main was highly concurrent; writes were retried without force-push and current `main` was re-read after merge.
- No BricsCAD V25 runtime proof is claimed remotely.

## Validation gates

- direct-handle preflight remains `EntitySnapshotReader.ReadHandles(document, sourceHandles)`;
- current source order is source resolve -> direct snapshot read -> wall/source validation -> `RegenerateDirtySubset(...)` -> `SetImpliedSelection(sourceIds.ToArray())` -> `BuildCategory(...)`;
- `EntitySnapshotReader.ReadImpliedSelection(document)` is absent from canonical Build3D source preflight;
- `FinalizeUi` still selects generated handles after a successful build, falling back to source handles only when needed;
- canonical static gate requires exactly one resolved-source implied-selection handoff and enforces the ordering above;
- no GitHub Actions were dispatched.

## Implementation

- `fead64e5acac427a05378b8ddcd37d874d5a1e01` — `fix(build3d): hand sources to native dispatch`
- `a101fd199c2a8bdfdb52c0c64c2d71832a634559` — `test(build3d): guard source selection handoff`
- current `main` was re-read at `8dfcc7ab81bdee86a54eef2a64e1f8fdf52672f5`; both source and gate contracts were present after concurrent commits.

## Exact completion condition

Completed: canonical Build3D keeps PICKFIRST unchanged across preflight early returns, supplies resolved source IDs to the implied-selection-based native builders only at dispatch time, the regression gate locks the ordering, current `main` retains the change, and this claim is closed with exact implementation/test SHAs.