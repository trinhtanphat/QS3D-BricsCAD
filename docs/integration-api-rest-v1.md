# QS3D Integration REST API v1

The standalone host exposes the BricsCAD-independent `QS3D.Integration.Contracts` vocabulary under `/api/v1`. Native CAD adapters populate `IIntegrationReadStore`; native database objects never cross the HTTP boundary.

## Authentication and authorization

The host authorizes an already authenticated `ClaimsPrincipal`. Deployment must install an authentication handler (OIDC/JWT, Windows, gateway or another ASP.NET Core provider) that supplies a stable subject, optional `tenant_id`, and one or more `scope` claims. The API never trusts query-string identity or scope. Anonymous callers receive 401; authenticated callers missing the route scope receive 403. Stores/adapters must additionally enforce tenant ownership when resolving IDs; a caller's tenant is available through `AuthorizationContext` and must not be inferred from a resource identifier.

Read scopes are `project:read`, `model:read`, `source:read`, `quantity:read`, `boq:read`, `classification:read`, `qa:read`, `revision:read`, `tender:read`, and `procurement:read`.

## Routes

`GET /api/v1/projects/{id}`, `/models/{id}`, `/sources/{id}`, `/quantities/{id}`, `/boq/{id}`, `/classifications/{id}`, `/qa/{id}`, `/revisions/{id}`, `/snapshots/{id}`, `/diffs/{id}`, `/tenders/{id}`, and `/procurements/{id}` return a `qs3d.integration.v1` envelope. Missing resources return RFC 7807 problem details. Cost currently travels on BOQ unit-rate/currency as defined by the v1 contract; richer cost DTOs remain additive v1 evolution.

## Determinism and live provenance

Adapters must read one detached semantic generation per response and preserve source handle, revision, fingerprint and generation identifiers from the contracts. Lists exposed by future collection routes must be explicitly ordered by stable identifiers before serialization. Never recompute a stale quantity from mutable CAD state while constructing a response; reject or refresh the generation as one unit.

## Power BI / ERP / CRM

Clients should pin `/api/v1`, send the deployment provider's bearer/session credentials, request only required read scopes, and persist contract IDs rather than array positions. A Power BI connector can call the quantity/BOQ endpoints and retain `EvidenceRef.SourceHandle`, revision/fingerprint and generation identity for drill-through. ERP/CRM integrations should use project/BOQ/tender/procurement IDs as external keys and treat unknown major contract identifiers as incompatible.

## Compatibility

Routes under `/api/v1` obey the `qs3d.integration.v1` additive compatibility policy. Existing fields, meanings, units and identifiers cannot be removed or reinterpreted within v1. Breaking HTTP or DTO changes require `/api/v2` plus a new contract major identifier.


## Cost and project boundaries

`GET /api/v1/estimates/{id}` requires `cost:read` and returns the v1 `EstimateRef` contract. Read stores may attach `ResourceAccess(projectId, tenantId)` to a resource; the host checks `tenant_id` and optional `project_id` claims before reading the payload. This prevents cross-tenant/project disclosure while preserving existing `/api/v1` routes and envelopes. Store implementations remain the application-composition seam; the default empty store publishes no synthetic business data.
