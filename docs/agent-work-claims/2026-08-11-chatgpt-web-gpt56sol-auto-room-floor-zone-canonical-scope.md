# Work claim — Auto Room Floor/Zone canonical scope matching

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auto-room-floor-zone-canonical-scope`
- Registered: `2026-08-11T22:51:00+07:00`
- Baseline main SHA observed: `0d6874710daf68b1e8b7d981066e7a4cb56afd97`
- Priority: P1 — Auto Room source-signature reuse and stale-room selection currently compare mutable `ProjectElement.FloorId` / `ZoneId` raw. Padded/case-varied runtime relation state can therefore hide an existing Auto Room from reuse or stale marking even though Floor/Zone service identity is canonicalized elsewhere.

## Reserved scope

- Canonicalize Floor/Zone scope identity in `AutoRoomLifecycle.FindBySourceSignature()` and `MarkStaleForSelection()` without changing boundary signatures, topology, stale policy, Family synchronization or Room finish behavior.
- Add deterministic Core smoke coverage and a focused preflight for these two matching paths.

## Expected surfaces

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- `tests/QS3D.Core.SmokeTests/AutoRoomCanonicalScopeSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/AutoRoomCanonicalScopeSmokeRegistration.cs` (new)
- `scripts/preflight-auto-room-canonical-scope.py` (new)
- this claim file for close-out

## Excluded scope

- No RoomBoundaryEngine/topology, native `QS3DROOMAUTO`, Room Finish generation, Family defaults, Workspace/UI, persistence migration, CAD/Ribbon/updater/release/GitHub Actions.
- No global rewrite of `ProjectElement` Floor/Zone setters.

## Validation plan

- Find-by-signature smoke: padded/case-varied Room Floor/Zone ids still match canonical/padded requested scope and return the existing exact Room instead of creating an identity gap.
- Stale-selection smoke: padded/case-varied Room scope participates in stale detection, mutates the intended Room only, and unrelated Floor/Zone Room remains untouched.
- Focused preflight rejects raw FloorId/ZoneId comparisons in these two methods.
- Re-fetch current `main` before source write and preserve concurrent winners.

## Completion condition

- Auto Room reuse and stale lifecycle share canonical Floor/Zone identity with the newly hardened project services, with deterministic regression coverage and a completed pushed claim.
