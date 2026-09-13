# Integration API scope case-sensitivity

QS3D Integration API v1 treats authorization scopes as exact identifiers. Canonical scopes such as `qs3d.project.read`, `qs3d.cost.read`, `qs3d.workbook.refresh`, and `qs3d.admin` are matched with ordinal case-sensitive semantics.

This is a security-boundary correction, not an API shape change. API version remains `1.0`; routes, DTOs, response codes, ETags, workbook refresh contracts, and canonical scope names are unchanged.

Hosting adapters for Power BI, ERP, CRM, and service-to-service integrations must preserve the exact scope casing issued by the identity provider. Tokens carrying variants such as `QS3D.ADMIN` or `QS3D.COST.READ` no longer authorize canonical lowercase scopes and will fail closed with `403 FORBIDDEN`.

Scope ordering and de-duplication are also ordinal and case-sensitive. This prevents a differently-cased token claim from collapsing into a canonical authorization grant before `HasScope` evaluates it.

Backward compatibility is preserved for clients already using the documented canonical lowercase scopes. Integrations that previously relied on case-insensitive scope matching must correct their token/configuration casing rather than depending on permissive authorization behavior.
