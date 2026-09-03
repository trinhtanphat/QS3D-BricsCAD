# Coordination workbook cell-coordinate integrity

## Scope

This REMOTE_SAFE Core contract protects `CoordinationWorkbookTraceReader` from XLSX cell-coordinate metadata that disagrees with the containing worksheet row. It does not qualify licensed BricsCAD runtime behavior.

## Invariant

Before a cell value or formula marker is admitted into coordination identity/provenance lookup, its `c@r` must be one complete canonical Excel A1 coordinate:

- uppercase column letters only;
- column within A..XFD (1..16,384);
- non-zero canonical decimal row suffix with no leading zero;
- row within 1..1,048,576;
- no trimming, signs, suffix garbage or other ignored characters; and
- the cell row must exactly equal the enclosing `row@r`.

The reader continues to reject duplicate cell columns, identity formulas, invalid shared-string references, unsafe/duplicate worksheet parts, ambiguous TRACE_KEY rows, and ClashId/TRACE_KEY provenance mismatches.

## Deterministic regression

`CoordinationWorkbookCellCoordinateIntegritySmoke` starts from a valid QS3D-generated workbook and rewrites cell coordinates without changing their text payload. It verifies fail-closed behavior for:

- a CLASHES cell declared on a different row;
- a TRACE_MODEL cell declared on a different row;
- trailing coordinate garbage;
- lowercase/non-canonical column spelling;
- a leading-zero row;
- row zero;
- a missing row suffix; and
- column XFE beyond Excel's XFD limit.

The regression is registered by a dedicated module initializer to avoid coupling this carrier to shared smoke-registration paths owned by other lanes.

## TDD evidence

Regression-only head `4973f680ff4afbcad1c492a8d0a11691d7de058a` ran in Shared CI `33717738042`: preflight and Core build passed, then deterministic smoke failed with `Coordination workbook trace reader accepted mismatched cell row coordinate.` This proves the current-main parser accepted the hostile coordinate before the production repair.

## Acceptance

A candidate is mergeable only after the focused auto-discovered source guard, aggregate preflight, deterministic Core smoke, required V25 compile checks and final build all pass on the exact current head, followed by latest-main reconciliation and an expected-head protected merge.
