# Work claim — Door/opening schedule host target integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-door-opening-host-target-integrity-20260812-1045`
- Registered: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `9826397496fa32097f0463f3b26142d2eba01976`
- Priority: P2 evidence-driven reporting integrity

## Confirmed defect

`DoorOpeningScheduleBuilder.Build(...)` currently validates `HostWallId` only for canonical surrounding whitespace. A non-empty canonical token is added to `HostIds`/`HostCount` without proving that it resolves to an existing semantic element or that the target is a wall category.

This is inconsistent with the reporting identity guard, which fails closed on missing Family/Floor/Zone references, and with Model Health, which reports missing/non-wall opening hosts as errors. A stale or non-wall `HostWallId` can therefore be emitted as an apparently valid host in the Door/Opening schedule.

## Reserved scope

- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleHostIdCanonicalitySmoke.cs`
- this claim file

## Expected fix

Preserve missing/empty `HostWallId` as the supported unhosted case. For a non-empty canonical `HostWallId`, require an existing unique semantic target and require that target category is one of the wall host categories before it contributes to schedule `HostIds`/`HostCount`.

## Excluded scope

- No HostLink mutation/repair logic.
- No change to opening/door authoring or physical-cut behavior.
- No XLSX exporter changes.
- No BricsCAD/native/runtime or GitHub Actions work.

## Validation plan

- Canonical existing wall host remains reported once.
- Missing and empty host property remain unhosted.
- Padded/whitespace-only host tokens remain rejected.
- Canonical orphan host id fails closed.
- Canonical host id targeting a non-wall element fails closed.

## Completion condition

Source and focused smoke regression are committed to `main`, exact integration SHAs are recorded, and this claim is marked `COMPLETED` without claiming local BricsCAD/runtime PASS.
