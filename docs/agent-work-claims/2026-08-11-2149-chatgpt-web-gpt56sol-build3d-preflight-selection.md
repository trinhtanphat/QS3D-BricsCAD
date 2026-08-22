# Agent work claim — QS3DBUILD3D preflight selection preservation

- Agent: `chatgpt-web-gpt56sol-build3d-preflight-selection-20260811-2149`
- Started: `2026-08-11T21:49:00+07:00`
- Completed: `2026-08-11T22:15:00+07:00`
- Status: `COMPLETED`
- Task ID / title: `QS3DBUILD3D preflight selection preservation`
- Source / user driver: user requested `continue all`; primary remaining product priority is stabilizing native 3D build behavior.
- Baseline main SHA: `408c47b33703cda109b22acf705305ef9653e7f3`

## Objective

Keep `QS3DBUILD3D` preflight read-only with respect to the user's PICKFIRST/implied selection. The command previously rebased implied selection to semantic source IDs before source snapshot validation and wall-source validation; those validation branches could return without building anything while still changing the user's selection. Read the already-resolved source handles directly for preflight instead, while preserving the generated-solid/source selection applied after a successful build.

## Expected path surfaces

- `src/QS3D.BricsCAD.V25/Build3DCommands.cs`
- canonical focused regression `scripts/preflight-build3d-canonical.py`
- this claim file for lifecycle close-out only

## Explicit exclusions

- `src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs` and the adjacent PlanTo3D lane
- Direct Draw / Create Similar command paths
- solid topology/builders and Core geometry generation
- `src/QS3D.Core/Rules/**`, QTO rule engine, rule-create UI
- persistence/schema, updater/licensing, Workspace presentation, Rebar parser work
- every other agent's claim file

## Dependencies / risks / merge constraints

- Main was highly concurrent throughout the change; writes were refreshed/retried without force-push and current `main` was verified after merge.
- The adjacent PlanTo3D source-freshness work remained outside this claim; this change is restricted to `QS3DBUILD3D` preflight selection behavior.
- Existing fail-closed semantic/source validation, scoped regeneration, native ownership rollback, and successful post-build generated selection are preserved.
- No BricsCAD V25 runtime proof is claimed remotely.

## Validation gates

- `Build3DCommands.cs` now uses `EntitySnapshotReader.ReadHandles(document, sourceHandles)` for Build3D source preflight.
- `document.Editor.SetImpliedSelection(sourceIds.ToArray())` and `EntitySnapshotReader.ReadImpliedSelection(document)` are no longer used by the Build3D preflight path.
- Canonical static gate requires direct-handle source reads, preserves live source-ID resolution and successful `CadHandleService.Select(document, generatedHandles)`, and rejects reintroduction of implied-selection mutation/readback.
- Current `main` source was re-read after merge and the expected contracts were present.
- No GitHub Actions were dispatched and no BricsCAD V25 runtime result is claimed.

## Implementation

- `08181759d27d5e6e15cbfedc7dbb81d9fc938c55` — `fix(build3d): preserve PICKFIRST during preflight`
- `d8ee7027e6d078bdd98854a2130f2cbf9f59ff9b` — `test(build3d): guard preflight selection preservation`
- ancestry check from implementation SHA to verified `main` showed `behind_by: 0`; current `main` retained the new source and regression contracts.

## Exact completion condition

Completed: `QS3DBUILD3D` early-return preflight paths no longer change implied selection solely to inspect source CAD, existing semantic/native build and post-build selection behavior remain intact, focused static regression is merged on `main`, and this claim is closed with the exact implementation/test SHAs.