# Work claim — Regenerator catalog read-only result

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:09:00+07:00`
- Completed: `2026-08-12T09:18:00+07:00`
- Baseline main SHA observed: `b2aab53e286878438348b831cb32c0fb9ab128f4`
- Priority: P2 — Core API result immutability

## Confirmed defect

`RegeneratorCatalog.CreateDefault()` advertised `IReadOnlyList<IElementRegenerator>` but returned a raw `IElementRegenerator[]`. Callers could cast the result back to an array and replace entries, so the public read-only collection contract was structurally mutable.

## Completed scope

- Source fix: `d6aea8c457aed5e88a413b5729d9b520924152c5`
- Focused Core smoke: `29174bcc4e2d4234e6cdf18bf989d7f710b3e7ab`
- Plan: `docs/plans/2026-08-12-regenerator-catalog-readonly.md`

## Result

1. `CreateDefault()` now returns a read-only wrapper over the same five default regenerator implementations.
2. Ordering remains Opening, Wall, Structural, Room, GenericTakeoff.
3. Focused smoke rejects raw-array exposure and verifies index replacement throws `NotSupportedException`.
4. Moving-main ancestry was checked through `3a67566bed6dddaab9ce43bce59fb8c4a1f92722`; both source and smoke commits remain ancestors and concurrent commits did not touch `RegenerationEngine.cs` after the source fix.
5. Regeneration execution, dependency ordering, quantity rules and rollback behavior were not changed.

## Validation boundary

Source/diff/ancestry and focused smoke source were verified remotely. GitHub Actions were not dispatched, and no licensed BricsCAD V25/V26 runtime PASS is claimed.
