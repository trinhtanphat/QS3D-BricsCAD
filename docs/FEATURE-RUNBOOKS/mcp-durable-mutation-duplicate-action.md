# MCP durable mutation ledger — duplicate action identity

Issue: #5548
Lane: C04

## Failure mode

The durable mutation ACK ledger uses `actionId` as the process/restart idempotency identity. Before this carrier, `LoadDurableLocked()` decoded persisted records and assigned them with dictionary indexer replacement. If a corrupted or externally modified ledger contained the same persisted `actionId` twice, the later line silently replaced the earlier request fingerprint, drawing provenance, result, and durable timestamp. Replay meaning therefore depended on file order instead of a unique persisted identity.

## Required contract

- Persisted `actionId` values are unique within one durable ledger generation.
- A duplicate persisted `actionId` is corruption and must be rejected before the later record is stored.
- Any load corruption clears all partially loaded records through the existing fail-closed recovery path; no partial durable state survives.
- Duplicate rejection must not trigger a native CAD retry or mutation.
- Existing 1 MiB, 1024-record, strict UTF-8, field-count, timestamp, fingerprint and stable-document-identity bounds remain intact.
- Same-process reservation/replay and process-global single-writer coordination are unchanged.

## Hosted verification

`scripts/preflight-mcp-durable-mutation-duplicate-action.py` is auto-discovered by feature source guards and pins duplicate detection before dictionary storage plus existing clear-on-corruption/bounded-load behavior. Exact-head Shared CI and V25 compile remain required.

Hosted/static/V25 compile evidence is `REMOTE_SAFE` only.

## LOCAL_ONLY qualification

When a licensed BricsCAD V25 session is available on the exact candidate/merged SHA:

1. Produce at least one durable mutation ACK and stop the MCP/BricsCAD process cleanly.
2. Duplicate its persisted ledger line with the same `actionId` but altered fingerprint/result/provenance fields.
3. Restart and verify the ledger is rejected as corrupt and no durable action is replayable from the partial load.
4. Verify no native mutation is issued merely because ledger recovery rejected the file.
5. Restore a valid single-record ledger and verify normal same-drawing durable replay still works.

Do not report these steps as LOCAL_PASS from hosted CI.
