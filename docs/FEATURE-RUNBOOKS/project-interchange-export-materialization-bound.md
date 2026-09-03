# Project interchange export materialization bound

Lane-Key: `issue-5271`

## User-visible defect

`ProjectInterchangeJsonExporter.Build` historically accumulated the full semantic snapshot in an unbounded `StringBuilder` and only enforced `ProjectInterchangeJsonValidator.MaxFileBytes` after final `ToString()` materialization. A project containing individually valid property values could therefore build an aggregate snapshot beyond the canonical 16 MiB package ceiling before rejection.

## Correctness contract

The exporter owns a bounded UTF-8 builder whose byte counter uses strict UTF-8 encoding and the same `ProjectInterchangeJsonValidator.MaxFileBytes` public ceiling as canonical validation. Every append is admitted before it is copied into the backing builder. Once an append would cross the ceiling, export fails closed with `InvalidDataException` before final `ToString()` materialization.

The bound is a UTF-8 byte bound, not a UTF-16 character-count approximation. Invalid surrogate input remains fail-closed. Ordinary snapshots must retain the existing deterministic ordering, escaping, numeric formatting, reference validation, and canonical-validation result.

## Deterministic regression

`ProjectInterchangeExportMaterializationBoundSmoke` builds one family containing 600 individually valid 32,768-character property values. The aggregate is intentionally larger than 16 MiB while remaining below the 4,096-member family-property limit. The smoke requires rejection from the bounded exporter path and separately verifies an ordinary semantic snapshot remains byte-for-byte deterministic and accepted by `ProjectInterchangeJsonValidator`.

Run the focused source guard:

```text
python scripts/preflight-project-interchange-export-materialization-bound.py
```

The regression is also registered in the deterministic Core smoke suite and is exercised by protected Shared CI.

## Runtime boundary

No licensed BricsCAD runtime is required. This is deterministic Core serialization/resource correctness only; runtime status is `NOT_APPLICABLE`. No `LOCAL_PASS` claim is part of this lane.
