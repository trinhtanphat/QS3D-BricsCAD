# Work claim — Release #37 QSDB changeVersion gate reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release37-qsdb-changeversion-gate-20260812-1535`
- Registered: `2026-08-12T15:35:00+07:00`
- Baseline main SHA: `9a3f6b5fee70d79233c01237d8e8cb783a1f52a1`
- Priority: P1 release preflight / stale schema regression token

## Confirmed mismatch

Current schema-3 QSDB requires explicit `changeVersion`; missing current-schema persistence state fails closed. Legacy v1/v2 migration synthesizes the required field before strict schema validation. This hardening landed in `a10060088aad60e46c9e8ed812e7ca0eef15d042`.

`QsdbSaveAtomicitySmoke` now runs `MissingCurrentChangeVersionIsRejected()` in addition to round-trip and malformed-value rejection. Release #37 `preflight-qsdb-change-version.py` still expected obsolete `LegacyFileDefaultsChangeVersion`, causing a stale gate failure.

## Integrated reconciliation

- Claim: `4a1aa0c6877fcae7eaad1913cf70274c3c532ced`
- Gate fix: `13f3ffb4d078af188bd92cc0cbac3cf6ea8a7830`

The gate now pins `MissingCurrentChangeVersionIsRejected` for strict current-schema files while preserving serializer/load parse boundaries, the legacy migration zero default, canonical non-negative integer validation, malformed/overflow rejection as `InvalidDataException`, and backup fallback classification.

## Limitations

- QSDB production and smoke source were not changed by this lane.
- GitHub Actions were not rerun or dispatched.
- No aggregate preflight/build/package/release/runtime PASS is claimed.
