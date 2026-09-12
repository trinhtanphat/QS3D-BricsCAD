# Cubicost reviewed-quantity downstream integration

This carrier closes the handoff gap between the existing Cubicost Concrete/Formwork reconstruction workflow and commercial QS consumers without introducing another quantity engine.

## Authority boundary

`CubicostConcreteFormworkWorkflow.Quantify(...)` remains authoritative for reconstructed Concrete/Formwork quantities and review state. `CubicostReviewedQuantityDownstreamBridge` only admits and projects those canonical `CubicostQuantityLine` records.

The bridge requires an explicit `CubicostDownstreamBinding` for each source classification. A binding carries the concrete and formwork formula identifiers used by the existing formula/classification layer. Missing or duplicate bindings fail closed instead of guessing a cost/formula mapping.

## Review gate

The default admission policy allows only `Accepted` and `Corrected` quantities. `Rejected` rows never flow downstream. `Proposed` rows are rejected unless the caller explicitly enables proposed admission for a review-oriented preview; even then, `BuildInventory(...)` and `BuildCommercialHandoffs(...)` refuse to publish proposed rows commercially.

This creates a deterministic boundary between recognition/correction and downstream commercial use.

## Traceability

Every downstream line retains the original:

- component id and source classification;
- review status;
- source/revision/method/confidence through the same `QuantityEvidence` instance;
- explicit formula id;
- inventory classification and unit.

Concrete is projected as `<classification>.CONCRETE` in `m3`; formwork is projected as `<classification>.FORMWORK` in `m2`. Ordering is deterministic by inventory classification and component id.

## Commercial integration

`BuildInventory(...)` produces the existing `TakeoffInventoryLine` contract, so the result can reuse current inventory/estimate infrastructure rather than creating a parallel BOQ model. `BuildCommercialHandoffs(...)` provides transport-neutral Estimate, Tender and Procurement handoff records while retaining the same evidence-bearing line. Host/application adapters remain responsible for mapping those records into their existing transaction/repository APIs.

The bridge deliberately does not calculate rates, estimate values, tender prices or procurement costs. Those remain owned by the existing formula/rate-book/estimate/tender/procurement systems.

## Validation and compatibility

The bridge fails closed for duplicate component identity, duplicate or missing classification bindings, proposed/unreviewed publication, invalid quantity values and duplicate downstream lines. Existing `CubicostConcreteFormworkWorkflow`, `TakeoffInventoryLine`, rebar/MEP workflows and BricsCAD adapters are unchanged.

Smoke coverage exercises accepted/corrected evidence propagation, deterministic inventory projection, Estimate/Tender/Procurement handoffs, proposed-review gating, missing mappings and duplicate component rejection.
