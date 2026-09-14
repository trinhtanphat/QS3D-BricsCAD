# Cubicost Concrete/Formwork domain publication

This boundary closes a P1 Cubicost parity gap between dedicated Concrete/Formwork workflows and the existing commercial downstream pipeline.

`CubicostConcreteFormworkDomainOrchestrator` remains the recognition/review quantity source. `CubicostConcreteFormworkPublicationWorkflow` does not recalculate quantities. It requires both dedicated domain projections to be reviewed and reconciles them by component identity before publication.

The workflow fails closed when Concrete and Formwork cardinality differs, a peer component is missing, classification/storey/review state differs, evidence is not from the same in-memory generation, units are not canonical (`m3` for Concrete and `m2` for Formwork), quantities are invalid, or a downstream classification/formula binding is missing/duplicated.

After reconciliation, each domain row is mapped into the existing `CubicostDownstreamLine` contract. Inventory aggregation and Estimate/Tender/Procurement handoffs are delegated to `CubicostReviewedQuantityDownstreamBridge`; this avoids a second commercial quantity engine and keeps existing compensated aggregation and reviewed-only publication rules authoritative.

## Architecture boundary

The implementation lives in `QS3D.Core` and must remain independent of BricsCAD, AutoCAD and desktop UI frameworks. Native hosts may present or persist the resulting publication, but they must not bypass the reconciliation boundary or recompute domain quantities.

Quantity evidence stays attached to every Concrete/Formwork line and commercial handoff. Recognition and correction evidence remains authoritative; downstream projections are publication views, not new measurement evidence.

## Deterministic validation

`QsCubicostDomainPublicationSmoke` proves reviewed dual-domain publication, inventory plus three commercial destinations, evidence identity preservation, and fail-closed behavior for mismatched evidence generation and missing domain peers. `preflight-cubicost-domain-publication.py` locks the reuse and host-independence constraints.
