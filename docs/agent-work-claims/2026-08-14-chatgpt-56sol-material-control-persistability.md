# Agent work claim — Material Catalog control-character persistability

- Agent: `chatgpt-56sol-material-control-persistability`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `50df6c9ba9a794c604368dda85dc40329f48bff2`
- Claim commit on main: `5d4f3b2eca6b62746bdcef9b41b45df5315f2815`
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

## Validation completed

- Production source commit: `5790485c4d975d6ecc6d9f9d956400f2ba207e8b` (`fix(core): reject control characters in material identity`).
- Focused regression commit: `f4616717550d9dd1cffb08e11b0dd96021aa4638` (`test(core): guard material control persistability`).
- Agent → integration PR #1317 merged at `557bb1ef7ea1e689087e4214df896e836914f454` after GitHub reported it mergeable.
- Refreshed `main` advanced independently to `d504675082a9359b1b2205b490e07937cf92a917`; PR #1318 reconciled that current-main delta into the same integration branch at `cffa573a768c55fc45dbed35d83f9b26372e1206`.
- Final integration → `main` PR #1319 merged at `9016c85ecdc8ade3d10498fe60afdffb18e84239`; remote readback on that SHA confirms the `char.IsControl` required-text guard and both `U+0001` Id/Name regression cases are present.
- The focused regression captures `ProjectState.ChangeVersion`, `UpdatedUtc`, and metadata count before each rejected mutation and confirms the custom catalog remains empty.
- Final PR diff was limited to the two reserved files. The contents API also normalized the pre-existing no-final-newline state of `ProjectMaterialCatalog.cs`; this is formatting-only and does not alter behavior.
- Immediately after landing, `fetch_commit_workflow_runs` and combined commit status returned no workflow/status records for `9016c85ecdc8ade3d10498fe60afdffb18e84239`. No manual Actions dispatch/rerun/cancel was performed, so no CI PASS/FAIL is inferred from that observation.
- No executable checkout, managed build, smoke executable run, licensed BricsCAD host, native V25/V26 qualification, private-DWG probe, signing, or packaging validation was performed in this lane; no such PASS is claimed.

## Evidence before registration

At baseline `50df6c9ba9a794c604368dda85dc40329f48bff2`, `ProjectMaterial.Required(...)` trimmed, enforced length, and called `RequireWellFormedUnicode(...)`, but had no control-character rejection. `ProjectMaterialCatalog.UpsertCustom(...)` constructs that material before mutating the project, so strengthening this validator provides a narrow fail-fast persistence guard. The existing focused `ProjectMaterialCatalogSmoke` covered round-trip, rename/reference behavior, corrupt graph/storage, and built-in shadowing but not control-character Id/Name rejection. Repository history search showed sibling control-character guards for Project/Property/Snapshot/Family identities but no Material Catalog control-character lane.

## Completion record

`COMPLETED`: claim-first ownership was published on `main`; source and focused regression were implemented only on the agent branch; concurrent `main` was reconciled into one integration branch; the two-file lane landed once through PR #1319; remote main ancestry/readback was verified; available auto-CI evidence and validation limitations are recorded above. A later unrelated reporting-smoke commit `525bd016a2d15a2d2c0e34a226597b7360dbae15` advanced `main` after the landing while preserving `9016c85ecdc8ade3d10498fe60afdffb18e84239` as its direct parent.