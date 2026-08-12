# Agent work claim — Release #34 Revision timestamp gate

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:48 Asia/Ho_Chi_Minh`

## Scope

Reconcile `preflight-revision-store-integrity.py` with the completed Revision timestamp-canonicality contract already present in production. Revision snapshots require the exact invariant UTC round-trip (`O`) representation emitted by the serializer; offset-equivalent, short-form UTC, padded, local and unspecified timestamp text must fail closed rather than being silently normalized.

## Files

- `scripts/preflight-revision-store-integrity.py`
- this claim file

## Out of scope

- production `RevisionSnapshotStore.cs`
- Revision smoke fixtures owned by other agents
- QSDB timestamp policy
- release/updater/signing/runtime qualification

## Acceptance checks

- gate requires `DateTime.TryParseExact(..., "O", ..., DateTimeStyles.RoundtripKind, ...)`;
- gate requires `DateTimeKind.Utc` and exact round-trip text equality;
- gate requires smoke coverage that offset-equivalent and short-form UTC timestamps fail closed, while canonical UTC remains UTC;
- file-size, atomic publish, XML/schema and cleanup assertions remain intact.
