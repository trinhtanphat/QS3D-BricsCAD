# Work claim — Rebar fabrication qualification null-entry fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-fabrication-null-health-20260812-0754`
- Registered: `2026-08-12T07:54:00+07:00`
- Baseline main SHA: `f7d257200861948f09a3c16919374056e5b9737f`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`RebarFabricationQualificationHealthService.Inspect(ProjectState)` filters `project.Elements` with `x != null && HasGeneratedRebarOutput(x)`. When fabrication qualification is enabled, a malformed project containing a null semantic element can therefore be silently normalized into the ordinary `REBAR_FAB_OUTPUT_MISSING` path instead of failing visible through `ComprehensiveModelHealthService`'s existing provider-failure boundary. Other specialized health providers now fail closed on malformed collection entries; this provider should not report a false domain diagnosis for invalid project state.

## Reserved scope

- `src/QS3D.Core/Diagnostics/RebarFabricationQualificationHealthService.cs`
- `tests/QS3D.Core.SmokeTests/RebarFabricationQualificationNullHealthSmoke.cs`
- this claim file

## Acceptance checks

- With fabrication qualification enabled, a null semantic element causes direct Rebar Fabrication qualification inspection to throw `InvalidOperationException` before output classification.
- `ComprehensiveModelHealthService` surfaces that data failure as Error-level `HEALTH_PROVIDER_FAILED` for `RebarFabricationQualificationHealthService`.
- Existing valid requirement parsing, missing standard/revision, missing output, approval, and project-binding behavior remains unchanged.
- Focused module-initializer smoke coverage pins malformed direct/composite behavior and a representative valid missing-output path.
- No CAD/native/UI, fabrication engineering standard, generated ownership policy, persistence, release/update, or unrelated diagnostic provider changes.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime qualification claim.

## Coordination

Recent claim/commit search found active neighboring health lanes for Room Finish, Curtain Frame, Rebar Ownership, Grid Naming/Annotation and generated rebar variants, but no claim reserving `RebarFabricationQualificationHealthService` null-entry handling. This lane excludes those active scopes.
