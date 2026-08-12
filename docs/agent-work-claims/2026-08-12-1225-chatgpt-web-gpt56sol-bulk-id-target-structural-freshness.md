# Work claim — Bulk Edit ID-target structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bulk-id-target-structural-freshness-20260812-1225`
- Registered: `2026-08-12T12:25:00+07:00`
- Completed: `2026-08-12T12:31:00+07:00`
- Baseline main SHA: `5c462b3a79eb58db3c9caba6f9950058ce3d9532`
- Claim commit: `3258a8867b9e5c3185a7df49ee289f0c9a05ac78`
- Source fix commit: `5543cd73654ae9b90a8b88043e80fffdd4345345`
- Focused smoke commit: `07631e6fdc3e944ff490ad5a3a476e2fd55c31e0`
- Integration PR: `#882`
- Main integration SHA: `54170bf981a1bae7d08abf93b001aca95777a08d`
- Priority: P1 — ID-based bulk edits must not silently resolve replacement same-ID elements introduced during lazy target enumeration.
- Task Key: `CORE-BULK-ID-TARGET-STRUCTURAL-FRESHNESS`

## Confirmed defect

Completed Bulk Edit structural-freshness lanes protected object-target enumeration and verified target/Family ownership after `OwnedDistinctByIds(...)` returned. However `OwnedDistinctByIds(...)` materialized caller IDs first and only then resolved them from live `project.Elements`. A lazy ID source could replace selected `B1` with a same-ID instance without calling `Touch()`, after which the replacement was treated as current and could be mutated under the unchanged revision.

## Implemented contract

- `OwnedDistinctByIds(...)` snapshots project element references before caller ID enumeration.
- Caller ID materialization, bounds, blank/duplicate/missing-ID validation remain unchanged.
- Target IDs are resolved against the pre-enumeration snapshot instead of live project state.
- Existing outer `ChangeVersion` freshness checks retain precedence for ordinary semantic mutations.
- Existing object-target ownership and Family-assignment ownership guards reject same-ID replacement/removal before mutation.
- Object-target APIs, property policy, Family validation/default propagation, atomic rollback and successful mutation semantics remain unchanged.

## Regression evidence

`BulkEditIdTargetStructuralFreshnessSmoke` covers both ID-based paths. Lazy IDs replace `B1` with a same-ID element without `Touch()`; `SetProperty(...)` and `AssignFamily(...)` must reject without changing project revision or either original/replacement target state. A stable ID-based property edit remains successful and advances the project revision.

## Integration / concurrency evidence

The branch diff from claim commit contained exactly `BulkEditService.cs` plus the focused smoke. The first squash attempt was rejected because `main` changed concurrently. Current-main readback still showed the exact pre-fix source blob `c1d8b88adb384e7226550f0c434a872b4985b75e`, and compare from refreshed `main@3d3fc35b8b864ab47e7272a70be6ba1621a71bee` to PR head showed only the two reserved files. After another unrelated main advance to `ed05830886404e3f3c78b2ed8699486bd2c18cd4`, source readback remained pre-fix. PR #882 was then squash-merged with expected head `07631e6fdc3e944ff490ad5a3a476e2fd55c31e0` as `54170bf981a1bae7d08abf93b001aca95777a08d`.

## Validation boundary

No GitHub Actions were dispatched. No force-push was used. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only lane.
