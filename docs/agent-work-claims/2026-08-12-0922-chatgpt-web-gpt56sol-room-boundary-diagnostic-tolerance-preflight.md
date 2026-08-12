# Work claim — Room boundary diagnostic tolerance preflight

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:22:00+07:00`
- Baseline main SHA: `649918c8589d15dc720164c74ff1acaf7a31edf0`
- Priority: evidence-driven remote-safe Core input validation

## Reason

`RoomBoundaryDiagnosticService.Analyze` validates `minimumArea` before enumerating the caller-provided `source`, but does not validate `tolerance` until after it has materialized up to 5,001 segments and delegated to `RoomBoundaryEngine.Discover`. The underlying engine itself rejects non-finite/non-positive tolerance before enumerating its source. The diagnostic adapter therefore violates the same fail-fast contract: an invalid tolerance can trigger arbitrary enumerable work/side effects, a source exception, or the input-limit exception before the invalid public argument is reported.

## Reserved scope

Validate `tolerance` as finite and strictly positive in `RoomBoundaryDiagnosticService.Analyze` before enumerating `source`, preserving all valid discovery, diagnostics, privacy fingerprints, segment limits, topology behavior, and accepted-boundary handoff.

## Expected surfaces

- `src/QS3D.Core/Geometry/RoomBoundaryDiagnostics.cs`
- `tests/QS3D.Core.SmokeTests/RoomBoundaryDiagnosticTolerancePreflightSmoke.cs`
- `tests/QS3D.Core.SmokeTests/RoomBoundaryDiagnosticTolerancePreflightRegistration.cs`
- this claim file

## Excluded scope

- No changes to `RoomBoundaryEngine` topology/discovery math.
- No changes to tolerance defaults or minimum-area semantics.
- No BricsCAD host changes and no GitHub Actions dispatch.

## Validation plan

- Use an enumerable that records or throws on enumeration and assert invalid tolerance is rejected before its first `MoveNext`.
- Cover `NaN`, infinity, zero, and negative tolerance.
- Assert a valid empty-source diagnostic remains `NoInput` with the configured tolerance path unaffected.
- Re-fetch current `main` and target blobs after this claim lands and before each write; never force-push.
- Record static/exact-diff/ancestry verification only; do not claim an executed repository `dotnet` or BricsCAD V25 runtime PASS in this hosted session.

## Coordination

Recent claim/commit search found no reservation for `RoomBoundaryDiagnostics`, room-boundary diagnostic tolerance validation, or this validation-order defect.

## Completion condition

Current `main` rejects invalid diagnostic tolerance before touching the caller enumerable, includes focused CAD-independent smoke coverage, and this claim is marked `COMPLETED`.
