# MCP durable mutation ledger record-limit safety

Issue: #5554  
Lane-Key: `issue-5554`

## Failure mode

The durable mutation acknowledgement ledger is bounded to 1 MiB and 1024 durable records. The historical loader used the record limit in the `for` loop condition, so parsing stopped after the 1024th nonblank record even when more admitted file content remained. Record 1025+ therefore bypassed malformed-record, identity, timestamp and duplicate-action validation.

## Required contract

- A ledger containing exactly `MaxDurableRecords` valid nonblank records is accepted.
- Blank trailing lines do not consume record capacity.
- Every nonblank line in an admitted file is inspected.
- The first nonblank record beyond `MaxDurableRecords` throws `InvalidDataException` before record parsing/store.
- Existing corrupt-ledger recovery clears the partially loaded in-memory dictionary and emits the bounded diagnostic.
- The 1 MiB strict-UTF8 admission, duplicate `actionId` rejection, durable drawing affinity and deterministic persistence ordering remain unchanged.

## Deterministic validation

Run:

```text
python scripts/preflight-mcp-durable-mutation-record-limit.py
```

The preflight is auto-discovered by the aggregate feature-source guard. It pins the load-loop shape so the record cap cannot again become an early loop termination that silently ignores tail state.

## Native/runtime boundary

This carrier changes persisted-ledger parsing only. It does not execute BricsCAD database operations, acquire a document lock, start a native transaction, retry a native command, or alter the process-global writer coordinator.

Remote source/preflight/V25 compile evidence is `REMOTE_SAFE`. A licensed BricsCAD restart/replay exercise is `LOCAL_ONLY`; absence of that runtime is `NO_RESULT`, never a native PASS.
