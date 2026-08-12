# Work claim — Bulge Arc read-only result parity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:43:00+07:00`
- Completed: `2026-08-12T08:47:00+07:00`
- Baseline main SHA observed: `f746a372a7782d38436deb86e55ebe72e381e125`
- Priority: P2 — Core API result immutability consistency

## Confirmed defect

`BulgeArcTessellator.Tessellate(...)` advertised `IReadOnlyList<Point2>`, and the curved-arc path returned `List<Point2>.AsReadOnly()`, but the straight/near-zero-bulge fast path returned a raw `Point2[]`. Callers could therefore cast only the straight-path result back to an array and mutate it, making the public read-only contract depend on input shape.

## Implemented contract

1. Straight and curved tessellation results now expose consistent non-mutable collection semantics.
2. Straight path still returns exactly `[start, end]` in the same order.
3. Curved path values, sagitta segmentation, segment limits, overflow guards and geometric calculations are unchanged.
4. Focused Core smoke coverage checks straight value parity and proves index mutation is rejected for both straight and curved results.
5. No BricsCAD/native CAD behavior or release workflow changes.

## Integration evidence

- Claim registration: `dab99a78ee217a1b552cef4161caac191fc85557`.
- Planning: `eab639ba584fa9ab89bed85a7881d64a5b692d6b`.
- Source fix on `main`: `3db390b3d5e2c257af148f108814977d44cbb9f9`.
- Source diff was exactly one replacement: raw straight-path array -> `Array.AsReadOnly(...)` (`+1/-1`).
- Focused smoke on `main`: `8c0d7c66bec762f37e9cc2ec7c5d5a364ed99782`.
- Moving-main ancestry check at observed HEAD `2c13039902877d66f73f12a16acffc3ecae6c8ae`: source was 12 commits behind with no subsequent `BulgeArcTessellator.cs` overlap; smoke was 2 commits behind with no subsequent reserved-path overlap.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff and ancestry review. GitHub Actions were not dispatched and no executable smoke, local .NET build, or licensed BricsCAD runtime PASS is claimed from this connector-only environment.
