# Work claim — ProjectElement generated stale idempotency

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-generated-stale-idempotency`
- Registered: `2026-08-12T09:42:00+07:00`
- Completed: `2026-08-12T09:47:00+07:00`
- Last Updated: `2026-08-12T09:47:00+07:00`
- Baseline main SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Claim commit: `95ba837b6703395d34bbfb624c8699788b6ed037`
- Branch source commit: `eb816ef229e2ed7d8ea3847c3814a41aaab2dce9`
- Branch regression commit: `44249d9123f7a9662d2d7d44a48a8c632087f584`
- Pull request: `#712`
- Main merge commit: `f56c95d2cf89823b6e24c1f3ef72dd581d781537`
- Priority: deterministic generated-output freshness defect found during owner-requested continue-all audit
- Task Key: `CORE-PROJECT-ELEMENT-GENERATED-STALE-IDEMPOTENCY`

## Confirmed defect

`ProjectElement.MarkGeneratedGeometryStale(...)`, `MarkGeneratedCurtainFrameStale(...)`, and `MarkGeneratedCurtainPanelStale(...)` rewrote stale state/snapshot and `UpdatedUtc` whenever generated output existed, even when the same output signature was already stale for the same reason. A simple `already stale => return` would also have been wrong because changed handles/signatures must refresh the stale snapshot.

## Implemented contract

- same generated signature + same normalized stale reason is now a true no-op and preserves `UpdatedUtc`;
- generated signature/handles changing while stale refreshes the stale snapshot and `UpdatedUtc`;
- stale reason changing while output snapshot is unchanged updates reason and `UpdatedUtc`;
- no generated output still creates no aggregate stale state;
- specialized Curtain Frame/Panel marking follows the same output-exists vs changed-state contract;
- existing stale-query purity, explicit clear behavior, health semantics, output signature normalization and dirty-flag behavior remain unchanged.

## Regression coverage

The existing registered `GeneratedGeometryStaleSmoke` now additionally proves:

- repeated same-signature/same-reason marking leaves timestamp and snapshot unchanged;
- changed generated handle refreshes the stale snapshot and remains stale against the new output;
- changed reason advances freshness without changing the snapshot;
- specialized Curtain Panel marking is timestamp-idempotent and refreshes after handle change;
- existing no-generated-output behavior remains covered.

Timestamp checks explicitly wait for the wall clock to advance before the second call, so a hidden timestamp write cannot pass because of equal clock resolution.

## Merge/readback evidence

- PR `#712` changed exactly `ProjectElement.cs` and `GeneratedGeometryStaleSmoke.cs`.
- Squash merge succeeded at `f56c95d2cf89823b6e24c1f3ef72dd581d781537` with expected head `44249d9123f7a9662d2d7d44a48a8c632087f584`.
- Direct readback from `main` confirmed source blob `8c9469f6bed2e4aca968d77a236230ab9ec9d32b` and smoke blob `25a510ba5b0fb349b577a58f4f1dfc2d88a24b79`.
- Comparison from merge SHA to later `main` reported `behind_by=0`; concurrent commits touched unrelated surfaces.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS was claimed or performed in this hosted lane.

## Outcome

Generated stale marking now mutates freshness only when stale state, stale snapshot or stale reason changes, while changed generated output remains visible and refreshable. Claim released as completed.
