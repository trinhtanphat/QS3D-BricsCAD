# Work claim — semantic mutation journal exception redaction

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-semantic-mutation-journal-redaction-20260812-0827`
- Registered: `2026-08-12T08:27:00+07:00`
- Baseline main SHA: `06a53250217420da17e004c45d29434ceb5636b7`
- Priority: P1 — prevent detached/public-readable diagnostic journal entries from exposing raw exception messages.

## Confirmed defect

`ProjectSemanticMutationExecutor.SafeDetail(...)` copied `Exception.Message` into `ProjectSemanticMutationJournalEntry.Detail`. Because `ProjectSemanticMutationJournal.Entries` is public-readable, arbitrary exception text such as local paths or project-specific payload could escape into detached diagnostics.

## Implemented fix

- Rolling-back/rollback-failed journal detail now keeps bounded exception-type evidence only (`<Type> occurred.`), without copying raw exception messages.
- The original thrown exception and aggregate rollback-failure behavior remain unchanged for callers.
- Phase ordering, rollback semantics, journal saturation best-effort behavior, operation-name validation and capacity remain unchanged.
- Focused smoke proves rollback still restores state, journal retains `InvalidOperationException` and rollback phases, a private path marker is absent from every detail, and the caller still receives the original exception message.

## Integration evidence

- Claim registration: `64c76bf6dc251cf83f67366966a73a43504154a2`.
- Branch source commit: `356f9bb24ac138e18b840e33b1c09350a5d00bb3`.
- Branch smoke commit: `640b5560f439432d975219140f3777897458e4d8`.
- Branch diff was exactly the reserved executor plus new focused smoke (+3/-3 source lines).
- Comparison from claim registration to then-current `main` `3b10e48123bb07db09cc13eef309ea96daa5e35a` showed 11 intervening commits and no modification of either reserved path.
- PR `#650` squash-merged cleanly at `166dc81a98e3f4cd3469e03ea72c7d1c5a2d5b1f`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.
