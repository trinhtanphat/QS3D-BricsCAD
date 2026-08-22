# Work claim — Wall footprint result defensive snapshot

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:58:00+07:00`
- Completed: `2026-08-12T09:01:00+07:00`
- Baseline main SHA observed: `057d9fd153190511322fd7339c5ea0406587b276`
- Priority: P2 — Core result ownership and immutability

## Confirmed defect

`WallFootprintResult` exposed `Polygon` as a get-only `IReadOnlyList<Point2>`, but its public constructor stored the caller-supplied collection reference directly. A caller could pass a mutable array/list, construct the result, then mutate the original collection and silently change `result.Polygon`; an array could also be cast back from the result and index-mutated.

## Implemented contract

1. `WallFootprintResult` now snapshots the polygon supplied to its public constructor into an owned read-only collection.
2. Later mutation of the caller collection cannot alter `Polygon`.
3. Returned `Polygon` rejects structural/index mutation.
4. Polygon points and scalar metrics are otherwise unchanged.
5. `WallFootprintEngine.Build(...)` math, validation, miter/bevel, area/perimeter and numeric protections are unchanged.

## Integration evidence

- Claim registration: `5bfb237e15fae4b8ccc0a34064d8b0ec041cbbab`.
- Planning: `8287081b4b92b597b8d83093e91dc50f821612c3`.
- Source fix on `main`: `f4f78858c8a4259dd841fb45b6f459bfd6a0e01b`.
- Exact source diff: one constructor assignment changed from retaining `polygon` to `new List<Point2>(polygon).AsReadOnly()` (`+1/-1`).
- Focused smoke on `main`: `b9722740f78442002f9e10450fc29916de3d69ae`.
- Moving-main ancestry at observed HEAD `2efdca5b638396f70246752cb332656e83c16445`: source was 6 commits behind and no later `WallFootprintEngine.cs` overlap appeared; smoke was 3 commits behind with only unrelated health/family/hardening changes after it.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff and ancestry review. GitHub Actions were not dispatched and no executable smoke, local .NET build, or licensed BricsCAD runtime PASS is claimed from this connector-only environment.
