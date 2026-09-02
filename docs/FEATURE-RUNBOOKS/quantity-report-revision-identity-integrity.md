# Quantity report revision identity integrity

## Scope

`QuantityReportRevisionService` publishes deterministic revision/report provenance from canonical Core state. Required identities must remain nonblank, exact-trim, control-free, and XML-safe before snapshot/report publication.

The public `Capture(ProjectState, snapshotId)` boundary must reject malformed UTF-16 and other XML-invalid identity text rather than allowing it to cross into `QuantityReportRevisionSnapshot.SnapshotId` or the paired semantic revision identity.

Valid supplementary-plane Unicode is preserved exactly. Existing project/domain identity validation, row stable-key uniqueness, semantic revision comparison, quantity finiteness, and report ordering remain unchanged.

## Deterministic regression

`QuantityReportRevisionIdentityIntegritySmoke` covers:

- malformed UTF-16 snapshot identity (`REV\uD800`) rejection;
- XML-invalid non-control snapshot identity (`REV\uFFFF`) rejection;
- valid supplementary-plane Unicode preservation (`REV\U0001F680`).

`scripts/preflight-quantity-report-revision-identity-integrity.py` is auto-discovered and pins the centralized `CanonicalIdentity` XML admission plus the deterministic smoke/runbook contract.

## Runtime classification

Runtime: NOT_APPLICABLE. This is deterministic Core revision/report provenance integrity and does not require licensed BricsCAD execution.
