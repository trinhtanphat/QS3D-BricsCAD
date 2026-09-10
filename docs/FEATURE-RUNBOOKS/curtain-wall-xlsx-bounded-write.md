# Curtain Wall XLSX bounded publication

Carrier: #6307 / PR #6308  
Lane: C02  
Ownership-Key: `c02.curtain-wall-xlsx-bounded-write-v1`

## Root cause

`CurtainWallXlsxExporter` admitted Excel-scale row cardinality, snapshotted rows, then built the complete 20-column worksheet XML in an unbounded `StringBuilder` before ZIP publication. Valid long text/provenance could therefore amplify managed memory before package validation or atomic replacement could fail closed.

## Contract

- Keep the existing source-row snapshot and post-snapshot stability checks.
- Preserve exact WallCount / ElementIds / SourceHandles cardinality and provenance identity.
- Emit worksheet XML through strict UTF-8 into a bounded entry stream; never retain the complete worksheet as a `string`.
- Bound each XML entry to 32 MiB, aggregate uncompressed XML to 64 MiB, and final ZIP output to 64 MiB with checked arithmetic.
- Keep deterministic sheet schema/order, invariant numeric serialization, XML escaping, and fixed ZIP timestamps.
- A byte-budget or encoding failure must occur before destination replacement and must clean the owned temporary file.

## Validation

`python scripts/preflight-curtain-wall-xlsx-bounded-write.py` is the focused source/smoke guard. `CurtainWallXlsxBoundedWriteSmoke` drives multibyte text beyond the worksheet byte budget and verifies that an existing workbook sentinel survives and no owned temp artifact leaks.
