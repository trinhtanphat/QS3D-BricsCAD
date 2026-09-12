# Trimble construction lifecycle — project-controls / ERP boundary

This carrier extends the canonical `TrimbleConstructionLifecycleEngine` merged by #6474. It does not introduce another procurement or construction lifecycle engine.

## Canonical flow

`Quantity → Cost → Estimate → Tender → Procurement → Commitment / PO / Delivery → Field progress / Actual cost → Project controls → ERP / Power BI adapter`

`TrimbleConstructionLifecycleEngine.BuildProjectControls(...)` remains authoritative for package aggregation. Approved suppliers are required for commitments and orders, cancelled purchase orders are excluded, and package controls retain commitment, ordered, delivered, actual-cost and planned/actual-progress values.

## Interchange contract

`TrimbleProjectControlsInterop` converts canonical `ProjectControlLine` values into deterministic `TrimbleProjectControlExportRow` records. Rows are ordered by package id and carry:

- schema version `qs3d.trimble.project-controls.v1`;
- caller-supplied source revision / snapshot identity;
- package id;
- commitment, ordered, delivered and actual cost;
- ordered/commitment procurement ratio;
- delivered/ordered delivery ratio;
- actual/commitment ratio;
- commitment variance and non-negative cost-to-complete;
- planned progress, actual progress and schedule variance.

The export fails closed when non-zero activity would require division by a zero denominator, when financial values are negative, when delivered value exceeds ordered value, or when progress is outside `[0,1]`. Zero/zero ratios normalize to zero.

`ToCsv(...)` is an interchange convenience, not an ERP SDK. It emits invariant-culture numbers and deterministic package ordering. ERP, Power BI, CRM, accounting and project-control SDK dependencies belong in outer adapters; `QS3D.Core` remains host-neutral.

## Compatibility

Existing `ProjectControlLine` and `TrimbleConstructionLifecycleEngine` APIs are unchanged. Consumers that only need the original construction-control aggregation do not need to adopt the interchange layer. New integrations should pin the schema version and source revision and treat a future schema version as an explicit migration boundary.

## Regression boundary

`QsTrimbleConstructionLifecycleSmoke` verifies cancelled-order exclusion, deterministic ordering, procurement/delivery/actual ratios, source revision propagation, stable CSV evidence, and zero-denominator fail-closed behavior.
