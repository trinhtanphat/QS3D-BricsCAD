# Work claim — Room Finish single-sync global element identity integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-finish-sync-global-element-integrity-20260812-0923`
- Registered: `2026-08-12T09:23:00+07:00`
- Baseline main SHA: `7c553f8cf0b07915ced53ab833aff495aedbdc3a`
- Priority: P1 — prevent direct Room Finish synchronization from mutating a project with globally ambiguous semantic element identity.

## Confirmed defect

`RoomFinishSynchronizationService.SynchronizeExisting(...)` became globally identity-safe through `RoomFinishIdentityService.FindExisting(...)`, while the direct `Synchronize(project, room, finish)` overload validated only the requested Room/Finish identities. Unrelated duplicate semantic element IDs could therefore coexist while a unique Room/Finish pair still mutated Finish semantic state and project revision.

## Implemented fix

- Direct `Synchronize(project, room, finish)` now preflights the complete project element identity set after existing Room/Finish argument/ownership validation and before snapshot/mutation.
- Null entries and case-insensitive duplicate semantic IDs fail closed before Finish/project state changes.
- `SynchronizeExisting`, rollback semantics, stale Auto Room guard, provenance/dependency/metric rules and recent idempotency behavior remain unchanged.
- Focused smoke pins no Floor/Zone/fingerprint/property/dependency/dirty/timestamp/project revision mutation under unrelated duplicates and validates the normal direct synchronization control path.

## Integration evidence

- Claim registration: `e1dae6b25c1df1189f5956c3ee8dccb48a4300d3`.
- Branch source commit: `8f86440b85a3201c069650e0255ba5a5fd18df83`.
- Source-only compare confirmed exactly +13 lines in `RoomFinishSynchronizationService.cs` and no incidental churn.
- Focused smoke commit: `77e9ffb118e28d89ec2ca0afa1b0414fe5e5e2ac`.
- Branch diff was exactly the reserved source plus new 92-line smoke.
- Comparison from claim registration to PR base `1811234b6f75a955b01461cdd7cf52f52d05aa10` showed 19 intervening commits and no reserved-path overlap.
- PR `#692` squash-merged at `ed2448e545ffaf43422afe57bf02ba007cc2da64`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.
