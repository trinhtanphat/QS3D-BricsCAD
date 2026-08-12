# Work claim — Diagnostic summary severity smoke repair

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-diagnostic-severity-smoke-repair-20260812-0903`
- Registered: `2026-08-12T09:03:00+07:00`
- Completed: `2026-08-12T09:08:00+07:00`
- Baseline main SHA: `718f1d73095afce30452d5d3c8b50f4925f8c44f`
- Priority: P0 — current Core smoke execution was blocked by a regression fixture that violated a strengthened constructor invariant before reaching the exporter boundary it intended to test.
- Integration PR: `#674`
- Main integration commit: `3aab5473a3c8f5a654f33d0bed1410357025d358`

## Confirmed regression

The completed diagnostic-severity integrity lane added `ModelHealthIssue` constructor validation for undefined `HealthSeverity` values and also added `ProjectDiagnosticSummarySmoke.UndefinedSeverityFailsClosedWithoutReplacingExport()`. That smoke constructed `new ModelHealthIssue(..., (HealthSeverity)999, ...)`, so the constructor threw before the smoke entered its intended `ProjectDiagnosticSummaryExporter.Build/Export` assertions. The active local V25 Level probe independently reported the full Core smoke run stopping at this fixture.

## Implemented scope

- `tests/QS3D.Core.SmokeTests/ProjectDiagnosticSummarySmoke.cs`
- this claim file for close-out

## Completed contract

- The strengthened `ModelHealthIssue` constructor and exporter production guards are unchanged.
- The smoke now explicitly asserts the public constructor rejects undefined severity with `ArgumentOutOfRangeException`.
- Exporter defense-in-depth coverage is retained using a test-local reflection helper that first creates a valid issue and then mutates only the private severity backing field to build an otherwise-unrepresentable malformed fixture.
- Existing Build/Export undefined-severity checks and atomic destination-replacement assertion remain covered.
- No production source was changed.

## Validation evidence

- Claim registration: `e3c51040b6c860f44986779ac2036c7ebac3753b`.
- Branch test repair: `3f7509f98903a38b888b4c42202662d628d475f5`.
- The branch was synchronized twice with moving `main` without force-push; PR `#674` squash-merged to `main` as `3aab5473a3c8f5a654f33d0bed1410357025d358`.
- Post-merge readback confirms the public constructor assertion, reflection-only malformed fixture, exporter fail-closed assertions, and atomic destination preservation are present on `main`.
- The local V25 Level probe PR remains an independent lane and was not modified or merged by this claim.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Completion condition

`COMPLETED`: the smoke no longer fails prematurely at malformed public construction, deterministic constructor/exporter boundary coverage remains integrated on current `main`, and exact integration SHA/evidence is recorded above.
