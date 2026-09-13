# Integration API v1 exact resource identity casing

## Contract

Project IDs and workbook IDs are opaque integration identifiers. API v1 now compares those identifiers with ordinal, case-sensitive semantics at the resource boundary.

- `GET /api/v1/projects/{projectId}/...` returns `PROJECT_NOT_FOUND` when the route project ID differs from the snapshot project ID by case only.
- `POST /api/v1/projects/{projectId}/workbooks/{workbookId}/refresh` returns `WORKBOOK_IDENTITY_MISMATCH` when any refresh result belongs to a workbook whose ID differs from the requested workbook ID by case only.
- API version `1.0`, routes, DTO field names, authorization scopes, status-code families, ETag framing, and response content type are unchanged.

## Why this matters

Power BI, ERP, CRM, document stores, and federated BIM systems may treat external IDs as case-sensitive opaque keys. Case-insensitive matching can alias two distinct upstream resources and silently return or refresh the wrong project/workbook.

## Migration guidance

Consumers should preserve the exact casing returned by QS3D or the upstream system of record. Do not lowercase or uppercase `projectId` or `workbookId` before calling API v1. Existing clients that already round-trip identifiers unchanged require no changes.

This tightening is intentionally fail-closed: a case-only mismatch is rejected rather than guessed or canonicalized.
