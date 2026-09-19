# QS3D Integration API v1 contracts

`QS3D.Integration.Contracts` is a standalone managed boundary. It deliberately has no BricsCAD dependency, so desktop hosts, standalone IFC/QTO tools, services and test harnesses can exchange the same immutable data.

## Versioning

Every top-level message uses `ApiEnvelope<T>` and carries `qs3d.integration.v1`. Consumers must reject unknown major contract identifiers rather than guessing. Additive fields may be introduced compatibly; removal, semantic reinterpretation, unit changes, identifier changes, or required-field changes require a new major contract identifier.

## Evidence and generation identity

Published quantities carry `EvidenceRef` entries linking the component back to source/model identity, optional native/source handle, revision, fingerprint and semantic generation. `SnapshotRef` carries revision, fingerprint and generation identity. Producers must capture one detached generation and must not combine quantities, BOQ rows or evidence from different generations in one publication. Construction delivery and field-progress evidence follows the same generation rule so downstream ERP/project-control consumers can correlate progress without silently mixing model states.

## Host boundary

BricsCAD-specific adapters belong outside this project. The same contracts are intended for the future QuantBIM-style standalone IFC-QTO workbench and for BricsCAD adapters. No native database object, handle wrapper, UI object or mutable QS3D domain aggregate may cross this boundary.

## Authorization boundary

`AuthorizationContext` transports the authenticated subject, tenant and granted scopes without prescribing an authentication provider. Enforcement belongs to the API host; contracts remain transport- and host-neutral. Construction lifecycle endpoints should use least-privilege scopes such as procurement, cost-control and project-controls read scopes rather than treating project access as implicit authorization.

## Initial surfaces

The v1 vocabulary includes project/model/source references, quantity/evidence, BOQ, classification, QA findings, revision/snapshot/diff, tender and procurement references. Cost is represented initially through BOQ unit rate/currency.

### Construction lifecycle

The additive construction lifecycle vocabulary continues the benchmark flow after `TenderRef` and `ProcurementRef`:

- `SupplierRef` identifies supplier/vendor lifecycle state and optional external-system correlation.
- `SubcontractCommitmentRef` captures committed subcontract value against procurement and supplier identity.
- `PurchaseOrderRef` captures ordered value, required date and supplier correlation.
- `DeliveryRef` tracks planned/received delivery state and carries source evidence.
- `ProcurementProgress` records percent complete plus committed and actual values at a timestamp.
- `CostControlSnapshot` exposes budget, commitment, actual and forecast values for actual-vs-commitment/project-controls reporting.
- `FieldProgressRef` captures work-package progress with evidence and optional project-control correlation.
- `ProjectControlRef` is a provider-neutral schedule/cost/control integration point.
- `ExternalSystemRef` carries provider-neutral ERP/project-control IDs without embedding vendor SDK types.
- `ConstructionLifecyclePublication` binds these surfaces to one project and semantic snapshot.

These contracts intentionally avoid Trimble-, ERP- or scheduling-vendor SDK types. Adapters map external IDs at the edge, allowing Trimble-style construction lifecycle parity while keeping the core interchange usable by other ERP/project-control systems.

## Determinism

Identifiers and ordering are producer responsibilities. Producers should emit stable identifiers and deterministic list ordering whenever serialized output, signatures, fingerprints, snapshots or diffs are observable. Consumers must not infer identity from list position. Monetary values use `decimal`; currencies and units remain explicit. Producers should reject inconsistent currencies, impossible percentages, broken supplier/PO/delivery references and mixed-generation evidence before publication rather than repairing them downstream.
