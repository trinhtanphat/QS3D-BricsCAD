# Integration API commercial cache coherency

## Problem

QS3D API v1 used the project/model revision when building weak ETags for all cacheable resources. Tender and procurement records also expose mutable workflow `Status` values, and those values can change independently of a BIM/model revision. A Power BI, ERP, or CRM client could therefore reuse an old tender/procurement ETag and receive `304 Not Modified` even though the commercial workflow state had changed.

## v1 behavior

`GET /api/v1/projects/{projectId}/tenders` and `GET /api/v1/projects/{projectId}/procurement` remain API v1 endpoints with the same DTOs, authorization scopes, response bodies, and success/error status-code families. They are now classified as non-cacheable by the v1 endpoint catalog, matching the existing QA/live-state pattern.

The API still computes an ETag value for successful responses for compatibility, but `If-None-Match` is not allowed to suppress the representation for these two mutable commercial resources. Clients therefore receive current tender/procurement state with `200` instead of a false `304`.

Revision-bound resources keep their existing conditional-cache behavior.

## Compatibility

No route, API version, DTO field, scope name, or identity rule changes. Existing integrations do not need a payload migration. Consumers should stop relying on `304` for tender/procurement reads and treat each successful response as the current commercial workflow state.

A future API version can re-enable conditional caching when tender/procurement exposes a deterministic commercial-state version or representation fingerprint that participates in the ETag contract.
