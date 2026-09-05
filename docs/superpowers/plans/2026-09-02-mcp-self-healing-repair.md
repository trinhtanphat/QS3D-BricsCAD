# MCP self-healing repair loop implementation plan

**Goal:** Enrich existing MCP `tools/call` failure envelopes with a bounded repair ticket that ChatGPT can use to decide whether to correct its call, repair repository source, retry later, or stop for human review.

**Architecture:** Keep the existing MCP tool surface stable. Add a process-local `McpSelfHealingRepairRuntime` that computes a deterministic SHA-256 fingerprint from normalized failure identity, deduplicates repeated failures, tracks occurrence counts, applies a conservative source-repair eligibility policy, and opens a circuit after repeated identical repairable failures. `McpEmbeddedServerV2.CallTool` records both contract and runtime failures and embeds the returned repair object beside the existing structured error.

**Safety invariants:** Caller/schema/auth/policy/confirmation mistakes are never source-repair candidates. Unknown invented tools are treated as caller/tool-discovery errors rather than proof that source code is missing. No repair path bypasses mutation confirmation, writer lease, branch protection, CI, or runtime evidence requirements. The in-process ticket ledger is bounded.

## Task 1 — Pin behavior with a feature preflight

Create `scripts/preflight-mcp-self-healing-repair.py` first. It must fail while the runtime/integration is absent and assert the stable tool surface, deterministic fingerprinting, dedupe count, conservative eligibility, circuit-breaker threshold, and human-review escalation tokens.

## Task 2 — Implement bounded repair ticket runtime

Create `src/QS3D.BricsCAD.V25/McpSelfHealingRepairRuntime.cs` with:
- deterministic normalized SHA-256 fingerprint;
- `QS3D-REPAIR-<prefix>` ticket id;
- bounded in-memory ticket ledger;
- occurrence count and first/last seen timestamps;
- `sourceRepairEligible`, `circuitOpen`, `humanReviewRequired`, and `recommendedAction`;
- fail-closed caller/policy detection;
- circuit opening after four identical repairable failures.

## Task 3 — Integrate with existing error envelope

Update only `McpEmbeddedServerV2.cs`:
- record `McpToolContractException` as non-repairable contract failures;
- record classified runtime failures with their existing code/lane/message;
- add optional `repair` object to `structuredContent.error` while preserving current text/error fields and MCP tool list.

## Task 4 — Document operational use

Create `docs/FEATURE-RUNBOOKS/mcp-self-healing-repair.md` explaining how ChatGPT should consume `recommendedAction`: correct call locally, retry transient work, open a GitHub repair carrier for eligible failures, or stop when the circuit is open.

## Task 5 — Verify exact head and merge safely

Run the feature preflight through PR CI/aggregate preflight, inspect exact-head status, review the diff, then merge through the repository PR path only when required checks are green. Close issue #5340 after merged `main` is confirmed.
