# Work claim — AuditTrail existing-history persistability

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-audit-existing-history-integrity-20260812-0754`
- Registered: `2026-08-12T07:54:00+07:00`
- Baseline main SHA: `4253d55dc6019a2dedbda4b96228091ada484237`
- Priority: P2 — prevent a new audit mutation from advancing a project whose existing audit history already violates QSDB persistence invariants.

## Confirmed defect

`AuditEvent` entries are publicly mutable through `ProjectState.AuditEvents`. QSDB save requires every audit timestamp to have `DateTimeKind.Utc` and every action to be non-empty/canonical. `AuditTrail.Record(...)` validated the new action and null existing entries, but did not preflight these two persistence invariants on existing non-null events. It could therefore `Touch()` the project and append another event to history that was already unsaveable.

## Implemented fix

- `Record(...)` now preflights all existing entries before revision/history mutation.
- Existing audit timestamps must be UTC.
- Existing audit actions must be non-empty and have no surrounding whitespace.
- New-action trimming, null-history fail-closed behavior, snapshot cloning/read behavior, and `Clear()` as a repair path remain unchanged.
- Focused smoke pins no `ChangeVersion`/count mutation on malformed history plus one-revision append and normalized new action on canonical history.

## Reserved surfaces

- `src/QS3D.Core/Audit/AuditTrail.cs`
- `tests/QS3D.Core.SmokeTests/AuditExistingHistoryIntegritySmoke.cs`
- this claim file

## Integration evidence

- Claim registration: `858ba085cda8b3e8b67e64b86001f17c6f675258`.
- Branch source commit: `a7c8781b7cfe0c5e5026ffb48d4ae5bf9a47e428`.
- Branch smoke commit: `1f8fd5775cf1b91a244d96736beba99be0078028`.
- Branch diff was exactly the reserved source plus new smoke (+15/-3 source lines).
- Comparison from claim registration to then-current `main` `b67770612cdb9cac4e3cc294d3198bb7e9bec6b0` showed 9 intervening commits and no modification of either reserved path.
- PR `#631` squash-merged cleanly at `956314f16989b9849bdb6cfb7acd73995218231a`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no BricsCAD V25 runtime PASS is claimed.
