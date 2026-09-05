# MCP Durable Mutation ACK Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add bounded mutation retry identity and monotonic accepted/applied/durable acknowledgement so a disconnected caller can safely retry a CAD mutation.

**Architecture:** Add a process-global `McpMutationAckLedger` beside the existing single-writer coordinator. `McpCadAgentRuntime.Mutation` reserves/replays identities before writer entry, synchronous success marks applied, native command terminal success marks async operations applied, and verified save completion promotes matching applied records to durable.

**Tech Stack:** C#/.NET BricsCAD V25 host APIs, existing `McpTopLevelJson`, SHA-256, Python source preflight guards, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-03-mcp-durable-mutation-ack-design.md`

## Global Constraints

- `actionId` maximum length: 128 characters.
- Same `actionId` and same semantic request replays without executing again.
- Same `actionId` with different semantic request fails before writer entry.
- State is monotonic `accepted -> applied -> durable`.
- Native command queue acceptance is not `applied`; only matching terminal success can mark it applied.
- Durability requires successful save/SaveAs plus clean persistent-content DBMOD for the same live document generation.
- Restore durable records only after restart.
- Durable storage bounds: 1024 records, 1 MiB file, 16 KiB stored-result bound.
- Existing Reservation v2, writer lease, emergency-stop and native-command barriers remain unchanged.
- Hosted CI is source/compile evidence only; licensed BricsCAD runtime is separate.

---

### Task 1: Focused RED guard

**Files:**
- Create: `scripts/preflight-mcp-durable-mutation-ack.py`

- [ ] Write a guard that requires the ledger, read-only `cad_mutation_status`, reserve/replay-before-writer ordering, applied-success transition, clean-save durable promotion and native terminal ACK hook.
- [ ] Run `python scripts/preflight-mcp-durable-mutation-ack.py`; expect FAIL because production integration is absent.
- [ ] Commit `test: add failing durable mutation ack preflight`.

### Task 2: Ledger core

**Files:**
- Create: `src/QS3D.BricsCAD.V25/McpMutationAckLedger.cs`
- Modify if required: `src/QS3D.BricsCAD.V25/McpTopLevelJson.cs`

**Produces:** `ReserveOrReplay`, `MarkApplied`, native terminal state hooks, `PromoteDurableForDocument`, `StatusJson`, `ResetForServerStart`, `ResetVolatile`.

- [ ] Extend the guard for action ID validation, semantic fingerprint and bounded persistence; verify RED.
- [ ] Implement action ID validation/generation and canonical semantic fingerprint excluding retry/transport-only fields.
- [ ] Implement synchronized accepted/applied/durable records and fail-closed mismatched reuse.
- [ ] Persist only durable records under the existing QS3D per-user application-data root with deterministic eviction and bounded file/result sizes.
- [ ] Run focused guard; commit `feat: add bounded mutation ack ledger`.

### Task 3: Runtime routing and replay

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs`
- Modify the existing descriptor source only where required to publish `cad_mutation_status` and optional `actionId`.

- [ ] Extend guard for status routing and reserve/replay ordering; verify RED.
- [ ] Route `cad_mutation_status` as read-only.
- [ ] Change `Mutation(...)` to confirm -> reserve/replay -> writer -> execute -> mark synchronous success.
- [ ] Return ACK metadata plus the original structured result; replay must return before `EnterMutation`.
- [ ] Reset volatile state on emergency reset while durable state remains recoverable.
- [ ] Run focused guard; commit `feat: integrate mutation retry identity`.

### Task 4: Native command terminal ACK

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs`
- Modify runtime bridge only as required to associate current action identity with the pending native command.

- [ ] Extend guard so queue-time success cannot be treated as applied; verify RED.
- [ ] Carry current ACK identity into the pending native reservation without altering writer ownership.
- [ ] Matching `CommandEnded` marks applied for the same live document generation; cancel/fail never marks applied.
- [ ] Run focused guard; commit `feat: ack native command terminal success`.

### Task 5: Save-backed durable promotion

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs`

- [ ] Extend guard for promotion after clean DBMOD; verify RED.
- [ ] After native QSAVE success, promote matching applied records and persist durable state.
- [ ] After SaveAs target verification and clean DBMOD, promote the same live document generation using post-save document identity.
- [ ] Add bounded promotion evidence to save response; persistence failure must not be reported as durable success.
- [ ] Run focused guard; commit `feat: promote mutation acks after verified save`.

### Task 6: Exact-head verification and protected merge

- [ ] Run focused guard and aggregate discovered feature guards; all must PASS.
- [ ] Run V25 compile against trusted locked BricsCAD references; expect compile PASS only.
- [ ] Create/update the PR for #5456 and inspect exact-head required checks.
- [ ] Fix any genuine regression with a new RED assertion before production changes.
- [ ] Merge only when exact current head required checks succeed and the PR is mergeable.
- [ ] Verify `main` contains exact carrier ancestry and record branch, commits, PR, CI, merge commit and final main SHA on issue #5456.
