# MCP durable mutation replay — drawing affinity

Issue: #5532
PR: #5535
Lane: C04

## Failure mode

The durable mutation ACK ledger is process-global and survives process restart. A durable record stores the mutation request fingerprint plus the stable BricsCAD database identity captured at save time. Before this carrier, replay admission used only `actionId` + canonical request fingerprint. The stored drawing identity was not consulted, so an identical retry issued while another drawing was active could return the prior drawing's durable result.

## Contract

- Accepted/applied records remain same-process duplicate-mutation barriers and replay without introducing a second CAD mutation.
- Durable records require active-drawing affinity before replay.
- Active drawing and database fingerprint are read only inside BricsCAD application context.
- Ledger `Sync` is never held across application-context dispatch.
- After the identity read, the same durable record/reference/state and request fingerprint are revalidated under `Sync` before replay is returned.
- Drawing affinity compares the persisted stable database fingerprint. Path is diagnostic metadata and does not independently prove or disprove identity, so SaveAs path drift does not make the same database look like another drawing.
- Missing/unverifiable fingerprint and a different fingerprint fail closed. They never trigger blind mutation retry.
- Replay decision remains before process-global writer admission; no document lock, transaction, DBObject, or native mutation is held while waiting.

## Hosted verification

`scripts/preflight-mcp-durable-mutation-document-affinity.py` is auto-discovered with the feature guards. Existing durable-ACK/source guards and V25 compile remain required on the exact PR head.

Hosted/static/V25 compile evidence is `REMOTE_SAFE` only.

## LOCAL_ONLY BricsCAD qualification

When a licensed BricsCAD V25 session is available, validate on the exact merged/candidate SHA:

1. Mutate drawing A with an explicit actionId, save so the ACK becomes durable, and verify same-drawing retry replays without a second mutation.
2. Restart/reload the process so the durable ledger is loaded, activate drawing A, and verify the same actionId/request replays.
3. Activate drawing B and submit the same actionId/request; verify fail-closed cross-drawing rejection and no native mutation.
4. Exercise SaveAs on the same database and verify path-only drift does not defeat the stable fingerprint comparison.
5. Exercise missing/unavailable fingerprint behavior and verify no replay/no blind retry.
6. Confirm an application-context dispatch timeout cannot cause a late native mutation; this path performs identity reads only.

Do not report these steps as LOCAL_PASS from hosted CI.