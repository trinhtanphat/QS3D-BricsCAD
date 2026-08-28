# Issue #4284 — Estimating known-Count overrun ordering

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4284`

Canonical owner: independent QS3D schedule worker `C01`

Runtime: `NOT_APPLICABLE` — deterministic Core Commercial/Estimating integrity.

## Problem

`EstimatingPortfolio` and `BulkRateAssignmentRequest` already bind supported generic, read-only and non-generic `Count` metadata before traversal and reject final cardinality mismatch. Before this lane, an underreported Count was detected only after the unexpected element had already reached semantic processing.

For a trustworthy known Count N, observing element N+1 is itself the collection-integrity failure. The unexpected element must not reach null, token or duplicate validation first.

## Hardened boundaries

This carrier hardens exactly three traversal boundaries in `EstimatingWorkflow.cs`:

1. `EstimatingPortfolio(IEnumerable<EstimatingLine>)`;
2. `BulkRateAssignmentRequest.lineIds`;
3. `BulkRateAssignmentRequest.unitRates`.

Each boundary now checks the observed cardinality against the accepted known Count at the start of each loop iteration, before processing the yielded item. Existing maximum-entry guards remain independently authoritative for sources that do not expose a supported known Count.

## Preserved contracts

The change intentionally preserves:

- negative, oversized and conflicting known-Count rejection before enumeration;
- post-enumeration rejection when a source yields fewer items than its known Count;
- the 10,000 estimating-line limit;
- the 10,000 selected-line limit;
- the 256 unit-rate limit;
- pure streaming source behavior when no known Count is available;
- case-insensitive estimating identity and duplicate rules;
- selected-line and unit-rate input ordering;
- estimating arithmetic, rates, quantity provenance and audit behavior.

## Deterministic regression

`EstimatingKnownCountOverrunOrderingSmoke` uses adversarial `IReadOnlyCollection<T>` sources that advertise Count 1 but yield a valid first item followed by an invalid second item. The expected failure is the Count-integrity `InvalidOperationException`, proving the unexpected item did not reach semantic validation.

Controls also cover under-traversal and honest counted inputs. Existing `EstimatingPortfolioCountIntegritySmoke` and `BulkRateAssignmentRequestCountIntegritySmoke` remain the regression authority for malformed known Counts, streaming bounds and legacy identity/order behavior.

`scripts/preflight-estimating-known-count-overrun.py` statically locks all three early guard call sites, their ordering before semantic validation, the new smoke matrix and the legacy Count-integrity controls.

## Validation and landing

This is Core-only repository-safe work. No BricsCAD host, private DWG, signing evidence, package runtime or `LOCAL_PASS` is applicable.

Required landing path is:

1. exact-head automatic branch CI `SUCCESS`;
2. reconcile latest exact `main` on this canonical branch when necessary;
3. obtain fresh exact-head branch CI after reconciliation;
4. open/continue the one canonical PR with `Lane-Key: issue-4284`;
5. protected current-candidate `preflight` and `core` terminal `SUCCESS`;
6. strict-current mergeability and expected-head verification;
7. same-task protected PR merge and exact resulting `main` verification.
