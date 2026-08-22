# Agent Work Claim — Structural Wall opening HostWallId canonicality

- Agent: `chatgpt-gpt56sol-structural-wall-opening-host-canonicality`
- Owner: OpenAI ChatGPT
- Status: `COMPLETED`
- Registered: 2026-08-12 09:46 +07:00
- Completed source-side: 2026-08-12 09:50 +07:00
- Merged to `main`: 2026-08-12 09:53 +07:00
- Baseline main SHA observed: `3f0915076869f92244a0b5b384bf157d2ef097ee`
- Claim commit: `e11d74a456b2c1f38a4ee91c89bc6b9b3a0cf5d5`
- Implementation commit: `d21349b2ddefc98b86603566e0c9c011af6a1f6a`
- Regression-source commit: `54bdb7c65206575bbf87b843b59ed3a7570ca23e`
- Squash merge commit: `b27fbd046741a6d2443270eef5fac4de0eb45e46`
- Pull request: `#721`
- Task key: `CORE-STRUCTURAL-WALL-OPENING-HOST-CANONICALITY`

## Confirmed defect

`StructuralRegenerator.LinkedOpeningArea(...)` read Door/WallOpening `HostWallId` and compared the persisted raw value directly with the structural wall id. A padded or whitespace-only persisted HostWallId was therefore silently treated as not linked, so the wall could regenerate with an understated opening deduction instead of surfacing malformed semantic relation state.

This diverged from the canonical relation contract already enforced elsewhere: `HostLinkService` writes `HostWallId = wall.Id` exactly, and `DoorOpeningScheduleBuilder` rejects whitespace-only or padded persisted HostWallId values instead of normalizing them.

## Implemented

- Missing HostWallId and exact empty-string HostWallId remain unhosted.
- Non-empty HostWallId must be nonblank and free of surrounding whitespace before structural-wall opening matching.
- Canonical HostWallId matching remains case-insensitive.
- Validation occurs inside `LinkedOpeningArea(...)` before structural-wall `SetQuantity(...)` calls, so malformed host metadata cannot leave partial regenerated wall quantities.

## Changed surfaces

- `src/QS3D.Core/Services/StructuralRegenerator.cs`
- `tests/QS3D.Core.SmokeTests/StructuralWallOpeningHostIdCanonicalitySmoke.cs`
- this claim file

## Regression source

`StructuralWallOpeningHostIdCanonicalitySmoke` covers canonical linked opening deduction, padded HostWallId rejection, whitespace-only HostWallId rejection, missing/empty unhosted behavior, and verifies malformed host metadata is rejected before structural-wall quantity mutation.

## Excluded scope

- `MeasuredSolidQuantityPolicy.cs` and the active measured-solid stale-volume lane
- HostLink mutation semantics
- reporting/schedule source
- opening native cuts/rebar/mesh/UI/BricsCAD adapters
- GitHub Actions/build/release/runtime qualification

## Validation performed

- Re-read current `StructuralRegenerator`, `HostLinkService`, and the completed Door/Opening schedule HostWallId canonicality change before editing.
- Collision-checked recent structural-wall and HostWallId commits plus the active measured-solid claim; no reserved-source overlap was found before claim registration.
- Source and smoke writes occurred only on the claim branch after the claim landed on `main`.
- Reviewed the exact three-file PR patch, re-read `StructuralRegenerator.cs` on moving `main`, and confirmed no concurrent source overlap before merge.
- PR `#721` was squash-merged with expected head SHA `bab0944058e0fe3353357e2970f6a2f0adf49b43` into merge commit `b27fbd046741a6d2443270eef5fac4de0eb45e46`.
- No GitHub Actions/build/release was dispatched. The smoke source was not executed, so no executable smoke PASS is claimed. No BricsCAD V25/V26 runtime PASS is claimed remotely.
