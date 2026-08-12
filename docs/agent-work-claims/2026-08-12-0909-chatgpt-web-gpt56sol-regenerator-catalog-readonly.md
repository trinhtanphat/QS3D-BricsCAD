# Work claim — Regenerator catalog read-only result

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:09:00+07:00`
- Baseline main SHA observed: `b2aab53e286878438348b831cb32c0fb9ab128f4`
- Priority: P2 — Core API result immutability

## Confirmed defect

`RegeneratorCatalog.CreateDefault()` advertises `IReadOnlyList<IElementRegenerator>` but returns a raw `IElementRegenerator[]`. Callers can cast the result back to an array and replace entries, so the public read-only collection contract is structurally mutable.

## Reserved scope

- `src/QS3D.Core/Services/RegenerationEngine.cs` — `RegeneratorCatalog.CreateDefault()` result materialization only
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-regenerator-catalog-readonly.md`
- this claim file

## Contract

1. The default catalog preserves the same five regenerator instances/types and ordering.
2. The returned collection rejects structural/index replacement.
3. Regeneration execution, dirty ordering, quantity rules and transactional rollback semantics are unchanged.
4. No BricsCAD/native CAD behavior or release workflow changes.

## Validation boundary

Focused deterministic Core smoke plus exact source diff and moving-main ancestry review. No GitHub Actions dispatch and no licensed BricsCAD runtime PASS claim.
