# Work claim — Release #37 QSDB changeVersion gate reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release37-qsdb-changeversion-gate-20260812-1535`
- Registered: `2026-08-12T15:35:00+07:00`
- Baseline main SHA: `9a3f6b5fee70d79233c01237d8e8cb783a1f52a1`
- Priority: P1 release preflight / stale schema regression token

## Confirmed mismatch

Current schema-3 QSDB requires explicit `changeVersion`; missing current-schema persistence state fails closed. Legacy v1/v2 migration synthesizes the required field before strict schema validation. This hardening landed in `a10060088aad60e46c9e8ed812e7ca0eef15d042`.

`QsdbSaveAtomicitySmoke` now runs `MissingCurrentChangeVersionIsRejected()` in addition to round-trip and malformed-value rejection. Release #37 `preflight-qsdb-change-version.py` still expects obsolete `LegacyFileDefaultsChangeVersion`, causing a stale gate failure.

## Reserved scope

- `scripts/preflight-qsdb-change-version.py`
- this claim file

## Expected reconciliation

Update the gate to pin strict current-schema missing-changeVersion rejection while preserving serialized changeVersion, parse canonicality, non-negative domain guards, malformed/overflow rejection and backup-fallback classification.

## Excluded scope

- no QSDB production/source changes;
- no smoke behavior changes;
- no Actions rerun/dispatch;
- no runtime qualification claim.

## Completion condition

Gate is integrated/read back and claim closed with exact SHA evidence.
