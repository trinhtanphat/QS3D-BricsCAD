# Work claim — Generated Rebar Mode health null-element fail-visible

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-generated-rebar-mode-null-health-20260812-0734`
- Registered: `2026-08-12T07:34:00+07:00`
- Completed: `2026-08-12T07:38:00+07:00`
- Baseline main SHA: `2480dc798af64c3acc37136d20c1d74c8ed2a104`
- Source commit on implementation branch: `66c4a7629dfa95a53b722e348c6c4becbdcc232e`
- Smoke commit on implementation branch: `82f133bdb77720e3a3ccd23366153cac9811f523`
- Merged PR: `#622`
- Main squash SHA: `588e7640da962c9afcd3723f59cb90c22f9eb556`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`GeneratedRebarModeHealthService.Inspect(ProjectState)` silently skipped null semantic elements with `if (element == null) continue;`. That allowed this specialized provider to return a false-clean result for malformed project state rather than participating in the fail-visible provider contract used by `ComprehensiveModelHealthService`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedRebarModeNullHealthSmoke.cs`
- this claim file

## Completed contract

- direct Generated Rebar Mode health inspection now rejects null semantic elements with `InvalidOperationException` before returning results;
- composite health surfaces this provider failure as Error-level `HEALTH_PROVIDER_FAILED` through the existing wrapper behavior;
- valid mode diagnostics remain unchanged, including existing missing/unknown/mismatch/metadata-invalid codes;
- focused module-initializer smoke coverage pins direct fail-closed behavior, composite provider-failure visibility, and the existing `GENERATED_REBAR_MODE_MISSING` warning path;
- no rebar geometry, generation, quantity, CAD handles, persistence, WPF/native BricsCAD, release/update, or unrelated health-provider behavior changed.

## Validation evidence

- Re-fetched merged source from `main` after PR #622 and confirmed null entries now throw instead of continuing.
- Re-fetched merged smoke from `main` and confirmed direct/composite/valid-state coverage is present.
- Re-checked concurrent `main` movement before merge; no concurrent commit touched the reserved source/test files.
- GitHub Actions were not manually dispatched.
- The committed smoke was not executed from this web session, and no BricsCAD V25 runtime PASS is claimed.
