# Work claim — Generated Geometry Stale health null-element fail-visible

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-generated-geometry-stale-null-health-20260812-0721`
- Registered: `2026-08-12T07:21:00+07:00`
- Completed: `2026-08-12T07:32:00+07:00`
- Baseline main SHA: `a70ccd6b966fbbf18816d152f18cb0092586005b`
- Source commit on implementation branch: `70a052b1890e14959a5f7239315881d542151d3e`
- Smoke commit on implementation branch: `4824eae5df23da5bf3a7cedada6659eb3a06f751`
- Merged PR: `#618`
- Main squash SHA: `dfb5c06dafa0fa79d0817560bfab5587ebd2f988`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`GeneratedGeometryStaleHealthService.Inspect(ProjectState)` executed `if (element == null) continue;`. A malformed project with a null semantic element could therefore be reported clean by this provider. This was inconsistent with the repository's fail-visible health pattern: `ComprehensiveModelHealthService` deliberately converts diagnostic data failures such as `InvalidOperationException` into `HEALTH_PROVIDER_FAILED` errors instead of allowing invalid state to disappear from health output.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleNullHealthSmoke.cs`
- this claim file

## Completed contract

- direct stale-health inspection now rejects a null semantic element with `InvalidOperationException` instead of silently skipping it;
- the failure occurs before the provider can return a false-clean result;
- valid projects retain existing generated-output stale warning codes/messages;
- focused module-initializer smoke coverage pins direct fail-closed behavior, composite Error-level `HEALTH_PROVIDER_FAILED`, and unchanged `GENERATED_SOLID_STALE` behavior;
- `ComprehensiveModelHealthService` itself was not modified: its existing provider-failure conversion is reused;
- no generated-output stale semantics, CAD handles, mutation behavior, WPF, native BricsCAD code, persistence format, release/update code, or unrelated health providers changed.

## Validation evidence

- Re-fetched merged source from `main` after PR #618 and confirmed the null entry now throws instead of continuing.
- Re-fetched the merged smoke file from `main` and confirmed direct/composite/valid-state regression coverage is present.
- Compared concurrent `main` movement before merge; no concurrent commit touched the reserved source/test files.
- GitHub Actions were not manually dispatched.
- The committed smoke was not executed from this web session, and no BricsCAD V25 runtime PASS is claimed.
