# V25 BLT legacy selection cardinality bound

## Scope

`QS3DBLTPROBE` can inspect PICKFIRST or explicitly prompted selections through `BltLegacyCadInspector.ReadSelection`. That path performs the same metadata/XData/extension-dictionary/proxy inspection used by the already-bounded Current Space scanner and therefore must share the same admission ceiling.

## Required resource-safety contract

- One `MaxScannedEntities = 250000` constant governs both Current Space and selection inspection.
- Current Space preserves exact-limit acceptance and rejects the first entity beyond the ceiling.
- Selection cardinality is checked before `GetObjectIds()` and before opening the read-only inspection transaction; inputs above the ceiling fail closed with a stable diagnostic rather than silently truncating the result.
- Empty/cancel selection behavior remains unchanged.
- Per-object malformed/proprietary exceptions remain isolated by `TryAdd`; metadata and typed-value caps remain unchanged.
- Source entities remain read-only; this package does not infer or alter proprietary BLT geometry semantics.

## Hosted validation

Run `python scripts/preflight-v25-blt-legacy-selection-bound.py`, aggregate feature guards, Core smoke, trusted BricsCAD V25 references, and V25 plugin build. Hosted validation proves the source/compile contract only.

## LOCAL_ONLY follow-up

On a licensed host, validate an ordinary PICKFIRST selection, manual selection, cancellation, a synthetic exact-limit selection where practical, and an over-limit harness/probe if the host can create one safely. Confirm the over-limit path returns before per-entity/proxy inspection and does not mutate the DWG. Do not infer this runtime evidence from CI.
