# V25 BLT legacy Xrecord ResultBuffer lifetime

## Scope

`QS3DBLTPROBE`, `QS3DBLTSCAN`, `QS3DBLTAUDIT` and `QS3DBLTIMPORT` inspect extension-dictionary `Xrecord` payloads through `ResultBuffer` instances returned by `Xrecord.Data`.

## Required source behavior

- Each `Xrecord.Data` property is evaluated at most once for one inspected record.
- The exact returned `ResultBuffer` instance is owned by a `using` scope and is deterministically disposed.
- Null payloads remain accepted and skipped without changing metadata semantics.
- Existing typed-value parsing, count/value bounds, ordering and fail-soft per-record behavior remain unchanged.
- Do not add a second null-probe property access before the `using` acquisition.

## Hosted validation

Run `python scripts/preflight-v25-blt-legacy-xrecord-resultbuffer-lifetime.py`, aggregate feature guards, deterministic smoke tests, trusted V25 reference validation and the locked-reference V25 plugin build. Hosted evidence proves source/compile behavior only.

## LOCAL_ONLY follow-up

On a licensed disposable V25 host, repeatedly probe/scan a representative drawing containing extension-dictionary `Xrecord` data and confirm metadata remains stable and no host/native resource growth attributable to duplicate `ResultBuffer` acquisition is observed. Do not infer this native/runtime result from hosted CI.
