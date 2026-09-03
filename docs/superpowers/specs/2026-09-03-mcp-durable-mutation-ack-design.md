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

## Drawing identity

Each applied record is bound to the active drawing identity captured at mutation success. Use the strongest stable identity already available in the V25 runtime, preferring database fingerprint/project identity plus normalized rooted filename where available. Never use transient ObjectId/managed wrapper identity as durable drawing identity.

Save promotion is restricted to records whose stored drawing identity matches the drawing proved clean by the completed save operation. SaveAs must promote against the post-save published drawing identity and must not accidentally promote records belonging to another open document.

If the runtime cannot establish a stable drawing identity, the mutation may be `applied` but must not be promoted to `durable`.

## Ledger architecture

Introduce one narrow V25 component, `McpMutationAckLedger`, responsible for:

- validating/generating `actionId`;
- computing/comparing semantic request fingerprints;
- atomically reserving identities before mutation execution;
- transitioning records monotonically `accepted -> applied -> durable`;
- replaying prior successful results without executing the action;
- answering read-only status queries;
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
- store only bounded metadata needed for replay/status: action ID, semantic fingerprint, tool, stable drawing identity, durable timestamp, and bounded successful result;
- do not persist bearer tokens, writer tokens, arbitrary headers, raw request bodies, or secrets.

Only `durable` entries are serialized. `accepted`/`applied` records remain in memory and disappear on restart because unsaved CAD state cannot be inferred durable after a process boundary.

## Mutation lifecycle

1. `Mutation(...)` validates `confirmMutation` as today.
2. Resolve/generate `actionId` and semantic fingerprint.
3. Ask ACK ledger to reserve or replay before entering the writer gate.
4. If replay: return the stored acknowledgement/result immediately.
5. If new reservation: enter existing `McpCadMutationCoordinator.EnterMutation(...)`.
6. Execute the mutation body under the existing automation epoch checks.
7. At the existing synchronous success boundary, capture drawing identity and mark the record `applied`, then return the wrapped acknowledgement.
8. On pre-application failure, abandon/terminalize the reservation without claiming `applied`.

For asynchronous native command mutations, `applied` must be coupled to the existing native-command terminal success signal rather than mere queue acceptance. If the current call surface cannot safely observe that terminal result, that mutation class remains `accepted` until the coordinator exposes the matching success transition; it must never be promoted directly to durable.

## Save promotion

`cad_save` and `cad_save_as` retain their existing host-owned save behavior and DBMOD completion checks. After those checks prove persistent-content DBMOD clean:

1. capture the verified drawing identity;
2. promote all in-memory `applied` records for that same drawing identity to `durable`;
3. persist the bounded durable ledger;
4. return the save action acknowledgement plus promotion evidence such as `durablePromotedCount`.

The save mutation's own action record becomes durable only after its save success boundary is proven.

No DBMOD sample before save completion may promote records.

## Canonical request fingerprint

Prefer a structural/canonical JSON projection over raw request bytes. The fingerprint input must include the tool name and mutation-semantic arguments while excluding retry/transport fields. Ordering/whitespace differences in JSON must not create a different logical fingerprint.

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
- Durable ledger write failure after CAD save: the CAD save remains successful, but the response must not claim durable ACK persistence that was not written. Keep the record at `applied`/volatile and emit bounded diagnostic evidence.
- Corrupt durable ledger on startup: quarantine/ignore it and expose entries as `unknown`; never guess.
- Emergency stop/reset does not erase durable persisted records. It may clear in-flight accepted/applied volatile records.
- A retry of a durable entry after restart returns the stored durable acknowledgement without touching the DWG.

## Source changes anticipated

Exact production/test paths must be reserved before implementation. Expected surfaces are:

- `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs` — mutation wrapper, status routing, ACK response integration.
- `src/QS3D.BricsCAD.V25/McpMutationAckLedger.cs` — new bounded identity/state/persistence component.
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
6. only matching drawing identity is promoted by save;
7. DBMOD/save evidence is required before `durable`;
8. restart restores durable entries only;
9. corrupt/oversized ledger fails closed;
10. bounded record/result eviction behavior;
11. existing writer/native-command/emergency-stop contracts remain enforced;
12. V25 compile against trusted locked BricsCAD references remains green.

Hosted CI proves source contracts and compile only. Licensed BricsCAD runtime remains a separate qualification boundary.

## Acceptance

The source task is complete only when the exact carrier head has required protected CI success, the focused ACK guard is green, V25 compile is green, the PR is mergeable and merged through normal protected-main policy, and `main` is verified to contain that exact carrier ancestry. No force push, ruleset bypass, or false runtime PASS claim.
