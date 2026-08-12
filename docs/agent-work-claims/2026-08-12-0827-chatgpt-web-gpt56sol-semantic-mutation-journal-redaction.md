# Work claim — semantic mutation journal exception redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-mutation-journal-redaction-20260812-0827`
- Registered: `2026-08-12T08:27:00+07:00`
- Baseline main SHA: `06a53250217420da17e004c45d29434ceb5636b7`
- Priority: P1 — prevent detached/public-readable diagnostic journal entries from exposing raw exception messages.

## Reserved scope

`ProjectSemanticMutationExecutor.SafeDetail(...)` currently records `ExceptionType: Exception.Message` for rollback-start/rollback-failure phases. `ProjectSemanticMutationJournal.Entries` is public-readable, and exception messages may contain local paths, URI/query data, credentials/tokens, or project-specific payload. The journal should retain deterministic phase/type evidence without copying arbitrary raw exception text.

## Reserved surfaces

- `src/QS3D.Core/Services/ProjectSemanticMutationExecutor.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSemanticMutationJournalRedactionSmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- Replace raw exception-message journaling with a bounded generic detail that identifies the exception type only.
- Preserve the original thrown exception/aggregate exception behavior; caller exception semantics and stack/inner exceptions remain unchanged outside the journal.
- Preserve phase ordering, rollback behavior, journal saturation best-effort semantics, operation-name validation and journal capacity.
- Add focused smoke proving a secret/path marker in a thrown exception is absent from journal entries while the exception type and rollback phases remain visible.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.
