# Work claim — Door/opening schedule HostWallId canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-door-opening-host-id-canonicality-20260812-0934`
- Registered: `2026-08-12T09:34:00+07:00`
- Baseline main SHA observed: `c26a1423f1c6ccf224f00e799e26deae3a6321b7`
- Priority: P1 reporting relation integrity
- Task Key: `CORE-REPORTING-DOOR-OPENING-HOST-ID-CANONICALITY`

## Confirmed defect

`HostLinkService.LinkOpening(...)` writes exact canonical wall identity to the persisted `HostWallId` property, and the active Model Health lane independently treats aliases as malformed. `DoorOpeningScheduleBuilder.Build(...)`, however, currently reads `HostWallId` with `(hostRaw ?? string.Empty).Trim()` and silently uses that normalized value in `HostIds` / `HostCount`. A padded persisted host relation such as `" W1 "` can therefore look valid in the schedule and hide malformed project state.

## Reserved scope

- `src/QS3D.Core/Reporting/DoorOpeningSchedule.cs`
- one focused Core smoke regression for Door/opening schedule HostWallId canonicality
- this claim file

## Non-overlap / exclusions

The active `CORE-MODEL-HEALTH-HOSTWALL-CANONICALITY` claim reserves `ModelHealthService` only. This lane does not modify Model Health, `HostLinkService`, physical opening cut state, persistence, native CAD/UI, other reporting builders, or the shared Family/Floor/Zone relation guard.

## Intended contract

- missing/blank HostWallId keeps the existing unhosted schedule behavior;
- nonblank HostWallId with surrounding whitespace fails closed instead of being silently trimmed;
- exact canonical HostWallId preserves HostIds/HostCount behavior;
- host ID casing semantics are not broadened by this lane beyond the writer-owned exact text contract needed to prevent whitespace repair;
- no schedule mutation occurs before validation failure.

## Validation plan

Add deterministic Core smoke coverage for padded HostWallId rejection plus canonical and blank controls. Re-fetch moving `main`, compare exact file overlap, squash-merge with expected head SHA, then close this claim with exact integration evidence.

No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this lane.
