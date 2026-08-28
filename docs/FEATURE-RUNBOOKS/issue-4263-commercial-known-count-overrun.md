# Issue #4263 — Commercial known-Count overrun ordering

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4263`

Canonical owner: independent schedule worker `C01`

Runtime: `NOT_APPLICABLE` — deterministic Core Commercial collection integrity.

## Contract

For Commercial collection boundaries that already bind a trustworthy known `Count`, observing the first item beyond that Count is itself the failure. The unexpected item must not be null/identity/duplicate validated or retained before rejection.

This carrier hardens:

- `CommercialAuditLog.AppendBatch(...)`;
- `CommercialGuard.Snapshot<T>(...)`, currently used by `CommercialAuditRecord.sourceRevisions`.

The guard runs before semantic item processing. Existing pre-enumeration negative/oversized/conflicting Count checks and post-traversal under-yield checks remain unchanged. Sources without a supported known Count retain the independent streaming bounds.

Rejected audit batches remain failure-atomic because publication still occurs only after the whole snapshot validates.

## Deterministic regression

`CommercialKnownCountOverrunSmoke` proves:

- an underreported audit batch whose first unexpected record is `null` fails at the known-Count overrun boundary before null validation;
- an underreported source-revision collection whose first unexpected item is `null` fails at the same boundary;
- under-traversal still fails only after completed otherwise-valid traversal;
- rejected audit batches publish zero events;
- honest counted and pure-streaming controls remain accepted.

`scripts/preflight-commercial-known-count-overrun.py` statically locks both the source call sites and the ordering of the overrun guard before semantic validation.

## Exclusions

This lane does not change estimating formulas, rates, quantity logic, export formats, adapters, native BricsCAD behavior, or licensed runtime. Analogous `EstimatingPortfolio` / `BulkRateAssignmentRequest` traversal ordering is intentionally left for a separate collision-scanned follow-up after this carrier is terminal.

## Merge gate

Normal source lifecycle applies: exact current-main reconciliation, protected PR, terminal `preflight` + `core` SUCCESS for the exact candidate, strict freshness/mergeability, then expected-head same-task merge. No `LOCAL_PASS` claim is applicable.
