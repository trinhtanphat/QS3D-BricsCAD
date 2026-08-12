# Work claim — ProjectState Name persistence freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-name-freshness-20260812-0948`
- Registered: `2026-08-12T09:48:00+07:00`
- Baseline main SHA: `0773c70848f5bf5bdd48123e6031dd21d1c03454`
- Priority: P1 — ensure public Project Name mutation participates in ChangeVersion/persistence dirty tracking.

## Confirmed defect

`ProjectState.Name` is publicly mutable and QSDB persists it, while `ProjectPersistenceStamp.RequiresSave(...)` relies on `ProjectState.ChangeVersion`. The setter previously validated/trimmed and assigned `_name` without touching project freshness, so serialized content could change while an existing persistence stamp still reported no save required.

## Implemented fix

- Name input is validated/normalized before mutation.
- Canonical-equivalent same-name assignment remains a true no-op.
- A real name change assigns the normalized value then calls `Touch()` exactly once.
- Constructor initialization still writes `_name` directly and does not create a synthetic revision.
- Snapshot restore compatibility is preserved because `ProjectStateSnapshot.CopyInto(...)` ends by restoring the captured `UpdatedUtc`/`ChangeVersion`.
- Focused smoke pins persistence-stamp dirty tracking, canonical no-op, invalid-input atomicity and snapshot Name/version/timestamp restoration.

## Integration evidence

- Claim registration: `2fd8a0f6a0f38ee4123bd18ad8902b15cb34d392`.
- Branch source commit: `d7694c0d92cd7b0df903020e50158e2a2383d77f`.
- Focused smoke commit: `7103af60d8555fdf4927bb22d1c0e16ddcacdf21`.
- Exact branch diff was only `ProjectState.cs` (+7/-1) plus the new 71-line smoke.
- Comparison from claim registration to PR base `e9454e2566dfaabf00a6389c3f219ef46fe3f683` showed 18 intervening commits and no reserved-path overlap.
- PR `#722` squash-merged at `503edb6e2bdee487c5d45f3849fa4e5ad5582f6f`.

## Coordination

The earlier completed Project Name invariant lane remains authoritative for name validation. QSDB changeVersion canonicality owns persistence parsing; snapshot identity work remains independent.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.
