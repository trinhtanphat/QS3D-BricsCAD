# Work claim — Material catalog idempotent custom upsert

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-catalog-idempotent-upsert`
- Registered: `2026-08-12T01:14:00+07:00`
- Completed: `2026-08-12T01:16:00+07:00`
- Baseline main SHA: `45b4cc270876a15e01673feb31924d1de82099ad`
- Priority: P2 semantic revision integrity — a no-op catalog save must not create a project revision.

## Confirmed defect

`ProjectMaterialCatalog.UpsertCustom(...)` always called `project.Touch()` and rewrote `QS3D.MaterialCatalog.v1`, even when the existing custom material with the same id already had exactly the same normalized name, unit and description. An idempotent save therefore advanced `ProjectState.ChangeVersion` without any semantic state change.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogIdempotentUpsertSmoke.cs`
- this claim file

## Completed contract

- If the existing custom material is semantically identical after normal constructor normalization, `UpsertCustom(...)` returns the existing parsed material without touching the project or rewriting metadata.
- Exact case-sensitive comparison is retained for name/unit/description, so case-only name changes still flow through the existing rename/reference propagation path.
- Real updates, renames, new materials, duplicate-name protection, built-in shadow protection, reference propagation and catalog encoding remain unchanged.

## Published commits

- Source fix: `4413118966c11fe2a8cf3b7257ac16bd920cefd9` — `fix(material): make identical catalog upsert revision-neutral`
- Focused regression: `52129d5d40da670383fd77d224186ad17bd4f88c` — `test(material): guard idempotent catalog upsert`

## Validation notes

- Exact source diff was reviewed after publication: the only behavioral change is an early return for an existing material whose normalized name/unit/description are identical, plus the focused equality helper.
- Regression source checks unchanged `ChangeVersion` and serialized metadata for an identical normalized upsert, then verifies a real description update advances revision once and persists the new value.
- No GitHub Actions were dispatched.
- This hosted environment did not execute the .NET smoke binary or BricsCAD runtime, so no executable/runtime PASS is claimed.

## Completion condition

Satisfied: identical material upserts are revision-neutral, real updates still mutate once, regression source is on `main`, and this claim is closed.