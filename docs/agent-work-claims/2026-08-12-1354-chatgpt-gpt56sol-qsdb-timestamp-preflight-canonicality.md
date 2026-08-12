# Work claim — QSDB canonical UTC timestamp preflight sync

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T13:54:20+07:00`
- Baseline main SHA: `3c6bf9bdafd0651e372c41e89a01cc4396674889`
- Priority: `P0 source/static CI regression — timestamp smoke/parser now enforce exact canonical UTC while the dedicated preflight still checks the superseded normalize-offset contract`

## Reserved scope

Reconcile `scripts/preflight-qsdb-timestamp-offset.py` with the current QSDB timestamp persistence contract: exact canonical `O`-format UTC (`Z`) timestamps are accepted; explicit non-UTC offsets and missing offsets fail closed. This lane changes only the static guard, not persistence behavior.

## Expected surfaces

- `scripts/preflight-qsdb-timestamp-offset.py`
- Read-only verification of `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- Read-only verification of `tests/QS3D.Core.SmokeTests/QsdbTimestampOffsetSmoke.cs`

## Excluded scope

- Production QSDB parser/serializer semantics.
- `scripts/preflight-qsdb-schema.py` and other QSDB/static gates.
- LOCAL-003 fixture/runtime work, project-id canonicality, relation-token, save-size, XML-text, or other Persistence lanes.
- GitHub Actions dispatch, BricsCAD runtime qualification, packaging/release.

## Validation plan

- Confirm production `Date(...)` canonicalizes parsed timestamps to UTC text and rejects any input whose original text is not that canonical representation.
- Confirm smoke explicitly rejects `+07:00`, rejects missing offset, and accepts exact canonical `...Z` round-trip.
- Update only the dedicated timestamp-offset preflight tokens/message to guard that current contract.
- Read back the pushed diff and preserve all unrelated source/test behavior.
- Do not claim executable preflight/build/Actions/runtime PASS unless actually executed.

## Coordination

This is a narrow static-script reconciliation after PR #904 updated the timestamp fixtures while explicitly leaving the timestamp/schema preflights stale. It does not modify current Formula, Grid, Preview Review, XLSX, handle-identity, quantity, EntitySnapshot, interchange, or other active source lanes.

## Completion condition

A pushed `main` commit makes the dedicated timestamp preflight require current canonical-UTC parser and regression tokens, then this claim is marked `COMPLETED` with exact implementation SHA and validation actually performed.
