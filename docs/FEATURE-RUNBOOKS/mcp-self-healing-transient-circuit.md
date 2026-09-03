# MCP self-healing transient retry circuit

## Purpose

Bound repeated transient self-healing recommendations so a supervising client cannot retry the same persistent CAD/transport blockage indefinitely.

## Contract

- Caller/policy failures remain non-retryable and continue to recommend `correct_call_or_refresh_tools`.
- Transient failures such as timeout, document lock, transport disconnect, BUSY, writer-lease and lock-violation classes may recommend `retry_transient` only before the bounded circuit threshold.
- At occurrence 4 for the same deterministic fingerprint, transient failures set `circuitOpen=true`, `humanReviewRequired=true`, and recommend `human_review`.
- Source-repair failures retain their existing bounded circuit behavior.
- Ticket storage remains bounded to 256 entries with the existing deterministic eviction rules.
- Fingerprint construction is unchanged.

## Remote validation

Run:

```text
python scripts/preflight-mcp-self-healing-transient-circuit.py
```

Shared CI also discovers this preflight automatically and compiles the V25 plugin for build-relevant changes.

## Local-only boundary

This change is source/runtime metadata behavior. Hosted/static validation may prove source topology and compilation only. Do not claim licensed BricsCAD `LOCAL_PASS` unless the exact candidate is exercised in a compatible licensed runtime.
