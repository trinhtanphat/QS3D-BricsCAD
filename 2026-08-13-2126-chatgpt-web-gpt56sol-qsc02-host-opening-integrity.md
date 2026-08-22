# Work claim — QSC-02 host/opening integrity rule family

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsc02-host-opening-integrity-20260813-2126`
- Registered: `2026-08-13T21:26:00+07:00`
- Completed: `2026-08-13T21:30:00+07:00`
- Baseline main SHA: `6aaa1d208376df1b067cff15b4f763061a7116b9`
- Claim commit: `72443d5734d7ea487eb431bfa980f1f3a73608a3`
- Implementation commit: `c0782c99a3f58de315ff7eda06d59bc136acf6cf`
- Regression commit: `3b4075f911f03c8f6aa213000177ac5dc82fe0e3`
- Priority: `QSC-02 / P2`

## Result

Added a deterministic declarative host/opening integrity profile over the five existing `ModelHealthService` findings: `MISSING_HOST`, `AMBIGUOUS_HOST`, `INVALID_HOST`, `HOST_REFERENCE_NON_CANONICAL`, and `INVALID_HOST_CATEGORY`. No health predicate or host/opening business logic was changed.

The focused smoke creates real Door/WallOpening states for missing, unresolved, wrong-category, non-canonical, and ambiguous hosts, runs the existing `ModelHealthService`, verifies exactly one expected host finding per fixture, resolves each through the QSC profile with severity parity, and verifies an unrelated `MISSING_FAMILY` finding remains unmapped.

## Actual scope

- `src/QS3D.Core/Diagnostics/QsHostOpeningIntegrityRuleFamily.cs`
- `tests/QS3D.Core.SmokeTests/QsHostOpeningIntegrityRuleFamilySmoke.cs`
- this claim file

## Coordination / reconciliation

The parallel QSC-02 semantic-readiness lane owns family/floor/zone/material/dimension metadata only and completed without touching this host/opening scope. After the regression commit, current `main` was verified as a strict descendant (`ahead_by: 2`, `behind_by: 0`); the only intervening changes were claim closeouts, not this lane's production/test files. Exact remote production and regression files were read back from current `main`.

## Validation boundary

- Exact GitHub remote source/test readback: verified.
- Current `ModelHealthService` readback confirms all five mapped host codes still emit `HealthSeverity.Error` through the existing `ValidateHost` path.
- Local managed compile/smoke execution: not executed because this environment has no `dotnet`, `msbuild`, `csc`, or `mcs` executable.
- Native BricsCAD V25 qualification: not executed and not claimed.
- GitHub Actions: not dispatched.
- Force-push: not used.
