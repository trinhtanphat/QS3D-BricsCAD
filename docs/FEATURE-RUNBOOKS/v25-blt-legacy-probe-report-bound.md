# V25 BLT legacy probe-report resource bound

## Scope

`QS3DBLTPROBE` serializes selected legacy/proxy snapshots to a JSON report in the process temp directory. Upstream selection cardinality, metadata entry count and metadata value length are bounded, but those independent bounds can still multiply into a very large aggregate report.

## Required source behavior

- The report has one explicit maximum UTF-8 byte budget (`MaxProbeReportBytes`).
- JSON is written incrementally through a bounded stream/writer; the implementation must not build the complete report in a `StringBuilder` and then call `File.WriteAllText`.
- The writer checks the cumulative output size before publication and fails closed when the budget would be exceeded.
- Output is first written to a unique temporary sibling artifact. Only a completely serialized, flushed report may be renamed/moved to the final `.json` pathname.
- Exceptional/over-budget paths delete the partial temporary artifact and never return a path that represents a partial report.
- Ordinary reports preserve schema `QS3D_BLT_LEGACY_PROBE_V1`, object order, metadata ordering, JSON escaping and invariant numeric formatting. There is no silent object/metadata truncation at the report layer.

## Hosted validation

Run `python scripts/preflight-v25-blt-legacy-probe-report-bound.py`, aggregate feature guards, deterministic smoke tests, trusted V25 reference validation and V25 plugin compilation. These prove source/compile behavior only.

## LOCAL_ONLY follow-up

On a licensed disposable host, exercise a representative under-budget BLT probe and a deliberately high-cardinality/high-metadata fixture if available. Confirm the ordinary report remains valid JSON, an over-budget report fails without a partial published JSON artifact, temp cleanup succeeds, and host memory remains bounded relative to streamed output. Hosted CI must not be reported as native/runtime PASS.
