# Integration API v1 authorization boundary

`QsIntegrationApiV1` must authenticate and authorize a request before revealing whether a project identifier exists.

For a supported GET route, response ordering is therefore:

1. validate the requested method/resource route;
2. require an authenticated principal;
3. require the route's scope (or `qs3d.admin`);
4. only then resolve the requested project identifier;
5. return the resource or cache response.

This prevents project-ID enumeration through status-code differences. An unauthenticated request receives `401 UNAUTHENTICATED` for both existing and non-existing project IDs. An authenticated principal lacking the required resource scope receives `403 FORBIDDEN` for both. A correctly scoped principal retains the existing `404 PROJECT_NOT_FOUND` behavior for an unknown project.

The ordering is a security hardening only. API version `1.0`, route templates, scope names, DTO contracts, ETag behavior, successful response payloads, and workbook-refresh authorization remain backward compatible.

Regression coverage lives in `QsIntegrationApiAuthBoundarySmoke` and must execute automatically with the smoke assembly. Hosted CI proves the managed API contract only; it is not an external ERP/Power BI deployment certification.
