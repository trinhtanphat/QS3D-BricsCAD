# QSDB read path-affinity

## Scope

This REMOTE_SAFE Core persistence carrier closes the residual read-side TOCTOU gap in `QsdbProjectStore.LoadDocument`. It does not claim licensed BricsCAD runtime validation.

## Defect

Pathname validation before and after `FileStream` acquisition is insufficient: a redirected or replaced ancestor can exist only while the stream is opened and then be restored before the second pathname check. The held stream may therefore reference a different filesystem generation than the canonical pathname subsequently inspected.

## Required contract

After opening the read stream and before observing its length or constructing the XML reader, call `PersistencePathSafety.RequireExclusiveOpenStillBound(stream, fullPath, "project read")`. This binds the held OS handle to the admitted canonical pathname generation and fails closed on redirect/replacement drift.

Preserve the 64 MiB bound, prohibited DTD processing, null XML resolver, backup fallback behavior, recoverable-data exception classification, and supported Windows product boundary.

## Deterministic validation

Run `python scripts/preflight-qsdb-read-path-affinity.py`, the auto-discovered aggregate preflight, and the deterministic Core smoke suite. Protected merge evidence remains fresh exact-candidate `preflight` + `core` SUCCESS after latest-main reconciliation.

Hosted/static evidence must not be reported as licensed BricsCAD `LOCAL_PASS`.
