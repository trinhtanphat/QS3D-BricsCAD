# Platform Quantity Schedule CSV strict Unicode integration

## Scope
This carrier advances the pinned `external/QS3D-Platform` generation to the reviewed upstream Quantity Schedule CSV strict-Unicode fix. It does not change BricsCAD-native runtime behavior or claim licensed-host evidence.

## Root cause
The previously pinned Platform generation counted unpaired UTF-16 surrogates as replacement-character UTF-8 bytes. A later ordinary UTF-8 publication could therefore change semantic/provenance field bytes instead of rejecting malformed text.

## Required integrated contract
- Reject unpaired high and low surrogates before CSV field append.
- Preserve valid supplementary Unicode surrogate pairs.
- Preserve the 16 MiB emitted UTF-8 ceiling with checked accounting.
- Preserve deterministic ordering, invariant numeric `R` formatting, canonical CRLF/quoting, spreadsheet-active-text neutralization, cardinality and source provenance.
- Keep upstream executable malformed-Unicode coverage in the pinned Platform tree.

## Validation
1. `python scripts/preflight-platform-quantity-csv-strict-unicode.py`.
2. Existing Platform Quantity Schedule CSV safety/cardinality/provenance/evidence/output-budget/Unicode smoke coverage.
3. Parent aggregate preflight and `QS3D.Core.SmokeTests`.
4. Exact-head protected Shared CI after latest-main reconciliation.

## Runtime classification
`REMOTE_SAFE` source/integration validation. No licensed BricsCAD V25/V26 runtime PASS is required or implied.