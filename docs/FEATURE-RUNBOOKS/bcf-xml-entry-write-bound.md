# BCF XML entry write bound

Lane-Key: `issue-5261`

## Scope

This runbook covers deterministic Core resource/correctness behavior for BCF XML entry serialization. No licensed BricsCAD runtime qualification is required.

## Defect

`BcfZipPackage.Write` historically built a complete markup/viewpoint/version XML string and then UTF-8 encoded the complete string before comparing the resulting byte array with `MaxEntryBytes` (2 MiB). Because BCF text values are XML/canonical validated but not individually size-limited and a topic may contain many comments, valid bounded-cardinality input could make the writer materialize an XML/UTF-8 payload materially above the public per-entry ceiling before rejection.

The archive-level write ceiling from #5252 begins after these entry payloads are built, so it does not solve this earlier allocation boundary.

## Required contract

- `MaxEntryBytes` remains the canonical 2 MiB entry ceiling.
- XML serialization writes through a bounded UTF-8 stream before any complete XML string/byte-array materialization.
- The bounded stream rejects the write that would cross `MaxEntryBytes` before its backing buffer grows.
- XML remains deterministic, strict UTF-8, non-indented and declaration-bearing.
- Existing BCF GUID/reference/cardinality/path/timestamp contracts remain unchanged.
- `WriteTextEntry` retains its final byte-length assertion as defense in depth.
- The package-level 16 MiB bounded archive stream from #5252 remains unchanged.

## Deterministic regression

`BcfXmlEntryWriteBoundSmoke` creates 600 individually valid comments whose aggregate markup exceeds 2 MiB and requires the canonical entry-size failure. A small package is written/read and checked for unchanged topic identity. `scripts/preflight-bcf-xml-entry-write-bound.py` pins bounded XML serialization before complete byte materialization and rejects a regression to direct unbounded `XDocument.ToString(...)` entry construction.

## Validation

Run the focused preflight, deterministic Core smoke suite and Core Release build through Shared CI. Reconcile current protected main non-force before merge and require exact-head protected `preflight` and `core` SUCCESS. Merge only through the protected PR path with expected-head protection, then verify exact protected main contains the candidate.

Runtime: `NOT_APPLICABLE`.
