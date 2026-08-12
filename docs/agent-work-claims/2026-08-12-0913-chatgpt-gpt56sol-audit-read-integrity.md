# Work claim — AuditTrail read-side integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-audit-read-integrity-20260812-0913`
- Registered: `2026-08-12T09:13:00+07:00`
- Baseline main SHA: `13219765d9940c9ede67cdc554cd24f6216bd04e`
- Priority: owner-requested continue-all Core integrity hardening

## Confirmed defect

`AuditTrail.Record(...)` already treats non-UTC timestamps and non-canonical/blank audit actions in existing history as invalid because those entries violate the persisted audit contract. `AuditTrail.Events`, however, only rejects a null event and then clones strings with `?? string.Empty`. A malformed persisted/publicly-mutated event can therefore be read as an ordinary snapshot (including a null action silently becoming an empty action), creating a read-side false-normalization path inconsistent with the class's own existing-history validation.

## Reserved scope

- `src/QS3D.Core/Audit/AuditTrail.cs`
- one focused Core smoke test proving malformed read-side history fails closed while canonical history remains cloned/read-only
- this claim file

## Intended fix

- Reuse one shared audit-event integrity validator for both `Events` reads and `Record(...)` existing-history preflight.
- Keep `Clear()` as the repair path.
- Preserve clone isolation/read-only outer collection and all valid `Record(...)` behavior.
- Do not add stricter rules for optional `ElementId`, `Detail`, `Actor`, or `CorrelationId` fields in this lane.

## Validation boundary

Deterministic source/smoke coverage and final GitHub readback only. No GitHub Actions/build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim.
