# Agent work claim — Material Catalog control-character persistability

- Agent: `chatgpt-56sol-material-control-persistability`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `50df6c9ba9a794c604368dda85dc40329f48bff2`
- Implementation branch: `agent/chatgpt-56sol/material-control-persistability`
- Planned integration branch: `integration/chatgpt-material-control-persistability-20260814`

## Reserved scope

Fix one confirmed Core persistability defect in `ProjectMaterial`: required material identity text validates trimmed length and well-formed Unicode but accepts control characters. A custom material Id or Name containing an XML-illegal control character can therefore enter the canonical material catalog and later cross QSDB/XML persistence boundaries; a renamed material Name can also propagate into Family/Element property values.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` — make required material Id/Name validation reject control characters before catalog/project mutation.
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs` — focused deterministic regression proving control-character Id/Name rejection while preserving project metadata/revision state.
- this claim file for final closeout evidence.

## Excluded scope

- Material Catalog metadata rollback/static-gate work, raw/padded FamilyId regressions, UI behavior, reporting/export semantics, and unrelated Family/Floor/Zone atomicity lanes.
- optional material Unit/Description policy unless independently proven and re-claimed; this lane changes required Id/Name only.
- QSDB schema/version changes, native V25/V26 source, private-DWG evidence, signing, packaging, LOCAL_ONLY BricsCAD runtime qualification, and licensed host validation.
- manual GitHub Actions dispatch/rerun/cancel under `CI_POLICY.md`.

## Validation plan

- verify this claim is reachable from refreshed `main` and re-check concurrent claim/commit deltas before implementation;
- keep production change to the required-text validator only;
- add focused smoke cases for `U+0001` in custom material Id and Name and assert rejection happens before `ProjectState.ChangeVersion`, `UpdatedUtc`, or material metadata change;
- inspect exact source/test diff and compile semantics statically; do not claim managed/native PASS without executable evidence;
- reconcile through the planned integration branch, land source/test once into refreshed `main`, observe only automatically triggered CI evidence, verify ancestry/readback, then close this claim `COMPLETED` with exact SHAs.

## Evidence before registration

At baseline `50df6c9ba9a794c604368dda85dc40329f48bff2`, `ProjectMaterial.Required(...)` trims, enforces length, and calls `RequireWellFormedUnicode(...)`, but has no control-character rejection. `ProjectMaterialCatalog.UpsertCustom(...)` constructs that material before mutating the project, so strengthening this validator provides a narrow fail-fast persistence guard. The existing focused `ProjectMaterialCatalogSmoke` covers round-trip, rename/reference behavior, corrupt graph/storage, and built-in shadowing but not control-character Id/Name rejection. Repository history search shows sibling control-character guards for Project/Property/Snapshot/Family identities but no Material Catalog control-character lane.

## Completion condition

The exact validator fix and focused regression are reachable from refreshed `main` through the required agent/integration flow; no unrelated source is modified; remote ancestry/readback and available auto-CI evidence are recorded; validation limitations are explicit; this claim is then marked `COMPLETED` with claim/source/regression/integration/main SHAs.