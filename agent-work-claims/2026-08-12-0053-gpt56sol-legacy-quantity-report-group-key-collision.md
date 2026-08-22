# Work claim — Legacy quantity report collision-free grouping identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-legacy-quantity-report-key-20260812-0053`
- Registered: `2026-08-12T00:53:00+07:00`
- Completed: `2026-08-12T00:58:59+07:00`
- Baseline main SHA: `a37bef51f8757dee3d25bf06c30e5eec04d65c9c`
- Claim commit: `55080269b768935b95f14756be893100c21dbe83`
- Source fix commit: `c572d0a4191ab612b74c20d98c27ea4e0542b07c`
- Regression commit: `34fb57c7f18fa98824f8926d53b93179b4972f77`
- Priority: P2 evidence-driven remote-safe reporting integrity

## Confirmed defect

`QuantityReportBuilder.Group(...)` constructed its grouping identity by joining Floor, Category, Family name and normalized Material with an unescaped U+001F delimiter. `ElementInstance.Floor`, `FamilyDefinition.Name`, and `FamilyDefinition.Material` accept trimmed nonblank text without forbidding U+001F. Distinct accepted tuples could therefore serialize to the same dictionary key and merge counts, quantities and provenance incorrectly.

A concrete collision was one Beam row using Floor `F`, Family `Column`, Material `N<US>M`, and one Column row using Floor `F<US>Beam`, Family `N`, Material `M`; both serialized to the same delimiter-only key before this fix.

## Implemented

- `QuantityReportBuilder.Group(...)` now constructs its composite identity with deterministic length-prefixed tokens for Floor, Category, Family and normalized Material.
- The existing case-insensitive dictionary comparer, first-seen row order, accepted text characters, quantity aggregation and source-handle provenance behavior remain unchanged.
- Added `QuantityReportGroupKeyCollisionSmoke` with a module initializer. It proves the concrete accepted U+001F collision yields two rows while identical tuples still group and retain independent Count, LengthM, ElementIds and SourceHandles.

## Reserved / implemented surfaces

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/QuantityReportGroupKeyCollisionSmoke.cs`
- this claim file

## Excluded scope honored

- No `ProjectQuantityReportBuilder` changes.
- No `MaterialUsageScheduleBuilder` changes.
- No Family/Floor/Material validation restrictions.
- No quantity formulas/business rules, persistence, XLSX, adapter/native or UI changes.
- No GitHub Actions dispatch.

## Validation actually performed

- The standalone claim commit was verified as an ancestor of current `main` before implementation.
- Repeated current-main comparisons during heavy concurrent churn showed no changes to the reserved Reporting source/test surfaces.
- Two raw Git coherent-commit attempts were intentionally abandoned when non-fast-forward protection detected concurrent `main` movement; no force update was used.
- Because concurrent movement made a single raw-Git batch unsafe, the implementation was integrated through SHA-guarded Contents API writes, an explicit exception allowed by the repository batching rule for conflict-safe concurrent integration.
- Current `main` was re-read after integration: `QuantityReportBuilder.cs` contains the length-prefixed `GroupKey(...)`, and the focused smoke is present with the collision/normal-grouping assertions.
- Regression commit `34fb57c7f18fa98824f8926d53b93179b4972f77` was verified as an ancestor of a later current `main` (`472ac7740a5233c3242af2c5a5652efaaf3ac301`).
- No local checkout/.NET smoke execution is claimed in this connector-only lane.
- No BricsCAD runtime or GitHub Actions execution is claimed.

## Coordination

The completed Material Usage Schedule collision lane reserved only `MaterialUsageSchedule.cs` and its dedicated tests. This claim targeted the separate legacy `QuantityReportBuilder.cs` API and did not reopen Material Usage Schedule or Project Quantity reporting.

## Completion condition

Completed. Distinct accepted legacy quantity-report tuples can no longer alias through delimiter injection, focused regression source is present on current `main`, concurrent changes were preserved, and the exact implementation commits are recorded above.
