# Work claim — Room boundary diagnostic tolerance preflight

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:22:00+07:00`
- Completed: `2026-08-12T09:25:00+07:00`
- Baseline main SHA: `649918c8589d15dc720164c74ff1acaf7a31edf0`
- Claim commit: `702228b294e7fa00c3c81df3628de84f6f8d4b5b`
- Fix commit: `3f8f8eaf41e344f364f179c95d195f399f2c3201`
- Smoke commit: `3395e7b7194bdb8c99c74d40976873be3e9a24ab`
- Registration commit: `44b1fec832766d04530c235b7ba7185d9c111477`
- Priority: evidence-driven remote-safe Core input validation

## Reason

`RoomBoundaryDiagnosticService.Analyze` validated `minimumArea` before enumerating the caller-provided `source`, but did not validate `tolerance` until after it had materialized up to 5,001 segments and delegated to `RoomBoundaryEngine.Discover`. The underlying engine itself rejects non-finite/non-positive tolerance before enumerating its source. The diagnostic adapter therefore violated the same fail-fast contract: an invalid tolerance could trigger arbitrary enumerable work/side effects, a source exception, or the input-limit exception before the invalid public argument was reported.

## Implemented

`RoomBoundaryDiagnosticService.Analyze` now rejects `NaN`, infinity, zero, and negative tolerance immediately after the existing `source`/`minimumArea` preflights and before `source.Take(...).ToList()`. Valid topology, privacy fingerprints, segment limits, defaults, minimum-area semantics, and accepted-boundary handoff are unchanged.

Focused CAD-independent smoke coverage uses an enumerable that throws on its first `MoveNext` and verifies all invalid tolerance forms fail with `ArgumentOutOfRangeException` for `tolerance` before enumeration begins. It also verifies a valid empty input remains a `NoInput` diagnostic with zero candidates/accepted boundaries. A dedicated module-initializer registration invokes the new smoke without modifying shared registration surfaces.

## Reserved scope

- `src/QS3D.Core/Geometry/RoomBoundaryDiagnostics.cs`
- `tests/QS3D.Core.SmokeTests/RoomBoundaryDiagnosticTolerancePreflightSmoke.cs`
- `tests/QS3D.Core.SmokeTests/RoomBoundaryDiagnosticTolerancePreflightRegistration.cs`
- this claim file

## Excluded scope

- No changes to `RoomBoundaryEngine` topology/discovery math.
- No changes to tolerance defaults or minimum-area semantics.
- No BricsCAD host changes and no GitHub Actions dispatch.

## Validation

- Exact product diff: two inserted lines in `RoomBoundaryDiagnostics.cs`; no unrelated product edit.
- Exact smoke diff: one focused 63-line smoke source.
- Exact registration diff: one dedicated 13-line module-initializer source.
- `44b1fec832766d04530c235b7ba7185d9c111477` was verified as an ancestor of observed current `main` `ca5fd0745b9b52167bd0323228206472f36e0783` with `behind_by: 0`; intervening commits touched disjoint surfaces.
- Static/exact-diff/ancestry verification only. No repository `dotnet` or licensed BricsCAD V25 runtime PASS is claimed from this hosted session.

## Coordination

Recent claim/commit search found no reservation for `RoomBoundaryDiagnostics`, room-boundary diagnostic tolerance validation, or this validation-order defect.

## Completion condition

Satisfied: current `main` rejects invalid diagnostic tolerance before touching the caller enumerable, includes focused CAD-independent smoke coverage, and this claim is `COMPLETED`.
