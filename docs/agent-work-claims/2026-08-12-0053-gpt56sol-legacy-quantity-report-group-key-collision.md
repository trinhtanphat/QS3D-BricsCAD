# Work claim — Legacy quantity report collision-free grouping identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-legacy-quantity-report-key-20260812-0053`
- Registered: `2026-08-12T00:53:00+07:00`
- Baseline main SHA: `a37bef51f8757dee3d25bf06c30e5eec04d65c9c`
- Priority: P2 evidence-driven remote-safe reporting integrity

## Confirmed defect

`QuantityReportBuilder.Group(...)` constructs its grouping identity by joining Floor, Category, Family name and normalized Material with an unescaped U+001F delimiter. `ElementInstance.Floor`, `FamilyDefinition.Name`, and `FamilyDefinition.Material` accept trimmed nonblank text without forbidding U+001F. Distinct accepted tuples can therefore serialize to the same dictionary key and merge counts, quantities and provenance incorrectly.

A concrete collision is one Beam row using Floor `F`, Family `Column`, Material `N<US>M`, and one Column row using Floor `F<US>Beam`, Family `N`, Material `M`; both currently serialize to the same delimiter-only key.

## Reserved scope

Replace only the legacy `QuantityReportBuilder.Group(...)` delimiter-only composite key with deterministic collision-free token encoding while preserving case-insensitive grouping, first-seen row ordering, metrics, provenance and accepted text characters.

## Expected surfaces

- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `tests/QS3D.Core.SmokeTests/QuantityReportGroupKeyCollisionSmoke.cs`
- this claim file

## Excluded scope

- No `ProjectQuantityReportBuilder` changes.
- No `MaterialUsageScheduleBuilder` changes; its independently completed collision-fix lane remains untouched.
- No Family/Floor/Material validation restrictions.
- No quantity formulas/business rules, persistence, XLSX, adapter/native or UI changes.
- No GitHub Actions dispatch.

## Validation plan

- Preserve normal grouping for identical tuples.
- Prove the concrete accepted U+001F collision yields two separate rows with independent counts, representative quantities and provenance.
- Use length-prefixed token encoding rather than forbidding accepted text.
- Re-fetch exact source before write, preserve concurrent work, verify claim ancestry and read back current `main` after integration.
- Source/smoke review only; no .NET or BricsCAD runtime PASS unless actually executed.

## Coordination

The completed Material Usage Schedule collision lane reserved only `MaterialUsageSchedule.cs` and its dedicated tests. This claim intentionally targets the separate legacy `QuantityReportBuilder.cs` API and excludes Material Usage Schedule and Project Quantity reporting.

## Completion condition

The claim is complete when the collision-free grouping fix and focused regression are present on current `main`, concurrent changes are preserved, exact implementation SHA(s) are recorded here, and the claim is marked `COMPLETED`.
