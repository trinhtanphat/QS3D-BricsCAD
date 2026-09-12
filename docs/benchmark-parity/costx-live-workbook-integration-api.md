# CostX-style live workbook + Integration API v1

This benchmark-parity layer is additive to the existing `WorkbookLiveLink`, `WorkbookLiveLinkEngine`, `QsIntegrationResource` and `QsIntegrationRouteCatalog` foundation. Existing callers can continue using those types unchanged. New integrations should prefer `LiveWorkbookRefreshEngine2` and `QsIntegrationApiV1`.

## Live workbook v2

`LiveWorkbookBinding` binds one workbook cell and optional BOQ line to an authoritative source and to upstream workbook bindings:

- BIM element identity (`LiveWorkbookSourceKind.BimElement`), or
- calibrated drawing handle (`LiveWorkbookSourceKind.DrawingHandle`).

Each binding persists source revision, previous accepted value, dependency ids, multiplier and offset. `LiveWorkbookRefreshEngine2` resolves the dependency graph in stable ordinal order, so identical source snapshots always produce the same refresh values regardless of input enumeration order.

Refresh states are explicit:

- `Fresh` — accepted value and source revision remain current;
- `Refreshed` — the current source/dependency cascade changed the value or revision;
- `Stale` — usable evidence exists but its source revision is not the requested current revision;
- `MissingSource` — no authoritative source exists for the binding;
- `Conflict` — duplicate cell/binding identity or conflicting source snapshots make the result ambiguous;
- `Error` — missing/cyclic dependency, an unusable upstream result, or non-finite cascade arithmetic prevents deterministic recalculation.

`LiveWorkbookRefreshResult.Trace` records source evidence plus upstream binding values. `ToNextBinding()` creates the accepted baseline for the next refresh without mutating the prior audit snapshot. A batch reports both `HasBlockingFailure` and `HasStaleData`; publish/estimate adapters should refuse automatic publication when blocking failure is true and should surface stale data visibly rather than silently treating it as current.

## Refresh cascade

The deterministic calculation for a binding is:

`value = (authoritative source quantity + sum(resolved dependency values)) * multiplier + offset`

Bindings with no source can act as deterministic calculated cells over upstream bindings. Cycles are rejected as errors. Conflicting source snapshots are rejected instead of selecting an arbitrary winner. If otherwise finite admitted values overflow or produce `NaN`/infinity during the cascade, the affected binding returns `Error`, preserves its last accepted value, and blocks its dependents through the normal unusable-upstream path. The engine still returns deterministic results for unrelated bindings in the same batch instead of aborting the entire refresh operation.

## REST API v1 contract

`QsIntegrationApiV1Catalog` defines HTTP-oriented contracts under `/api/v1`. The Core project intentionally remains `netstandard2.0` and does not depend on ASP.NET; a Windows service, ASP.NET host, BricsCAD adapter or other transport host maps these descriptors to HTTP and serializes response bodies as JSON.

Read resources:

| Method | Route | Scope |
| --- | --- | --- |
| GET | `/api/v1/projects/{projectId}` | `qs3d.project.read` |
| GET | `/api/v1/projects/{projectId}/sources` | `qs3d.source.read` |
| GET | `/api/v1/projects/{projectId}/quantities` | `qs3d.quantity.read` |
| GET | `/api/v1/projects/{projectId}/boq` | `qs3d.boq.read` |
| GET | `/api/v1/projects/{projectId}/estimates` | `qs3d.cost.read` |
| GET | `/api/v1/projects/{projectId}/classifications` | `qs3d.classification.read` |
| GET | `/api/v1/projects/{projectId}/qa` | `qs3d.qa.read` |
| GET | `/api/v1/projects/{projectId}/revisions` | `qs3d.revision.read` |
| GET | `/api/v1/projects/{projectId}/snapshots` | `qs3d.revision.read` |
| GET | `/api/v1/projects/{projectId}/diffs` | `qs3d.revision.read` |
| GET | `/api/v1/projects/{projectId}/tenders` | `qs3d.tender.read` |
| GET | `/api/v1/projects/{projectId}/procurement` | `qs3d.procurement.read` |
| POST | `/api/v1/projects/{projectId}/workbooks/{workbookId}/refresh` | `qs3d.workbook.refresh` |

The special `qs3d.admin` scope satisfies all endpoint scopes. Missing identity returns 401; missing scope returns 403. The Core contract does not define token parsing or credential storage; the hosting boundary must authenticate OAuth2/JWT/API-key/enterprise identity and construct `QsApiPrincipal` from already-validated claims. This prevents domain code from handling secrets.

### Workbook route identity boundary

The refresh route is fail-closed on workbook identity. Every result in the supplied `LiveWorkbookRefreshBatch` must belong to the `{workbookId}` from the route. A cross-workbook batch returns HTTP 409 with `WORKBOOK_IDENTITY_MISMATCH` before normal refresh-conflict publication logic runs. Transport hosts therefore must not reuse a precomputed batch under a different workbook URL, even when the caller has `qs3d.workbook.refresh`.

This check complements, rather than replaces, project authorization in the hosting layer. `LiveWorkbookBinding` does not currently carry a project id, so the host must load the batch from the already-authorized project context before calling `RefreshWorkbook(...)`.

## DTO and caching contract

Version `1.0` supplies stable DTOs for project, source, quantity, BOQ, estimate, classification/named resources, QA findings, revisions/snapshots, revision diffs, tender and procurement. Cacheable GET resources emit weak revision-aware ETags. Matching `If-None-Match` produces 304. QA and workbook refresh are intentionally non-cacheable decisions.

ETag identity uses deterministic length-framed hexadecimal segments for project id, revision and resource kind rather than delimiter concatenation. This prevents project/revision pairs such as `P1-R2` + `X` and `P1` + `R2-X` from producing the same validator. Existing API routes and DTOs remain unchanged; clients holding a pre-hardening validator simply receive one 200 response and cache the new validator.

Workbook refresh returns 409 with `WORKBOOK_REFRESH_CONFLICT` when the deterministic batch contains a missing source, conflict or dependency error. A route/batch workbook mismatch returns 409 with `WORKBOOK_IDENTITY_MISMATCH`. Stale-but-resolvable refreshes return 200 with `STALE_SOURCE_REVISION` so clients can display the value while preventing silent freshness assumptions.

## Power BI / ERP / CRM hosting guidance

A host should:

1. authenticate the external caller and translate validated claims into `QsApiPrincipal` scopes;
2. authorize the caller for the requested project and load one immutable `QsApiProjectSnapshot` from that project/revision;
3. dispatch GET requests through `QsIntegrationApiV1.Get` and serialize `Body` as JSON;
4. propagate `ETag`, API version and HTTP status code;
5. execute workbook refresh through `LiveWorkbookRefreshEngine2`, verify the route `workbookId` matches the batch, persist accepted `ToNextBinding()` snapshots transactionally, then expose the result through the refresh endpoint;
6. keep source evidence references immutable enough to trace a Power BI/ERP/CRM value back to the BIM element or drawing handle that produced it.

## Backward compatibility

No existing benchmark type or route is removed. The original `WorkbookLiveLinkEngine` and four-route `QsIntegrationRouteCatalog` remain valid. V2 live-link and API v1 types use new names and can be adopted incrementally by existing workbook/export adapters. The ETag hardening changes only cache-validator values, not DTO shape, routes, scopes or response representation. Non-finite cascade containment changes only a previous exceptional failure mode: callers now receive a normal batch with blocking `Error` results and preserved last accepted values.

## Smoke coverage

`QsLiveWorkbookApiSmoke` covers:

- input-order-independent BIM + drawing refresh cascade;
- source evidence and upstream trace;
- stale and missing-source indicators;
- accepted refresh becoming fresh on the next pass;
- conflicting source snapshots;
- dependency-cycle rejection;
- non-finite cascade containment, downstream blocking and unrelated-row continuation;
- API route coverage including tender/procurement/workbook refresh;
- authentication, per-resource authorization and conditional GET;
- collision-safe project/revision ETags;
- 409 workbook refresh conflicts; and
- 409 rejection when a refresh batch belongs to a different workbook than the route.
