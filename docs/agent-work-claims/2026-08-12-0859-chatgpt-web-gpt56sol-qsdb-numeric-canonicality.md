# Work claim — QSDB numeric canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-numeric-canonicality`
- Registered: `2026-08-12T08:59:00+07:00`
- Baseline main SHA: `351478288c27f025a62afdf04960b49e2ee3c129`
- Priority: P1 persistence canonicality / deterministic round-trip integrity found during owner-requested `continue all` audit.

## Confirmed defect

`QsdbProjectStore.Serialize(...)` emits persisted floor elevations and element quantities through `F(double)`, which has used invariant round-trip `ToString("R", CultureInfo.InvariantCulture)` since the repository's initial QSDB persistence implementation (`95c39e51b550b740f9df1bd77b219bcc5406998c`). The current `Double(...)` loader, however, accepts any finite token parseable with `NumberStyles.Float`, so semantically equivalent noncanonical tokens such as `1.0`, `1e0`, `+1`, or `-0` can be loaded and then silently rewritten on the next save.

Current schema migration requires the numeric attributes to be present but does not canonicalize them. Because historical QSDB writers already emitted the same `"R"` representation, rejecting noncanonical numeric tokens does not invalidate QSDB files produced by supported repository serializers.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- one focused Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current store and claim before writes.
2. Preserve invariant finite parsing, then require the original persisted numeric token to exactly match `F(parsedValue)` using ordinal comparison.
3. Apply the same helper to floor `elevationM` and element quantity `value`, without changing schema migration/defaults or in-memory numeric semantics.
4. Add focused smoke coverage that saves a canonical QSDB, confirms canonical round-trip, mutates floor/quantity numeric tokens independently to equivalent noncanonical representations, and requires `Load()` to fail closed.
5. Read back source/test on current `main`; do not dispatch GitHub Actions and do not claim BricsCAD runtime PASS.
6. Close the claim only after source/regression commits remain visible on current `main`.

## Excluded

- No schema-version bump or migration rewrite.
- No timestamp, change-version, dirty-flag, category, map/list or relation canonicality changes.
- No BricsCAD adapter/UI changes.
- No installer/signing/release changes.
