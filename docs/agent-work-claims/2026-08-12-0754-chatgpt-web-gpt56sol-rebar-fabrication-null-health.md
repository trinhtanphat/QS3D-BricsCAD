# Work claim — Rebar fabrication qualification null-entry fail-visible

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-fabrication-null-health-20260812-0754`
- Registered: `2026-08-12T07:54:00+07:00`
- Completed: `2026-08-12T07:57:00+07:00`
- Baseline main SHA: `f7d257200861948f09a3c16919374056e5b9737f`
- Claim commit: `da3fbf6f35151d32b2b84b1d9ba57c232201cb7b`
- Source implementation commit: `b5a9b2bc879682fbf937ae3e3c95696efe9f0cc1`
- Regression commit: `dab097a849d95dc19ecd8170f795909e34df6278`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`RebarFabricationQualificationHealthService.Inspect(ProjectState)` filtered `project.Elements` with `x != null && HasGeneratedRebarOutput(x)`. With a valid enabled fabrication-qualification requirement, a malformed project containing a null semantic element could therefore be silently normalized into the ordinary `REBAR_FAB_OUTPUT_MISSING` path instead of failing visible through `ComprehensiveModelHealthService`'s provider-failure boundary.

## Completed change

- A valid enabled fabrication requirement now rejects a null semantic element with `InvalidOperationException` before standard/revision/output classification.
- `ComprehensiveModelHealthService` retains its existing provider wrapper and therefore surfaces this malformed state as Error-level `HEALTH_PROVIDER_FAILED` attributed to `RebarFabricationQualificationHealthService`.
- Existing malformed requirement-token behavior is intentionally preserved: the established `REBAR_FAB_REQUIREMENT_INVALID` fail-closed path still collects its existing standard/revision/output diagnostics rather than being replaced by the new null-entry exception.
- Valid qualification with standard/revision metadata and no generated rebar output still produces the existing `REBAR_FAB_OUTPUT_MISSING` Error.

## Reserved scope respected

- `src/QS3D.Core/Diagnostics/RebarFabricationQualificationHealthService.cs`
- `tests/QS3D.Core.SmokeTests/RebarFabricationQualificationNullHealthSmoke.cs`
- this claim file

No CAD/native/UI, fabrication engineering standard, generated ownership policy, persistence, release/update, or unrelated diagnostic provider behavior was changed.

## Validation evidence

- Re-read current `main` source after publication and confirmed the null guard runs only after the requirement is both required and syntactically valid, before output filtering/classification.
- Re-read the existing `RebarFabricationQualificationSmoke`; its invalid-requirement test includes a null element and continues to exercise the historical diagnostic path because the new guard does not run for malformed requirement tokens.
- Added module-initializer smoke coverage proving direct null rejection, composite provider-failure visibility, and unchanged valid missing-output diagnosis.
- Remote connector validation was source/static only; no local `dotnet` build or Core smoke process was executed/claimed.
- No GitHub Actions were dispatched and no BricsCAD V25/V26 runtime qualification is claimed.

## Coordination

Recent claim/commit search found active neighboring health lanes for Room Finish, Curtain Frame, Rebar Ownership, Grid Naming/Annotation and generated rebar variants, but no claim reserving `RebarFabricationQualificationHealthService` null-entry handling. No force-push was used.
