# Work claim — legacy Room Finish generator numeric safety

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-room-finish-generator-numeric-safety`
- Registered: `2026-08-11T23:24:00+07:00`
- Baseline main SHA: `11486e07d818ec1df718f3775a4b3c23e15123da`
- Priority: evidence-driven remote-safe Core data-integrity hardening found during owner-requested continue-all audit

## Confirmed defect

Legacy `ElementInstance` metric properties are mutable `double` values and can contain negative, `NaN`, or infinity values. `RoomFinishGenerator.Generate(...)` currently copies `AreaM2`, `InnerPerimeterM`, and `SideAreaM2` directly into newly generated finish elements without validating them. This permits the generator itself to create invalid quantity-bearing elements and relies on a later reporting boundary to fail.

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

## Validation plan

- Reject negative Room area when an area-based finish is enabled.
- Reject non-finite Room side area when Wall Finish is enabled.
- Reject non-finite Room inner perimeter when Skirting is enabled.
- Prove invalid metrics that are not consumed because their corresponding finishes are disabled do not block unrelated valid generation.
- Preserve existing valid five-finish generation behavior and source-handle propagation.
- Register focused smoke through a module initializer to avoid the shared `SmokeTestRegistration.cs` hot spot.
- Re-fetch target blobs before writes and read back current `main` after integration; never force-push.

## Coordination

Current active claims observed before reservation cover Grid annotation audit-owned touch and strict release SemVer; recently active Semantic Tag, Material rename, and target-map work are separate surfaces. No current or recent claim was found for legacy `RoomFinishGenerator` numeric safety.

## Completion condition

Current `main` rejects invalid consumed legacy Room Finish metrics at generation time, includes focused deterministic Core regression coverage, and this claim is closed as `COMPLETED` with exact pushed commits and actual validation scope.
