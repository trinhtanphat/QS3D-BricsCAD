# Door XLSX export-status aggregation precision

## Scope

`QS3DDOORXLSX` exports detached, regenerated Door/Opening schedule rows and reports a cross-row opening-area total after the workbook is written. The Core `DoorOpeningScheduleBuilder` already preserves representable mixed-magnitude row aggregates. This contract covers only the V25 command's status-area fold; V26 consumes the shared V25 source tree.

## Defect

A strict pairwise `QuantityReportMath.Add` status fold can reject valid row areas such as `1e16`, `1`, `1`: the first small addend is transiently lost even though the final binary64 value `10000000000000002` is representable. A valid Core schedule could therefore fail in presentation-only arithmetic before workbook publication.

## Required behavior

- Aggregate cross-row `OpeningAreaM2` with bounded compensated state.
- Revalidate every row area through `QuantityReportMath.NonNegative` before accumulation.
- Fail closed on NaN, infinity, overflow, or material final compensation loss.
- Preserve checked `QuantityReportMath.AddCount` element totals.
- Preserve case-insensitive distinct host counting.
- Preserve SaveFileDialog-before-project-read ordering, existing-project-only behavior, detached snapshot regeneration, schedule building, and workbook export.
- Finalize the status area after row traversal and before workbook export/status formatting.
- Keep post-export UI reporting best-effort and keep exception details redacted from the user-facing error path.

## Deterministic regression

The auto-discovered `scripts/preflight-door-export-status-aggregation-precision.py` pins the compensated implementation, strict final representability check, status ordering, and prohibition on pairwise/`+=` accumulation. `scripts/preflight-door-xlsx-error-redaction.py` independently preserves the historical detached-export, ordering and redaction contracts while recognizing the compensated status path.

## Runtime boundary

This is deterministic arithmetic/source correctness. Hosted source guards and locked-reference V25 compilation are authoritative; no licensed BricsCAD `LOCAL_PASS` is required or claimed for this change.
