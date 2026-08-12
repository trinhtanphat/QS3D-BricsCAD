# Work claim — Door/opening schedule HostWallId canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-door-opening-host-id-canonicality-20260812-0934`
- Registered: `2026-08-12T09:34:00+07:00`
- Completed: `2026-08-12T09:39:00+07:00`
- Baseline main SHA observed: `c26a1423f1c6ccf224f00e799e26deae3a6321b7`
- Claim commit: `d00993c683c183ff1ec0e35128f79c2eadaa59c8`
- Pull Request: `#703`
- Reviewed head: `f16317afc511c09432542998eb79e07cdd29b5c4`
- Merge SHA: `c24c84dfb9dd3f5c86f2a15908eb426bffe6ef8a`
- Priority: P1 reporting relation integrity
- Task Key: `CORE-REPORTING-DOOR-OPENING-HOST-ID-CANONICALITY`

## Confirmed defect

`HostLinkService.LinkOpening(...)` writes exact canonical wall identity to the persisted `HostWallId` property. `DoorOpeningScheduleBuilder.Build(...)` previously read `HostWallId` with `(hostRaw ?? string.Empty).Trim()` and silently used that normalized value in `HostIds` / `HostCount`, so malformed persisted relations such as `" W1 "` appeared canonical in reporting.

## Completed implementation

- `DoorOpeningScheduleBuilder` now preserves missing/null/empty `HostWallId` as the existing unhosted behavior.
- Whitespace-only and leading/trailing-whitespace `HostWallId` values fail closed instead of being silently repaired.
- Canonical host text is preserved exactly for existing `HostIds` / `HostCount` aggregation.
- No host-id casing normalization was introduced.
- Model Health, `HostLinkService`, physical opening state, persistence, CAD/UI and other reporting builders were not modified.

## Regression evidence

`tests/QS3D.Core.SmokeTests/DoorOpeningScheduleHostIdCanonicalitySmoke.cs` covers canonical host reporting, missing/empty unhosted controls, padded-host rejection and whitespace-only rejection.

Moving-main comparison from the claim commit through the pre-merge head showed no overlap with `DoorOpeningSchedule.cs` or the new smoke. The source was re-read on moving `main` immediately before the head-locked squash merge.

## Validation boundary

No GitHub Actions/build/release dispatch occurred. No local/full build or licensed BricsCAD V25/V26 runtime PASS is claimed.
