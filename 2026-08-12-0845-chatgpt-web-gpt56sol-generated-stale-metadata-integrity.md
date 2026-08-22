# Work claim — generated stale metadata integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-generated-stale-metadata-integrity-20260812-0845`
- Registered: `2026-08-12T08:45:00+07:00`
- Baseline main SHA: `92fe422a809309bd818fb6be68baa90dfd1f53cd`
- Priority: P1 — make malformed persisted stale-state fail visible without changing output-generation freshness semantics.

## Confirmed defect

Normal `ProjectElement.MarkGenerated*Stale(...)` writes a stale state marker and matching stale snapshot together. `IsGenerated*Stale()` intentionally requires the snapshot to match the current output so stale metadata from an old/rebuilt output does not carry forward. Persisted/directly-mutated `State=stale` with a missing/blank snapshot was therefore an impossible writer state that all ordinary stale helpers treated as false, making malformed metadata appear clean.

## Implemented fix

- `GeneratedGeometryStaleHealthService` now checks the ten known generated state/snapshot pairs.
- A known state equal to `stale` with absent/blank paired snapshot emits Error-level `GENERATED_STALE_METADATA_INVALID` for the element.
- `ProjectElement.IsGenerated*Stale()` snapshot-match semantics remain unchanged.
- A nonblank old snapshot after output rebuild is not classified as corrupt or stale by this lane.
- A matching snapshot still produces the ordinary generated-stale warning.

## Integration evidence

- Claim registration: `5c446a69dcdb92faf472a5616d288d0360b0980c`.
- Branch source commit: `b9e7ad525115c324d8d2dad5e4fe22e0f50df2a0`.
- Branch smoke commit: `65fd5efd4019bcc00343eafd0efbb4366d3a134c`.
- Branch diff was exactly the reserved stale-health source plus new focused smoke (+32 source lines).
- Comparison from claim registration to PR base `2bb84c11b0e20586be0509af603c5da930a52695` showed 16 intervening commits and no reserved-path overlap.
- PR `#661` squash-merged cleanly at `98469144f23aa55c3a3b715316247138ea73fad2`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.
