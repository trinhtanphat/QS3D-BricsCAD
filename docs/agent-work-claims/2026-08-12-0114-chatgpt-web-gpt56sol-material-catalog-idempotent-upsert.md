# Work claim — Material catalog idempotent custom upsert

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-idempotent-upsert`
- Registered: `2026-08-12T01:14:00+07:00`
- Baseline main SHA: `45b4cc270876a15e01673feb31924d1de82099ad`
- Priority: P2 semantic revision integrity — a no-op catalog save must not create a project revision.

## Confirmed defect

`ProjectMaterialCatalog.UpsertCustom(...)` always calls `project.Touch()` and rewrites `QS3D.MaterialCatalog.v1`, even when the existing custom material with the same id already has exactly the same normalized name, unit and description. An idempotent save therefore advances `ProjectState.ChangeVersion` without any semantic state change.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogIdempotentUpsertSmoke.cs` (new focused auto-registered smoke)
- this claim file

## Intended contract

- If the existing custom material is semantically identical after normal constructor normalization, return the existing material without touching the project or rewriting metadata.
- Real updates, renames, new materials, duplicate-name protection, built-in shadow protection, reference propagation and catalog encoding remain unchanged.

## Validation plan

- Add a material, snapshot `ChangeVersion` and serialized catalog metadata, then upsert the identical normalized values and require both to remain unchanged.
- Confirm a real description update still advances the project revision exactly once and persists the new value.
- Re-fetch source before update, SHA-guard write, inspect exact diffs, then close claim.
- No GitHub Actions dispatch; no executable .NET/BricsCAD runtime PASS claim from this hosted environment.

## Completion condition

Identical material upserts are revision-neutral, real updates still mutate once, regression source is on `main`, and this claim is closed.