# Agent work claim — Release #34 Revision timestamp gate

- Status: `COMPLETED`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:48 Asia/Ho_Chi_Minh`
- Completed: `2026-08-12 14:50 Asia/Ho_Chi_Minh`

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

## Implementation

- claim: `a34fe39c1e63a6c4bc29286534700b677e5075bd`
- gate reconciliation: `1d6af4f32ad088c8aa3347e68cb9e1495013794f`
- existing production timestamp hardening: `4b153b6e82087ad41754cbc94ff79a25544b4cd4`
- existing timestamp regression: `0845bb05edd14f09db8fa0cd51894bbe2890585b`

## Evidence & limitations

Remote readback confirms the gate now pins exact invariant UTC round-trip parsing/text equality and the existing smoke rejects equivalent offset text, zero-offset text, missing-offset text and short-form UTC while accepting the serializer's canonical seven-digit `Z` representation. Production Revision store/smoke code was not changed in this lane. No GitHub Actions or licensed BricsCAD runtime was executed.
