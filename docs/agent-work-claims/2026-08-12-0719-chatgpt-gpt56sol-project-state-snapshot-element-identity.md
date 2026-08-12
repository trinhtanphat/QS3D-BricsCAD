# Work claim — ProjectStateSnapshot element rollback identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-project-state-snapshot-element-identity`
- Registered: `2026-08-12T07:19:00+07:00`
- Last Updated: `2026-08-12T07:31:00+07:00`
- Baseline main SHA: `9e1057b1bc9a8d2786ecc6bdeb7d3e210d4aa4dd`
- Priority: deterministic Core rollback identity defect found during owner-requested evidence-driven continue-all audit
- Task Key: `CORE-PROJECT-STATE-SNAPSHOT-ELEMENT-IDENTITY`

## Confirmed defect

`ProjectStateSnapshot.Restore(...)` restored semantic values through a `CopyInto(...)` branch that cleared `target.Elements` and constructed a new `ProjectElement` for every captured element. A `ProjectElement` reference that was canonical and valid before a transaction therefore became stale after rollback even when the semantic id returned to the project.

The remove/fail/retry case was stricter: an element removed during the failed operation was recreated as a clone, so the exact pre-transaction reference could never become canonical again.

## Implemented scope

`ProjectStateSnapshot.Capture(...)` now keeps two deliberately separate rollback ingredients:

1. a fully detached deep-value snapshot used as the authoritative restore payload;
2. a captured id → original `ProjectElement` reference registry used only when restoring into the exact `ProjectState` instance that was captured.

On rollback of that captured project, snapshot values are copied back into those original element objects and `ProjectState.Elements` is reassembled in captured order. Post-capture elements disappear; removed captured elements are reinserted using their original references. Category, Family/Floor/Zone ids, drawing fingerprint, source handles, dependencies, properties, quantities, dirty flags and element timestamps are restored before the original object is reattached.

`CreateDetachedCopy(...)` still uses the clone-only path and never receives the captured-reference registry. Restoring into a different `ProjectState` object with the same `ProjectId` also remains clone/value based rather than injecting canonical references from the captured project.

## Committed evidence

- Initial source fix: `de46d3d84dd1839dd6f592fa84d57061fd54930b` — `fix(core): preserve element identity on snapshot rollback`
- Same-ProjectId foreign-target isolation hardening: `1994fcf9ea0ae7fbdf679e442c8d9775bd12d413` — `fix(core): scope rollback identity to captured project`
- Focused smoke: `fac26bd879dd6a35334d6a45052274edc5582e0b` — `test(core): guard snapshot element rollback identity`
- Smoke registration: `95c7c1a9f26c17744198cac83f8efb8466e71d0f` — `test(core): register snapshot element identity smoke`
- Read-back on moving-main snapshot `c28a5090381fa9473ca2f59f11dc51647fe03396` confirmed the hardened source, focused smoke and registration were all still present after concurrent commits.

The focused smoke proves mutate + remove + add rollback semantics, `ReferenceEquals` continuity for both retained and removed-then-restored captured elements, value/dirty/timestamp/project-state restoration, post-capture element removal, captured ordering, and detached-copy non-aliasing.

A supplemental smoke assertion for restoring into a different same-`ProjectId` `ProjectState` was drafted after source review, but two optimistic-lock writes returned HTTP 409 while `main` was moving rapidly. It was not force-written and is not claimed as committed. The source-side isolation itself is committed in `1994fcf...` and was read back on later `main`.

## Preserved behavior / exclusions

- Zone/Floor/Family/QuantityRule/AuditEvent object identity was intentionally not broadened into this batch; their existing value-restore semantics remain unchanged.
- BricsCAD adapter/UI, project session/store persistence, command-specific rollback flows and native DWG transactions were not modified.
- Project identity, project `UpdatedUtc`/`ChangeVersion`, element dirty/timestamp restoration and captured element order remain snapshot-authoritative.
- No unrelated `ACTIVE` claim was overwritten, no force-push was used, and no GitHub Actions/build/release was dispatched.
- No local Core smoke execution or BricsCAD V25 runtime qualification is claimed by this remote batch.

## Completion condition

Satisfied: rollback of the captured canonical project restores both semantic values and canonical `ProjectElement` identity for every element that existed at capture time, including remove-then-restore cases, while detached copies remain non-aliasing.