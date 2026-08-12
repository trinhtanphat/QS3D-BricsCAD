# Work claim — Comprehensive health collision-free issue identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-comprehensive-health-key-collision-20260812`
- Registered: `2026-08-12T11:18:00+07:00`
- Completed: `2026-08-12T11:20:00+07:00`
- Baseline main SHA: `f11e000bc4760fb16c7a9e3935427b9ca71666df`
- Integration SHA: `1f2024560caf1757e2184e8b9c85abe3569ff6a9` (PR #812)
- Task Key: `CORE-COMPREHENSIVE-HEALTH-KEY-COLLISION`

## Defect

`ComprehensiveModelHealthService.Add(...)` de-duplicated provider issues with a newline-delimited string built from severity, upper-cased code, upper-cased element id and ordinary message. `ModelHealthIssue` permits embedded newlines, so distinct provider issues could produce the same identity and one could be silently dropped before callers or baseline capture saw it.

## Completed repair

- issue identity is encoded with invariant length-prefixing rather than delimiter concatenation;
- severity sensitivity is preserved;
- code and element-id identity remain case-insensitive;
- ordinary message identity remains exact;
- existing `*_STALE` message-insensitive de-duplication remains intact;
- focused Core smoke pins a concrete newline collision and the stale-message non-regression.

## Validation boundary

PR #812 was inspected as exactly two changed files and squash-merged to `main` at `1f2024560caf1757e2184e8b9c85abe3569ff6a9`.

No GitHub Actions/full build/executable smoke/BricsCAD V25/V26 runtime PASS was claimed or executed.
