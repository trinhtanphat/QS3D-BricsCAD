# Agent work claim — QS3DBUILD3D preflight selection preservation

- Agent: `chatgpt-web-gpt56sol-build3d-preflight-selection-20260811-2149`
- Started: `2026-08-11T21:49:00+07:00`
- Status: `ACTIVE`
- Task ID / title: `QS3DBUILD3D preflight selection preservation`
- Source / user driver: user requested `continue all`; primary remaining product priority is stabilizing native 3D build behavior.
- Baseline main SHA: `408c47b33703cda109b22acf705305ef9653e7f3`

## Objective

Keep `QS3DBUILD3D` preflight read-only with respect to the user's PICKFIRST/implied selection. The command currently rebases implied selection to semantic source IDs before source snapshot validation and wall-source validation; those validation branches can return without building anything while still changing the user's selection. Read the already-resolved source handles directly for preflight instead, while preserving the builder-owned source selection needed during actual native build and the generated-solid selection used after a successful build.

## Expected path surfaces

- `src/QS3D.BricsCAD.V25/Build3DCommands.cs`
- one focused static regression under `tests/` if an existing Build3D gate is not suitable
- this claim file for lifecycle close-out only

## Explicit exclusions

- `src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs` and the active PlanTo3D source-freshness lane
- Direct Draw / Create Similar command paths
- solid topology/builders and Core geometry generation
- `src/QS3D.Core/Rules/**`, QTO rule engine, rule-create UI
- persistence/schema, updater/licensing, Workspace presentation, Rebar parser work
- every other agent's claim file

## Dependencies / risks / merge constraints

- Main is highly concurrent; refresh immediately before every write and never force-push.
- The active PlanTo3D source-freshness claim is adjacent in user-visible 3D capability but owns `PlanTo3DCommands.cs`; this claim is restricted to `QS3DBUILD3D` preflight selection behavior.
- Preserve current fail-closed semantic/source validation, scoped regeneration, native ownership rollback, and successful post-build generated selection.
- Do not claim BricsCAD V25 runtime proof remotely.

## Validation gates

- source contract uses `EntitySnapshotReader.ReadHandles(document, sourceHandles)` (or equivalent direct-handle read) for Build3D preflight rather than mutating implied selection merely to read snapshots;
- no `SetImpliedSelection(sourceIds...)` occurs before source/wall preflight validation;
- focused static regression guards both the no-preflight-selection-mutation contract and the successful generated-selection path;
- no GitHub Actions dispatch.

## Exact completion condition

`QS3DBUILD3D` early-return preflight paths no longer change implied selection solely to inspect source CAD, existing semantic/native build and post-build selection behavior remain intact, focused source/static validation is merged on current `main`, and this claim is marked `COMPLETED` with the exact implementation/test SHA(s).