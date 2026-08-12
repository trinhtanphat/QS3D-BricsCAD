# Work claim — legacy Room Finish generator numeric safety

- Status: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-room-finish-generator-numeric-safety`
- Registered: `2026-08-11T23:24:00+07:00`
- Baseline main SHA: `11486e07d818ec1df718f3775a4b3c23e15123da`
- Priority: evidence-driven remote-safe Core data-integrity hardening found during owner-requested continue-all audit

## Confirmed defect

Legacy `ElementInstance` metric properties are mutable `double` values and can contain negative, `NaN`, or infinity values. `RoomFinishGenerator.Generate(...)` copied `AreaM2`, `InnerPerimeterM`, and `SideAreaM2` directly into newly generated finish elements without validating them. This permitted the generator itself to create invalid quantity-bearing elements and relied on a later reporting boundary to fail.

## Reserved scope

Fail closed inside the CAD-independent legacy Room Finish generator when an enabled generated finish would consume a non-finite or negative length/area metric. Preserve valid zero/non-negative finite values, disabled finish behavior, generated IDs/families/materials/floor/source handles, and all modern semantic Room Finish lifecycle behavior outside this legacy utility.

## Expected surfaces

- `src/QS3D.Core/Services/RoomFinishGenerator.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishGeneratorNumericSafetySmoke.cs`
- `tests/QS3D.Core.SmokeTests/RoomFinishGeneratorNumericSafetySmokeRegistration.cs`
- this claim file

## Excluded scope

- No `ElementInstance` schema redesign or setter validation.
- No Room Finish native BricsCAD commands, modeless UI, schedules/tables, project lifecycle, synchronization, or ownership changes.
- No reporting changes; reporting already rejects invalid non-negative metrics.
- No BricsCAD V25 runtime qualification and no GitHub Actions dispatch.

## Delivered behavior

- Enabled finish outputs now validate the length/area values they consume before constructing a generated `ElementInstance`.
- Negative, `NaN`, and infinite consumed metrics fail closed.
- Disabled outputs do not validate unrelated metrics, so a bad unused area does not block a valid skirting-only generation.
- Existing valid output ordering, generated identities, family/material/floor values, and source-handle propagation remain unchanged.

## Commits

- Registration: `37caa3451bad50de3e76b09dcccdea0ae5fdaa6c` — `chore(agent): claim room finish generator numeric safety`.
- Implementation: `be76815240232cd783aefcb2c7c56a75eed76440` — `fix(room): reject invalid legacy finish metrics`.
- Regression: `144baf125b06482e0616521709b4a0946dd9beb9` — `test(room): guard legacy finish metric integrity`.
- Smoke registration: `b880c64a99bb67a3f9bcaabb18fef504a1639ac9` — `test(room): register legacy finish metric smoke`.

## Validation actually performed

- Re-fetched and read back `RoomFinishGenerator.cs` from current remote `main`; the guard is present.
- Re-fetched and read back the focused smoke from current remote `main`; it covers negative area, infinite wall area, NaN skirting length, disabled-invalid-metric behavior, valid five-output generation, and source-handle propagation.
- The smoke is registered through a module initializer, avoiding the shared `SmokeTestRegistration.cs` hot spot.
- Writes used GitHub content/blob concurrency checks and no force-push.
- No GitHub Actions were dispatched.
- This hosted environment has no local .NET SDK/compiler and no licensed BricsCAD V25 runtime, so no unexecuted build/runtime PASS is claimed. This change is Core-only and does not introduce a new native V25 qualification scenario.

## Coordination

Concurrent active claims during this lane remained on separate Grid annotation, release SemVer, semantic-view, and other non-overlapping surfaces. No other agent work was overwritten.

## Completion condition

Satisfied: current `main` rejects invalid consumed legacy Room Finish metrics at generation time, focused deterministic Core regression coverage is present, and this claim is closed as `COMPLETED`.
