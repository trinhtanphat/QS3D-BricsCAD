# Work claim — CST-03 revision cost measurement-lineage integrity

- Status: `RELEASED`
- Agent: `gpt56sol-cst03-measurement-lineage-20260813-2345`
- Registered: `2026-08-13T23:45:00+07:00`
- Released: `2026-08-13T23:50:00+07:00`
- Baseline main SHA: `08f3ff380e51237c1483ea62236ba4257a8ffb1a`
- Priority: `P1` CST-03 revision cost impact integrity.

## Verified gap

`EstimateLine.Create()` selects a canonical measurement trace by the exact tuple `(SemanticIdentity, SourceIdentity, QuantityKey)`. The baseline `EstimateRevisionCostImpact.RequireComparable()` validated only `EstimateLineId`, unit, currency and cost code. Reusing one line ID for a different measurement tuple could therefore produce a numerically reconciled cost delta across unrelated measurement lineage.

The existing strict-comparability smoke covers line ID, unit, currency and cost code but its helper always uses the same measurement tuple, so this mismatch is not covered.

## Reserved scope

- `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs`
- `tests/QS3D.Core.SmokeTests/EstimateRevisionCostImpactSmoke.cs`
- this claim file

## Excluded scope

- `EstimateLine.cs` and the concurrent zero-adjustment canonicality lane.
- Rate resolution, mapping, persistence, reports, UI, native behavior, MeasurementTrace/MTR-05 provenance work.

## Published source state

Implementation commit `5a99e10e21e8975f15ae48b5ff979082ac49ba01` added exact ordinal comparison of `SemanticIdentity`, `SourceIdentity` and `QuantityKey` before cost-delta arithmetic. GitHub readback on later `main` confirmed those guards are present.

## Release reason / validation

Focused regression publication was attempted through both the existing smoke-file update route and two smaller new smoke-file routes. Each test write was blocked by the connector safety gate before mutation. A restore tree pointing the production file back to its pre-lane blob was then prepared, but commit creation for that restore was also blocked before mutation.

No GitHub Actions were dispatched. No managed smoke, build, or native BricsCAD runtime PASS is claimed. Because the required regression could not be published or executed, this lane is not `COMPLETED` and is released rather than left `ACTIVE`.

A future claimant should refresh current `main`, re-verify the source guard, add the missing focused regression for semantic/source/quantity-key mismatch, execute whatever LOCAL managed gate is actually available, and then close the lane only with real evidence.
