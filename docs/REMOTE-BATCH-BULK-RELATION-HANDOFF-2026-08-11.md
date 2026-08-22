# Remote batch handoff — bulk semantic relation policy

Updated: 2026-08-11 (UTC+7)

This note records only the execution boundary for the current remote source batch. It is **not** a second local work queue; `docs/LOCAL-AGENT-INBOX.md` remains authoritative.

## REMOTE_DONE source scope

- Generic semantic bulk property editing is routed through one shared `SemanticPropertyEditPolicy` from both `BulkEditService` and `SemanticSelectionBulkEditService`.
- Identity/reference-shaped keys (`Id`, `*Id`, `*Ids`, `*Ref`, `*Refs`, `*RefId`, `*RefIds`), source-derived CAD fields and native/generated ownership fields fail closed before generic mutation.
- `BulkEditAtomicitySmoke` covers `BottomLevelId` and numeric `HostRefId` rejection without dirty/timestamp/change-version mutation.
- `scripts/preflight-bulk-semantic-property-policy.py` statically guards the shared source contract.
- `scripts/preflight-remote-local-handoff-policy.py` guards the repository rule that unavailable remote work is handed to the canonical local inbox and is not retried by equivalent remote agents.

## Execution not claimed by this remote batch

This agent cannot produce local BricsCAD V25/Windows runtime evidence and did not dispatch GitHub Actions. The environment also could not obtain a working Git checkout for local command execution, so this note does **not** claim Core build, smoke, aggregate preflight, V25 build, NETLOAD, private-DWG or native runtime PASS.

Do **not** create a duplicate LOCAL_ONLY item for this batch. `LOCAL-001 — exact V25 build/load baseline` already owns clean exact-SHA Core Release build/Core smoke plus V25 build/load qualification. A compatible local agent should execute that existing item on the newest intended merged SHA; the new auto-discovered preflights are part of the repository gate set at that SHA.

Remote/non-local agents must not retry this execution boundary merely because this note exists. Follow `AGENTS.md`, `docs/REMOTE-AGENT-SCOPE.md` and `docs/LOCAL-AGENT-INBOX.md`; only update the existing local item if later source materially changes its scenario or evidence requirements.
