# Work claim — ProjectElement category reassignment invalidation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-element-category-invalidation-20260812-0837`
- Registered: `2026-08-12T08:37:00+07:00`
- Baseline main SHA: `9b6c343a5920e3e02eda59c4c43591aa85f92dac`
- Priority: P1 — ensure public semantic category reassignment invalidates derived quantity/geometry state.

## Confirmed defect

`ProjectElement.Category` is a public reassignment setter. A real valid category change previously only assigned `_category`; it did not mark dirty flags or existing generated output stale. Because category controls geometry policy, quantity/report behavior and generated-output semantics, an element could remain `Dirty=None` with apparently clean generated geometry after category reassignment.

## Implemented fix

- A real valid category change now assigns the category then invalidates through the existing `MarkDirtyCore(ElementDirtyFlags.All, true)` primitive.
- Constructor behavior remains unchanged because construction writes the backing field directly.
- Same-category assignment remains a true no-op.
- Undefined categories still throw before mutation and preserve the previous category/dirty/stale state.
- FamilyId is not cleared or reassigned by this lane.
- Focused smoke pins All-dirty/generated-solid-stale behavior for a real change and preservation for same/invalid assignments.

## Integration evidence

- Claim registration: `c559b1f7b843d618622db2caf86c1417bd0ebc7a`.
- Branch source commit: `8176376780fba0a94b6746f7f49eec0654b1a054`.
- Branch smoke commit: `67fa8cebaacf85c75152c80667bf4de701d5af7a`.
- Exact branch diff confirmed `ProjectElement.cs` changed by only +1 line plus the new focused smoke; the full-file SHA-guarded write introduced no incidental churn.
- Comparison from claim registration to PR base `1e2f0894c2d3255ada739f8a2d6e23c13428e599` showed 28 intervening commits and no `ProjectElement.cs`/new-smoke overlap.
- PR `#656` squash-merged cleanly at `d64db03111959d5ae3084776d697ff8678100903`.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.
