# Curtain XLSX provenance vector integrity

## Contract

Each exported curtain-wall worksheet row carries parallel per-wall provenance vectors: `ElementIds` and `SourceHandles`. A row is admissible only when `WallCount == ElementIds.Count == SourceHandles.Count`. Positional provenance must never be emitted with a shorter or longer source-handle vector.

The exporter must reject the mismatch before package commit, with a deterministic worksheet-row diagnostic. Existing cell text limits, XML safety, non-negative/range validation, row/list mutation-stability checks, atomic replacement and generated-package validation remain unchanged.

## Deterministic acceptance

The Core smoke covers three adversarial cases: a short source-handle vector, a long source-handle vector, and a matched vector. Both mismatches must fail closed without replacing destination bytes; the matched vector must export successfully with the original positional ordering preserved.

`python scripts/preflight-curtain-xlsx-provenance-vector-integrity.py` guards both the production cross-vector invariant and smoke registration.

## Runtime classification

REMOTE_SAFE. This is deterministic Core XLSX/export data integrity. Licensed BricsCAD or private-DWG execution is not required and must not be claimed as `LOCAL_PASS`.
