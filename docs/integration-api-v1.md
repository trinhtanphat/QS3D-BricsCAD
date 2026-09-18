# QS3D Integration API v1 contracts

`QS3D.Integration.Contracts` is a standalone managed boundary. It deliberately has no BricsCAD dependency, so desktop hosts, standalone IFC/QTO tools, services and test harnesses can exchange the same immutable data.

## Versioning

Every top-level message uses `ApiEnvelope<T>` and carries `qs3d.integration.v1`. Consumers must reject unknown major contract identifiers rather than guessing. Additive fields may be introduced compatibly; removal, semantic reinterpretation, unit changes, identifier changes, or required-field changes require a new major contract identifier.

## Evidence and generation identity

Published quantities carry `EvidenceRef` entries linking the component back to source/model identity, optional native/source handle, revision, fingerprint and semantic generation. `SnapshotRef` carries revision, fingerprint and generation identity. Producers must capture one detached generation and must not combine quantities, BOQ rows or evidence from different generations in one publication.

## Host boundary

BricsCAD-specific adapters belong outside this project. The same contracts are intended for the future QuantBIM-style standalone IFC-QTO workbench and for BricsCAD adapters. No native database object, handle wrapper, UI object or mutable QS3D domain aggregate may cross this boundary.

## Authorization boundary

`AuthorizationContext` transports the authenticated subject, tenant and granted scopes without prescribing an authentication provider. Enforcement belongs to the API host; contracts remain transport- and host-neutral.

## Initial surfaces

The v1 vocabulary includes project/model/source references, quantity/evidence, BOQ, classification, QA findings, revision/snapshot/diff, tender and procurement references. Cost is represented initially through BOQ unit rate/currency; richer cost/tender/procurement DTOs can be added additively while v1 compatibility is preserved.

## Determinism

Identifiers and ordering are producer responsibilities. Producers should emit stable identifiers and deterministic list ordering whenever serialized output, signatures, fingerprints, snapshots or diffs are observable. Consumers must not infer identity from list position.
