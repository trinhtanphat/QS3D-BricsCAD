# Work claim — Snapshot duplicate identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-snapshot-duplicate-identity-integrity`
- Registered: `2026-08-12T09:33:00+07:00`
- Baseline main SHA: `4ea70225d91dbc07edfa256c8e29884156f2f932`
- Priority: P1 snapshot fail-closed integrity found during owner-requested `continue all` audit.

## Confirmed defect

`ProjectState.FindZone/FindFloor/FindFamily/FindElement/FindQuantityRule` define duplicate semantic IDs as invalid project state, and QSDB persistence rejects the same duplicates. `ProjectStateSnapshot.Capture(...)` now happens to reject duplicate Zone/Floor/Family/Element IDs while building captured-reference dictionaries, but public `ProjectStateSnapshot.CreateDetachedCopy(...)` calls `Clone(...) -> CopyInto(...)`, whose `ValidateCollectionEntries(...)` checks only null entries. A malformed in-memory project can therefore be cloned with duplicate semantic identities and fail later only when a consumer performs lookup or another integrity-sensitive operation. QuantityRule duplicate IDs are also not rejected by snapshot validation at all.

The snapshot boundary should fail closed before producing another malformed ProjectState. This is separate from the completed QuantityRule engine duplicate-ID lane, which only guards rule evaluation/provenance.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current snapshot source and this claim before writes.
2. Extend snapshot collection validation to reject duplicate Zone/Floor/Family/Element/QuantityRule IDs case-insensitively before any target mutation/copy.
3. Preserve existing null-entry errors and same-project identity restoration; do not change domain service create/delete behavior.
4. Add smoke coverage proving `CreateDetachedCopy(...)` rejects duplicate IDs for all five semantic collections while a valid project still clones without aliasing.
5. Read back source/test on current `main`; do not dispatch GitHub Actions or claim BricsCAD runtime PASS.
6. Close claim only after source/regression remain visible on current `main`.

## Excluded

- No rule engine/provenance changes.
- No QSDB schema/token, ProjectSession, adapter/UI, installer or release changes.
