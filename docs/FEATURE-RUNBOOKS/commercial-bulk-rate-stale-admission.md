# Commercial bulk-rate stale admission

## Contract

Bulk rate assignment must not reprice an estimate line while its quantity-source provenance is marked stale. A stale line is a blocking preview row just like an explicitly blocked line, so `BulkRateAssignmentPreview.CanCommit` remains false and commit performs no commercial mutation or audit append.

A preview that was valid while a line was active must also fail closed if that line becomes stale before commit. This transition is guarded by `SourceLinesMatch`, which compares both `IsStale` and `StaleReason` against the preview snapshot before any rate mutation occurs.

## Deterministic acceptance

`BulkRateAssignmentStaleAdmissionSmoke` covers three cases:

1. An already-stale quantity-source estimate line is surfaced in `BlockedLineIds`, produces `CanCommit == false`, cannot commit, and retains its original rate state with no audit append.
2. An active line can produce a ready preview, but if `MarkQuantitySourceStale` changes its state before commit the stale preview is rejected with no rate mutation and no audit append.
3. A non-stale control line remains assignable; the requested referenced-rate provenance, exact amount, and one `rate-assigned` audit event are retained.

The auto-discovered source guard is `scripts/preflight-commercial-bulk-rate-stale-admission.py`. This package is deterministic Core/commercial validation and does not require licensed BricsCAD runtime evidence.
