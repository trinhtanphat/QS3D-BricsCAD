# Work claim — Semantic handle selection structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:38:00+07:00`
- Completed: `2026-08-12T10:41:00+07:00`
- Baseline main SHA: `cefe3a8c834d62dcc8dfaf3a77bfc33d5e285ab7`
- Claim commit: `756f78de1b5f24606e57f2288e6733525f437eee`
- Source fix: `3fe224c036fb01cffe7a0d928c2e0acaacd9fa9d`
- netstandard2.0 compatibility correction: `11f0d08945a8733795d867cd2ab981585840c357`
- Focused regression source: `f3f87a7f9b49254c1a94095a3ca0cb97db7d1187`
- Priority: P1 — semantic handle ownership resolution must not continue across caller-driven project element membership changes that bypass `ChangeVersion`.
- Task Key: `CORE-SEMANTIC-HANDLE-STRUCTURAL-FRESHNESS`

## Confirmed defect

`SemanticHandleOwnershipResolver.Resolve(ProjectState, IEnumerable<string>)` validated unique project element IDs before materializing caller-provided selected handles, then pinned only `ProjectState.ChangeVersion` across that lazy enumeration. `ProjectState.Elements` is a public mutable list. A side-effecting handle enumerable could remove, add, or replace semantic element instances directly without calling `project.Touch()`, leaving `ChangeVersion` unchanged. Ownership scanning then ran against a different element membership/identity set than the one validated before input enumeration.

This was distinct from the completed semantic-handle input-freshness lane, whose regression explicitly covered lazy inputs that call `project.Touch()`. The resolver feeds mutation workflows such as semantic untrack, so stale/ambiguous ownership resolution can influence which semantic elements are targeted.

## Completed scope

- `Resolve(...)` now snapshots the pre-enumeration canonical element ID → exact instance ownership map.
- Existing `ChangeVersion` validation remains immediately after selected-handle materialization and therefore retains diagnostic precedence for ordinary semantic mutation.
- Before empty-selection no-op or ownership scanning, `RequireElementOwnershipUnchanged(...)` requires current element count, IDs and exact instances to match the pre-enumeration snapshot.
- Element removal, addition, same-ID replacement, null entry or duplicate identity introduced without `Touch()` now fails closed before ownership resolution continues.
- `EnsureUniqueElementIds(...)` reuses the same snapshot validator, preserving existing malformed-project diagnostics for other resolver APIs.
- Selected-handle count/normalization/deduplication, source/generated ownership diagnostics, deterministic ordering and stable behavior are unchanged.
- `SourceHandleResolver`, `SemanticUntrackService`, generated-handle policies/health and CAD/UI/native adapters were not modified.

## Compatibility correction

The initial source fix used `Dictionary.TryAdd(...)`. `QS3D.Core` targets `netstandard2.0`, and repo conventions do not rely on that API. Before closing the lane, the snapshot helper was corrected to the existing `ContainsKey(...)` + `Add(...)` pattern in commit `11f0d08945a8733795d867cd2ab981585840c357`. Current source readback contains no `TryAdd` use in this helper.

## Validation evidence

- Claim registration on `main`: `756f78de1b5f24606e57f2288e6733525f437eee`.
- Source fix on `main`: `3fe224c036fb01cffe7a0d928c2e0acaacd9fa9d`.
- Compatibility correction on `main`: `11f0d08945a8733795d867cd2ab981585840c357`.
- Focused regression source on `main`: `f3f87a7f9b49254c1a94095a3ca0cb97db7d1187`.
- Post-integration source readback confirms ownership snapshot → lazy selection materialization → existing version guard → structural ownership guard → empty-selection/ownership scan ordering.
- `SemanticHandleSelectionStructuralFreshnessSmoke` covers removal before an empty-selection return, same-ID replacement during lazy selection and a stable lazy-selection control, while asserting caller-side structural changes do not fabricate a project version bump.

## Validation boundary

The regression source was committed and read back but was not executed in this connector session. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification is claimed.

## Completion

Completed. Semantic handle ownership resolution now rejects both semantic-version changes and silent structural element-set changes across caller-provided handle enumeration. Reservation released.
