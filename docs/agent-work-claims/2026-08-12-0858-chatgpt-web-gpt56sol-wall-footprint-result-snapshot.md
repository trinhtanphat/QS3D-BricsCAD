# Work claim — Wall footprint result defensive snapshot

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:58:00+07:00`
- Baseline main SHA observed: `057d9fd153190511322fd7339c5ea0406587b276`
- Priority: P2 — Core result ownership and immutability

## Confirmed defect

`WallFootprintResult` exposes `Polygon` as a get-only `IReadOnlyList<Point2>`, but its public constructor stores the caller-supplied collection reference directly. A caller can pass a mutable array/list, construct the result, then mutate the original collection and silently change `result.Polygon`; an array can also be cast back from the result and index-mutated.

## Reserved scope

- `src/QS3D.Core/Geometry/WallFootprintEngine.cs` — `WallFootprintResult` constructor ownership only
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-wall-footprint-result-snapshot.md`
- this claim file

## Contract

1. `WallFootprintResult` snapshots the polygon supplied to its public constructor.
2. Later mutation of the caller collection cannot alter `Polygon`.
3. Returned `Polygon` rejects structural/index mutation.
4. Polygon points and scalar metrics are otherwise unchanged.
5. `WallFootprintEngine.Build(...)` math, validation, miter/bevel, area/perimeter and numeric protections remain unchanged.

## Validation boundary

Focused deterministic Core smoke plus exact source/diff and moving-main ancestry review. No GitHub Actions dispatch and no licensed BricsCAD runtime PASS claim.
