# Work claim — Auto Room Floor/Zone canonical scope matching

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-auto-room-floor-zone-canonical-scope`
- Registered: `2026-08-11T22:51:00+07:00`
- Expanded: `2026-08-11T22:52:00+07:00`
- Completed: `2026-08-11T22:57:00+07:00`
- Baseline main SHA observed: `0d6874710daf68b1e8b7d981066e7a4cb56afd97`
- Priority: P1 — Auto Room source-signature reuse, stale-room selection and Room-finish quantity inclusion previously compared mutable `ProjectElement.FloorId` / `ZoneId` raw. Padded/case-varied runtime relation state could hide an existing Auto Room from reuse/stale marking or falsely exclude a correctly scoped Room finish.

## Implemented

- `78bb8e795606f0d84db5268c42c5904e74e628ed` — registered the initial source-signature/stale-scope lane before implementation.
- `324edfc58cd8611a5d0d9b114b1910bf70c369b5` — expanded the reservation before adding the adjacent Room-finish quantity scope correction.
- `970ada35d59739cd09cfe39a2b89c81ef6d3b0e5` — introduced one `SameScopeId()` helper using trimmed case-insensitive identity and routed `FindBySourceSignature()`, `MarkStaleForSelection()` and Room-vs-finish Floor/Zone checks in `IsExcludedFromQuantity()` through it.
- `7a7b9f40a6876b9268299b18eb6e251f15e01059` — added deterministic Core smoke coverage for canonical source-signature reuse, canonical stale selection with unrelated-scope exclusion, and Room-finish quantity inclusion/exclusion by canonical scope identity.
- `c41ceec37e66453438926ae14f201627afb1bec8` — module-registers the focused smoke.
- `b2aa664b7ef44673a69d84ae26dca4000a3f8183` — added `scripts/preflight-auto-room-canonical-scope.py`, requiring all three methods to use `SameScopeId()` and rejecting the previous raw comparisons.

## Preserved contracts

- No RoomBoundaryEngine/topology, native `QS3DROOMAUTO`, Room Finish generation, Family defaults, Workspace/UI, persistence migration, CAD/Ribbon/updater/release behavior changed.
- No global rewrite of `ProjectElement` Floor/Zone setters and no new quantity algorithm was introduced; only existing scope-equivalence checks were corrected.
- Source-signature normalization, stale policy, room provenance and dependency handling remain unchanged.

## Validation

- Re-fetched current `AutoRoomLifecycle.cs` and confirmed `FindBySourceSignature()` and `MarkStaleForSelection()` use `SameScopeId()` for both Floor and Zone scope, while `IsExcludedFromQuantity()` uses the same helper for Room-vs-finish scope checks.
- Re-fetched the focused smoke and confirmed all three scenarios are present.
- Compared `b2aa664b7ef44673a69d84ae26dca4000a3f8183` to current `main`; it is an ancestor and the later eight concurrent commits did not touch this lane's source/test/preflight files.
- No GitHub Actions workflow was dispatched and no BricsCAD V25 runtime PASS is claimed; this is deterministic Core behavior.

## LOCAL_ONLY disposition

- None added.

## Completion evidence

Auto Room reuse, stale lifecycle and Room-finish quantity scope checks now agree on canonical Floor/Zone identity with the hardened project services. Final implementation/preflight tip for this lane: `b2aa664b7ef44673a69d84ae26dca4000a3f8183`.
