# V25 BLT legacy aggregate snapshot budget

## Purpose

Protect the clean-room BLT legacy scan/import/probe adapter from retaining an unbounded aggregate managed snapshot graph while preserving the existing read-only native inspection and fail-closed evidence rules.

## Source contract

`BltLegacyCadInspector.ReadCurrentSpace` and `ReadSelection` share one deterministic aggregate retained-snapshot budget. Each completed `EntitySnapshot` is measured before it is appended to the retained result list. The estimate includes retained Handle/entity-type/layer text, metadata key/value text, one fixed structural reservation per snapshot, and one fixed structural reservation per metadata entry.

The budget is a deterministic admission contract, not a claim about exact CLR heap accounting. Text is charged using UTF-8 byte count; structural reservations prevent many tiny metadata entries from becoming effectively free. The aggregate ceiling is 64 MiB, aligned with the already-established BLT probe-report output ceiling while remaining independently enforced before command-level adaptation/filtering.

If the next completed snapshot would cross the ceiling, the scan throws before appending that snapshot. There is no partial-success/truncation path. Per-object malformed/proprietary inspection failures remain isolated as before; aggregate-budget exhaustion is a scan-level failure and must not be swallowed by the per-object fail-soft catch.

## Preserved boundaries

- Current Space and selection entity cardinality remain capped at 250,000.
- Per-snapshot metadata stays capped at 512 entries, typed-value extraction at 256 values, and retained metadata values at 512 characters.
- Source CAD objects remain read-only; no source entity is exploded destructively, erased, converted, or redrawn.
- Proxy/native behavior and performance on proprietary BLT objects remain LOCAL_ONLY.
- Hosted CI proves source/static/Core/V25 compile contracts only and is not licensed BricsCAD runtime evidence.

## Deterministic validation

Run:

```text
python scripts/preflight-v25-blt-legacy-snapshot-budget.py
```

The auto-discovered guard requires both scan entry points to initialize and route the same budget counter, requires budget estimate/reject/charge ordering before `result.Add(snapshot)`, keeps malformed-object isolation ahead of budget admission, and requires the estimator to account snapshot identity text plus metadata text and structural entry cost.

Repository acceptance additionally requires fresh exact-head Shared CI `preflight` and `core`, latest-main reconciliation, mergeability, expected-head protected merge, and exact-main verification.
