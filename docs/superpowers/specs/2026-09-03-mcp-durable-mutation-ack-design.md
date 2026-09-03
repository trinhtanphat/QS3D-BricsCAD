# MCP durable mutation acknowledgement and retry identity — design

Issue: #5456  
Lane-Key: `issue-5456`  
Ownership-Key: `v25.mcp.durable-mutation-ack-v1`  
Baseline main: `fb1faa3967790f169b9400463d31aba9e26b1a61`

## Problem

Direct CAD mutation calls currently enter the single-writer lane, execute the mutation, and return the tool result. If the HTTP/MCP transport disconnects after the CAD mutation commits but before the caller receives the response, the caller cannot distinguish an unknown success from a failed attempt. Blind retry can therefore duplicate geometry or another side effect.

The current writer lease/native-command coordinator protects concurrent mutation ordering, but it does not provide request identity, replay suppression, or durable acknowledgement across process restart.

## Goals

1. Every mutation can carry a bounded caller-supplied `actionId` that identifies one logical mutation attempt.
2. Repeating the same `actionId` for the same logical request never executes that mutation twice.
3. Reusing an `actionId` with a different mutation request fails closed.
4. Responses expose a monotonic acknowledgement state: `accepted`, `applied`, or `durable`.
5. A successful save/SaveAs plus clean persistent-content DBMOD promotes already-applied mutations for that drawing to `durable`.
6. Durable acknowledgement survives MCP/server restart through a bounded external ledger.
7. A read-only status tool lets a caller query an `actionId` without resubmitting the mutation.
8. Existing single-writer, emergency-stop, native-command barrier, and save safety contracts remain intact.

## Non-goals

- Exactly-once semantics across arbitrary external side effects outside the bounded CAD mutation runtime.
- Persisting retry metadata inside the DWG itself.
- Treating hosted CI or source checks as licensed BricsCAD runtime qualification.
- Making an asynchronous native command `applied` before its matching BricsCAD terminal success is known.
- Automatically retrying a mutation under a new identity.

## Public contract

### Mutation request identity

Mutation-capable tools accept optional `actionId`.

- `actionId` is trimmed, ASCII-safe, bounded to 128 characters, and rejects control characters/path separators.
- If absent, the runtime generates a random action ID and returns it. That generated value is retry-safe only if the caller retains and reuses it.
- Identity is scoped by a stable request fingerprint containing the mutation tool and canonical mutation arguments. Transport/session-only fields such as `actionId`, `writerToken`, `confirmMutation`, and execution-mode aliases are excluded from the semantic fingerprint.
- Same `actionId` + same fingerprint: return the existing acknowledgement/result without re-entering the mutation action.
- Same `actionId` + different fingerprint: reject before CAD mutation with a stable fail-closed error.

### Acknowledgement response

Successful mutation responses are wrapped with acknowledgement metadata while preserving the original tool result as structured JSON where possible:

```json
{
  "actionId": "...",
  "ackState": "applied",
  "accepted": true,
  "applied": true,
  "durable": false,
  "replayed": false,
  "result": { }
}
```

A replay sets `replayed=true` and does not invoke the mutation body again.

`accepted` means the identity has been admitted and reserved before the mutation body begins. `applied` means the bounded CAD mutation reached its success boundary. `durable` means save evidence has subsequently proved persistent-content DBMOD clean for the same drawing generation.

Errors before the mutation body do not publish `applied`. An admitted record that fails before application is removed or terminally failed so that it cannot masquerade as an applied mutation.

### Status query

Add read-only `cad_mutation_status` with required `actionId`. It returns one of:

- `unknown` — no current/durable record exists;
- `accepted` — admitted but not yet applied;
- `applied` — mutation succeeded but is not yet save-backed;
- `durable` — mutation succeeded and later save evidence promoted it.

The status query never enters the writer gate and never mutates the DWG.

## Drawing identity and SaveAs

Two scopes are intentionally separated.

**Volatile application scope:** while BricsCAD is running, newly accepted/applied records are bound to the exact active native `Document/Database` lifetime through a private process-local document token. That token may use managed reference identity internally because it is never persisted and is used only to prevent one open document from promoting another document's records.

**Durable drawing scope:** after a save has completed and persistent DBMOD is clean, the ledger captures a stable drawing fingerprint from the saved database plus the normalized rooted filename as diagnostic metadata. The implementation must use the strongest stable V25 database fingerprint exposed by the trusted BricsCAD/Teigha references. If no stable persisted database fingerprint is available, the runtime must not promote records to `durable`; it may only report `applied` and a bounded diagnostic explaining why durability could not be proven.

Save promotion uses the process-local document token, not the pre-save filename. This is critical for SaveAs: records applied to the active document before SaveAs remain attached to that same document lifetime even though its published path changes. After successful SaveAs, the durable record stores the post-save stable fingerprint/path. No record from another open document can be promoted by that save.

A durable replay after restart is keyed primarily by `actionId` + semantic request fingerprint. The stored durable drawing fingerprint is retained as provenance and is returned by status/replay. A mismatched semantic request still fails closed. The server never re-executes a durable action merely because the currently active document differs; it returns the historical durable acknowledgement for that action identity instead of mutating the current DWG.

## Ledger architecture

Introduce one narrow V25 component, `McpMutationAckLedger`, responsible for:

- validating/generating `actionId`;
- computing/comparing semantic request fingerprints;
- atomically reserving identities before mutation execution;
- transitioning records monotonically `accepted -> applied -> durable`;
- replaying prior successful results without executing the action;
- answering read-only status queries;
- binding volatile records to one active document lifetime;
- promoting matching applied records after verified save completion;
- loading only durable records on server start;
- writing only durable records to an external bounded ledger.

The ledger is process-global and separately synchronized. It does not replace `McpCadMutationCoordinator`; the existing coordinator continues to own DWG single-writer/native-command safety.

## Persistence

Durable records are stored under the existing QS3D per-user application-data root, not inside project/DWG files.

Requirements:

- strict UTF-8;
- one bounded file (target maximum 1 MiB) with a bounded record count;
- deterministic newest-first/oldest-eviction policy;
- atomic temp-file replacement where supported;
- corruption or oversize fails closed to an empty in-memory durable cache and records a bounded diagnostic; it must never block ordinary CAD startup or infer durability from malformed bytes;
- store only bounded metadata needed for replay/status: action ID, semantic fingerprint, tool, durable drawing fingerprint/path metadata, durable timestamp, and bounded successful result;
- do not persist bearer tokens, writer tokens, arbitrary headers, raw request bodies, or secrets.

Only `durable` entries are serialized. `accepted`/`applied` records remain in memory and disappear on restart because unsaved CAD state cannot be inferred durable after a process boundary.

## Mutation lifecycle

1. `Mutation(...)` validates `confirmMutation` as today.
2. Resolve/generate `actionId` and semantic fingerprint.
3. Ask ACK ledger to reserve or replay before entering the writer gate.
4. If replay: return the stored acknowledgement/result immediately.
5. If new reservation: enter existing `McpCadMutationCoordinator.EnterMutation(...)`.
6. Bind the reservation to the exact current document lifetime and execute the mutation body under the existing automation epoch checks.
7. For a synchronous CAD mutation, capture the existing success result and mark the record `applied`, then return the wrapped acknowledgement.
8. On pre-application failure, abandon/terminalize the reservation without claiming `applied`.

### Asynchronous native-command boundary

Queue acceptance is **not** application success. For mutation surfaces that dispatch a BricsCAD native command asynchronously, the ACK record remains `accepted` after enqueue. The existing native-command barrier must carry the `actionId`/ACK completion hook and perform exactly one terminal transition:

- matching `CommandEnded` => mark the corresponding ACK `applied` with the same document token;
- matching `CommandCancelled` or `CommandFailed` => mark/abandon it as terminal non-applied;
- no matching terminal event => never claim `applied` or `durable`.

This requires a narrow integration point in `McpCadMutationCoordinator`; it must not weaken its current process-global writer ownership or terminal-event matching. A retry while the same action is still `accepted` returns status/acknowledgement and never queues the native command a second time.

## Save promotion

`cad_save` and `cad_save_as` retain their existing host-owned save behavior and DBMOD completion checks. After those checks prove persistent-content DBMOD clean:

1. capture the exact current process-local document token;
2. capture the post-save stable database fingerprint and normalized rooted filename;
3. promote all in-memory `applied` records bound to that same document token to `durable`;
4. persist the bounded durable ledger atomically;
5. return the save action acknowledgement plus promotion evidence such as `durablePromotedCount`.

The save mutation's own action record becomes durable only after its save success boundary is proven and the durable ledger write succeeds.

No DBMOD sample before save completion may promote records. A durable ledger write failure must not retroactively make the CAD save fail, but the response must keep affected records non-durable/volatile and report bounded persistence failure evidence.

## Canonical request fingerprint

Use a structural/canonical JSON projection rather than raw request bytes. The fingerprint input must include the tool name and mutation-semantic arguments while excluding retry/transport fields. Ordering/whitespace differences in JSON must not create a different logical fingerprint.

If the repository's existing JSON helper cannot safely produce such a projection without a broad parser change, add one narrow bounded canonicalization helper for top-level MCP arguments rather than hashing the raw body. Unknown fields remain part of the fingerprint so an `actionId` cannot be silently reused for a changed request.

## Bounds and security

- Max action ID length: 128 characters.
- Max stored result per durable record: 16 KiB; larger successful results may be represented by a stable result digest plus a bounded replay summary, but must never cause duplicate execution on retry.
- Max durable records: 1024, also constrained by the 1 MiB file cap.
- SHA-256 is sufficient for request/result digests; fingerprints are integrity identities, not credentials.
- Constant-time comparison is not required for non-secret fingerprints, but comparisons must be ordinal and deterministic.
- No shell/process execution and no filesystem path derived from `actionId`.

## Failure semantics

- Duplicate identity with different fingerprint: stable fail-closed error, no writer-gate entry.
- Durable ledger write failure after CAD save: the CAD save remains successful, but the response must not claim durable ACK persistence that was not written. Keep affected records at `applied`/volatile and emit bounded diagnostic evidence.
- Corrupt durable ledger on startup: ignore/quarantine it and expose entries as `unknown`; never guess.
- Emergency stop/reset does not erase durable persisted records. It may clear in-flight accepted/applied volatile records only after existing native-command ownership safety permits release; a dispatching native command is never forgotten merely to clear ACK state.
- A retry of a durable entry after restart returns the stored durable acknowledgement without touching the DWG.

## Source changes anticipated

Exact production/test paths must be reserved before implementation. Expected surfaces are:

- `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — mutation wrapper, status routing, ACK response integration.
- `src/QS3D.BricsCAD.V25/McpMutationAckLedger.cs` — new bounded identity/state/persistence component.
- `src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs` — narrow async native-command ACK terminal hook only; preserve writer/barrier semantics.
- `src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs` and/or the existing save helper only where needed to expose verified save/drawing evidence; do not duplicate save logic.
- `scripts/preflight-mcp-durable-mutation-ack.py` — focused auto-discovered source contract/regression.
- existing aggregate/direct guards only if their current contract conflicts with this feature; reservation must be expanded before touching them.

## Test strategy

TDD begins with a focused failing preflight/regression proving the current code lacks the contract. The test must cover:

1. action ID validation/generation and bounded storage;
2. same-ID/same-request replay without a second action invocation;
3. same-ID/different-request rejection before mutation;
4. monotonic `accepted -> applied -> durable` transitions;
5. status query is read-only;
6. process-local document scoping prevents cross-document promotion;
7. SaveAs promotes the same document lifetime and stores post-save identity;
8. DBMOD/save evidence and stable post-save fingerprint are required before `durable`;
9. async native queue remains `accepted` until matching terminal success and is never queued twice on retry;
10. restart restores durable entries only;
11. corrupt/oversized ledger fails closed;
12. bounded record/result eviction behavior;
13. existing writer/native-command/emergency-stop contracts remain enforced;
14. V25 compile against trusted locked BricsCAD references remains green.

Hosted CI proves source contracts and compile only. Licensed BricsCAD runtime remains a separate qualification boundary.

## Acceptance

The source task is complete only when the exact carrier head has required protected CI success, the focused ACK guard is green, V25 compile is green, the PR is mergeable and merged through normal protected-main policy, and `main` is verified to contain that exact carrier ancestry. No force push, ruleset bypass, or false runtime PASS claim.
