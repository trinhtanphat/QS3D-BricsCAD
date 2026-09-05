# BCF ZIP text-entry encoding canonicality

## Scope

Issue #5230 / Lane-Key `issue-5230` hardens the Core BCF ZIP importer only. The package writer already emits XML text bytes using strict UTF-8 without a BOM and declares UTF-8 where it emits an XML declaration. Import must not reinterpret those text entries through BOM-selected UTF-16 or UTF-32 decoders.

Affected text entry classes are `bcf.version`, optional legacy `extensions.xml`, per-topic `markup.bcf`, and referenced `.bcfv` viewpoint files. Archive path, entry-size, total-size, cardinality, vocabulary, XML shape, numeric and timestamp rules are unchanged.

## Defect

The previous `ReadText` constructed `StreamReader` with `detectEncodingFromByteOrderMarks: true`. A syntactically valid BCF XML document encoded with a UTF-16/UTF-32 BOM could therefore be converted to a .NET string before the existing XML validators ran, despite the QS3D package boundary being canonical UTF-8.

## Contract

`BcfZipPackage` uses one strict `UTF8Encoding(false, true)` instance for both writer bytes and reader decoding. Reader BOM auto-detection is disabled. Invalid non-UTF-8 bytes fail closed and are normalized by the existing package-validation exception boundary; no alternate text encoding becomes an accepted alias of the canonical package.

Valid UTF-8 packages, including the existing standard-vocabulary package without `extensions.xml` and the legacy QS3D `extensions.xml` subset, retain their existing semantics.

## Deterministic regression

`BcfZipPackageSmoke` rewrites otherwise-valid package entries using BOM-bearing alternate encodings and requires rejection for all reader paths:

- `bcf.version`: UTF-16 LE;
- `markup.bcf`: UTF-16 BE;
- `.bcfv`: UTF-32 LE;
- legacy `extensions.xml`: UTF-32 BE.

The ordinary byte-deterministic writer, semantic round-trip, legacy extension, malformed XML, path, numeric/timestamp and nested-cardinality suites remain authoritative companion coverage.

`scripts/preflight-bcf-zip-text-encoding.py` is auto-discovered and pins the strict reader/writer tokens plus hostile smoke coverage while explicitly forbidding BOM auto-detection.

## Validation

Remote acceptance is deterministic Core/static validation: focused preflight, full aggregate feature guards, Core Release build/smoke and the protected PR `preflight` + `core` contexts on the exact candidate. Licensed BricsCAD runtime is not applicable to this Core serialization package and must not be claimed.
