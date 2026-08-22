# Work claim — Door/opening schedule host target integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-door-opening-host-target-integrity-20260812-1045`
- Registered: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `9826397496fa32097f0463f3b26142d2eba01976`
- Priority: P2 evidence-driven reporting integrity

## Confirmed defect

`DoorOpeningScheduleBuilder.Build(...)` validated `HostWallId` only for canonical surrounding whitespace. A non-empty canonical token could be added to `HostIds`/`HostCount` without proving that it resolved to an existing semantic element or that the target was a wall category.

This was inconsistent with the reporting identity guard, which fails closed on missing Family/Floor/Zone references, and with Model Health, which reports missing/non-wall opening hosts as errors. A stale or non-wall `HostWallId` could therefore be emitted as an apparently valid host in the Door/Opening schedule.

## Reserved scope

- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- `tests/QS3D.Core.SmokeTests/DoorOpeningScheduleHostIdCanonicalitySmoke.cs`
- this claim file

## Implemented fix

Missing/empty `HostWallId` remains the supported unhosted case. For a non-empty canonical `HostWallId`, the schedule now requires an existing unique semantic target and requires the target category to be an ArchitecturalWall, GlassWall, WallPier, or StructuralWall before it contributes to `HostIds`/`HostCount`.

No project repair/mutation is attempted by reporting.

## Integration evidence

- Claim registration: `97c74699d7f47d7bac8aa2c51c40ae07023f4c8a`
- Source fix: `17de2ee58e5916752d4e389f527dccf9227912b7`
- Focused regression: `704776fcfbc747beb68a406a25a1d7992738e6b0`
- Source readback blob: `1365ee54e42c768790133a4e8b5dde0eb53814ca`
- Smoke readback blob: `a45652b68f72ce23bd942dc4e2d98e9ba527fef7`

## Validation coverage

- Canonical existing wall host remains reported once.
- Missing and empty host property remain unhosted.
- Padded/whitespace-only host tokens remain rejected.
- Canonical orphan host id fails closed.
- Canonical host id targeting a non-wall element fails closed.

No GitHub Actions, executable smoke suite, .NET build, or BricsCAD V25 runtime was dispatched or claimed PASS in this remote lane.
