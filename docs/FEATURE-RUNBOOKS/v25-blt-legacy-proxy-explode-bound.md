# V25 BLT legacy proxy-explode inspection bound

## Scope

`QS3DBLTPROBE`, `QS3DBLTSCAN`, `QS3DBLTAUDIT`, and `QS3DBLTIMPORT` may inspect proprietary `ProxyEntity` objects through a transient `DBObjectCollection` returned by native `Explode`. Entity-scan cardinality is already bounded separately; this contract bounds expensive per-part exact-geometry inspection for one proxy.

## Required source behavior

- `MaxProxyExplodedParts = 4096` is the maximum number of exploded parts eligible for exact per-part Solid3d metric inspection.
- Native `Explode` still owns construction of the transient collection. Immediately after it returns, the adapter records the observed count and rejects exact metric inference when the count exceeds the ceiling.
- Over-limit proxies receive bounded diagnostic metadata and do not run per-part `MassProperties.Volume` / `Area` work.
- The adapter must not silently inspect only the first N parts or publish partial totals as exact geometry.
- Every DBObject returned by `Explode` is disposed from `finally`, including zero-part, over-limit, mixed, malformed and exceptional paths.
- Existing finite/non-negative aggregation, all-solid exact-evidence requirement, metadata limits and read-only source semantics remain unchanged.

## Hosted validation

Run `python scripts/preflight-v25-blt-legacy-proxy-explode-bound.py`, aggregate feature guards, Core smoke, trusted BricsCAD V25 reference validation and V25 plugin build. These establish source/compile safety only.

## LOCAL_ONLY follow-up

On a licensed host with disposable/sanitized legacy data, exercise representative ordinary proxies and a deliberately high-part-count proxy fixture if one can be produced safely. Confirm over-limit input returns without per-part exact metric work, source entities remain unchanged, transient objects are released, and ordinary under-limit exact evidence is unchanged. Do not infer this native/performance result from hosted CI.
