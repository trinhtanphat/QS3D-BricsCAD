# Trimble-style construction lifecycle parity

This lane extends the existing Quantity → Cost → Estimate → Tender → Procurement foundation into construction execution and project controls without replacing the earlier `ConstructionCommitment` / `ConstructionLifecycleEngine` compatibility layer.

## Core lifecycle

`TrimbleConstructionLifecycleEngine` adds:

- supplier lifecycle status with an explicit Approved gate before commitments/orders are accepted;
- subcontract commitments with original commitment + approved variation = current commitment;
- purchase-order delivery tracking with ordered, delivered and remaining value;
- package-scoped field progress with planned progress, actual progress and actual cost;
- deterministic package project-control lines joining commitments, deliveries and progress;
- actual-vs-commitment remaining value and schedule variance for downstream ERP/project-control adapters.

Cancelled orders are excluded from ordered/delivered package totals. Unknown or non-approved suppliers fail closed so procurement execution cannot silently bypass vendor governance.

## ERP / project-controls boundary

Adapters should map `ProjectControlLine` into the existing Integration API/ERP host rather than embed ERP SDK dependencies in `QS3D.Core`. Stable integration keys are `PackageId`, supplier id, commitment/order ids and project/revision context supplied by the host.

Recommended downstream projections are current commitment, ordered value, delivered value, actual cost, commitment remaining, planned progress, actual progress and schedule variance. These are sufficient for Power BI/project-controls views while retaining source transaction identities in the adapter layer.

## Compatibility

The original `ConstructionCommitment`, `ConstructionCostControlSummary` and `ConstructionLifecycleEngine` remain available unchanged. The new lifecycle types are additive and can be adopted package-by-package.

## Smoke acceptance

`QsTrimbleConstructionLifecycleSmoke` verifies approved-supplier governance, current commitment including approved variations, ordered/delivered totals, actual cost, commitment remaining and schedule variance across multiple construction packages.