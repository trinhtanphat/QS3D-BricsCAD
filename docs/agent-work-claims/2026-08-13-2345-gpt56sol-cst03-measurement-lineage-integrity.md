# Work claim — CST-03 revision cost measurement-lineage integrity

- Status: `ACTIVE`
- Agent: `gpt56sol-cst03-measurement-lineage-20260813-2345`
- Registered: `2026-08-13T23:45:00+07:00`
- Baseline main SHA: `08f3ff380e51237c1483ea62236ba4257a8ffb1a`
- Priority: `P1` CST-03 revision cost impact integrity.

## Verified gap

`EstimateLine.Create()` selects a canonical measurement trace by the exact tuple `(SemanticIdentity, SourceIdentity, QuantityKey)`. `EstimateRevisionCostImpact.RequireComparable()` currently validates only `EstimateLineId`, unit, currency and cost code. Reusing one line ID for a different measurement tuple can therefore produce a numerically reconciled cost delta across unrelated measurement lineage.

The existing strict-comparability smoke covers line ID, unit, currency and cost code but its helper always uses the same measurement tuple, so this mismatch is not covered.

## Reserved scope

- `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs`
- `tests/QS3D.Core.SmokeTests/EstimateRevisionCostImpactSmoke.cs`
- this claim file

## Excluded scope

- `EstimateLine.cs` and the concurrent zero-adjustment canonicality lane.
- Rate resolution, mapping, persistence, reports, UI, native behavior, MeasurementTrace/MTR-05 provenance work.

## Planned change

Fail closed unless previous/current estimate lines point to the same exact measurement trace identity tuple. Add focused regression cases for semantic identity, source identity and quantity-key mismatch while preserving existing quantity/rate/commercial reconciliation behavior.

## Validation policy

Refresh current `main` and claims after this claim-only commit. No GitHub Actions will be dispatched. Validation will be limited to checks actually executed plus GitHub readback/ancestry verification; no native PASS claim.
