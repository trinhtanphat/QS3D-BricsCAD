# Agent Work Claim — Structural Wall opening HostWallId canonicality

- Agent: `chatgpt-gpt56sol-structural-wall-opening-host-canonicality`
- Owner: OpenAI ChatGPT
- Status: `ACTIVE`
- Registered: 2026-08-12 09:46 +07:00
- Baseline main SHA observed: `3f0915076869f92244a0b5b384bf157d2ef097ee`
- Task key: `CORE-STRUCTURAL-WALL-OPENING-HOST-CANONICALITY`

## Confirmed defect

`StructuralRegenerator.LinkedOpeningArea(...)` reads Door/WallOpening `HostWallId` and compares the persisted raw value directly with the structural wall id. A padded or whitespace-only persisted HostWallId is therefore silently treated as not linked, so the wall can regenerate with an understated opening deduction instead of surfacing malformed semantic relation state.

This diverges from the canonical relation contract already enforced elsewhere: `HostLinkService` writes `HostWallId = wall.Id` exactly, and `DoorOpeningScheduleBuilder` now rejects whitespace-only or padded persisted HostWallId values instead of normalizing them.

## Reserved scope

- `src/QS3D.Core/Services/StructuralRegenerator.cs`
- one focused Core smoke source for structural-wall opening HostWallId canonicality
- this claim file

## Excluded scope

- `MeasuredSolidQuantityPolicy.cs` and the active measured-solid stale-volume lane
- HostLink mutation semantics
- reporting/schedule source
- opening native cuts/rebar/mesh/UI/BricsCAD adapters
- GitHub Actions/build/release/runtime qualification

## Plan

1. Preserve missing/exact-empty HostWallId as unhosted.
2. Fail closed before quantity mutation when a Door/WallOpening carries whitespace-only or surrounding-whitespace HostWallId.
3. Preserve canonical host matching and unrelated canonical host behavior.
4. Add focused smoke source proving canonical linked opening deduction remains intact while malformed HostWallId cannot silently under-deduct a structural wall.
5. Re-read moving `main`, review exact diff, merge only if the reserved source remains untouched, then read back source/test and close claim with immutable evidence.

No GitHub Actions/build/release is authorized. Smoke source may be added but will not be claimed as executed. No BricsCAD V25/V26 runtime PASS will be claimed remotely.
