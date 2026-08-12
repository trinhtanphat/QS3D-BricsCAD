# Work claim — Bulk Family structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:24:00+07:00`
- Completed: `2026-08-12T10:28:00+07:00`
- Baseline main SHA: `d322f2f42a89bd12dfe9c7ea75f9b3e1eec63bb8`
- Claim commit: `14d51638db61b9e41d208cf246b6779a85557eb8`
- Source fix: `8edfa62029ba3747b555fde96256c9441166ab19`
- Focused regression source: `f74dbc55d5c141b78d7f20d0a65bac26b901126f`
- Priority: P1 — bulk Family assignment must not commit against a target Family that stopped being canonical while caller-provided target IDs were enumerated.
- Task Key: `CORE-BULK-FAMILY-STRUCTURAL-FRESHNESS`

## Confirmed defect

`BulkEditService.AssignFamily(...)` validated and resolved the target `ProjectFamily`, snapshotted its properties, then materialized caller-provided `elementIds`. Its post-enumeration freshness check only compared `ProjectState.ChangeVersion`. Because `ProjectState.Families` is a public mutable collection, a lazy enumerable could remove the resolved Family or replace it with a different instance using the same semantic ID without calling `project.Touch()`. The bulk path would then continue using the stale Family reference and its pre-enumeration property snapshot.

`OwnedDistinctByIds(...)` resolves target elements after caller ID materialization, so target elements are already re-resolved against the post-enumeration project; the added element ownership loop is a defensive parity check. `ProjectFamilyService.Assign(...)` already uses the same reference-identity freshness contract for both the target Family and resolved elements.

## Completed scope

- `BulkEditService.AssignFamily(...)` preserves the existing `ChangeVersion` freshness guard and now immediately calls `RequireCurrentFamilyAssignmentOwnership(...)` after target-ID enumeration.
- The helper requires `project.FindFamily(family.Id)` to be the exact originally resolved Family instance before category/property planning continues.
- Resolved target elements are also checked by `ReferenceEquals` for parity with the canonical direct Family-assignment path.
- Family/category/property/dirty/project mutation does not begin when target Family ownership changed structurally without a version bump.
- Existing canonical no-op behavior, property inheritance, global Family identity validation, target bounds, category diagnostics and `ProjectSemanticMutationExecutor` behavior are unchanged.
- LOCAL-003 smoke fixtures and semantic-selection/ProjectFamilyService/template/regeneration/browser surfaces were not modified.

## Validation evidence

- Claim registration on `main`: `14d51638db61b9e41d208cf246b6779a85557eb8`.
- Source fix on `main`: `8edfa62029ba3747b555fde96256c9441166ab19`.
- Focused regression source on `main`: `f74dbc55d5c141b78d7f20d0a65bac26b901126f`.
- Post-integration source readback confirms enumeration freshness → canonical Family/element ownership guard → category/planning ordering.
- `BulkFamilyStructuralFreshnessSmoke` covers removing the target Family during lazy ID enumeration, replacing it with a different same-ID instance, and a stable valid assignment control. The failure cases assert no service-owned element/project mutation while preserving the caller's structural side effect.

## Validation boundary

The regression source was committed and read back but was not executed in this connector session. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification is claimed.

## Completion

Completed. Bulk Family assignment now fails closed when the resolved target Family loses canonical project ownership during caller-provided ID enumeration. Reservation released.
