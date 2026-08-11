# Work claim — Model Health issue severity integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `569e3d09d1a2d973f2ea655fa09c8f3f90131ba1`
- Priority: owner-requested continue-all Core correctness audit

## Confirmed defect

`ModelHealthIssue` currently accepts any underlying integer cast to `HealthSeverity`. An undefined value such as `(HealthSeverity)123` can therefore escape the Core diagnostics boundary and later be serialized, displayed, filtered, or counted as a non-canonical severity.

## Reserved scope

Harden the immutable Core diagnostic issue boundary so only defined `HealthSeverity` values can be constructed, and add a focused smoke regression for the invalid-enum case.

## Expected surfaces

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused smoke file under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Explicit exclusions / coordination

- No Model Health WPF/UI changes; the completed UI claim explicitly excluded Core Diagnostics.
- No changes to generated-solid runtime health inspection, which is concurrently owned by the `generated-solid-health-integrity` claim.
- No diagnostic rule redesign, persistence format change, CAD mutation, BricsCAD V25/native behavior, release/signing work, or GitHub Actions dispatch.
- Do not normalize or reject `Code`, `Message`, or `ElementId` in this lane unless a separate defect is proven and separately claimed.

## Validation plan

- Re-fetch current `main` and the exact Core source after this registration lands.
- Add a fail-fast `ArgumentOutOfRangeException` guard using framework-compatible enum validation.
- Add a deterministic Core smoke regression proving undefined severity is rejected while all defined severities remain constructible.
- Re-fetch landed files and inspect commit diffs on `main`.
- Do not claim local executable smoke/build or licensed BricsCAD runtime PASS without actual evidence.

## Completion condition

Current `main` rejects undefined `HealthSeverity` values at `ModelHealthIssue` construction, focused regression coverage is present, and this claim is closed as `COMPLETED` with exact implementation SHA(s) and validation actually performed.
