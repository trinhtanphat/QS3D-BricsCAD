# Work claim — Generated Rebar Mode health null-element fail-visible

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-rebar-mode-null-health-20260812-0734`
- Registered: `2026-08-12T07:34:00+07:00`
- Baseline main SHA: `2480dc798af64c3acc37136d20c1d74c8ed2a104`
- Priority: evidence-driven diagnostic integrity during owner-requested `continue all`

## Confirmed defect

`GeneratedRebarModeHealthService.Inspect(ProjectState)` silently skips null semantic elements with `if (element == null) continue;`. That allows this specialized provider to return a false-clean result for malformed project state rather than participating in the fail-visible provider contract used by `ComprehensiveModelHealthService`.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs`
- isolated focused Core smoke regression for this provider
- this claim file for close-out

## Contract

- direct Generated Rebar Mode health inspection rejects null semantic elements before returning results;
- composite health surfaces this provider failure as Error-level `HEALTH_PROVIDER_FAILED` via existing wrapper behavior;
- valid mode diagnostics remain unchanged, including existing missing/unknown/mismatch/metadata-invalid codes;
- no rebar geometry, generation, quantity, CAD handles, persistence, WPF/native BricsCAD, release/update, or unrelated health-provider behavior changes.

## Validation plan

Add isolated module-initializer smoke coverage for direct fail-closed behavior, composite provider-failure visibility, and one valid existing `GENERATED_REBAR_MODE_MISSING` warning path. Re-fetch current `main` before merge and do not overwrite concurrent work.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
