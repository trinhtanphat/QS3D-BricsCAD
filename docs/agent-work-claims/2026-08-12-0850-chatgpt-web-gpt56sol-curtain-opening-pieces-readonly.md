# Work claim — Curtain opening pieces read-only result parity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:50:00+07:00`
- Baseline main SHA observed: `953bc91e46bfbcbb2e089080e1d647f6529c74ac`
- Priority: P2 — Core planner result structural immutability

## Confirmed defect

`CurtainWallOpeningFramePlan.Pieces` and `CurtainWallOpeningPanelPlan.Pieces` are exposed as `IReadOnlyList<...>`, but both planners populate those properties with `.ToArray()`. Callers can cast returned `Pieces` back to the concrete array and replace elements, so the advertised read-only collection boundary is structurally mutable.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallOpeningFramePlanner.cs` result `Pieces` materialization only
- `src/QS3D.Core/Geometry/CurtainWallOpeningPanelPlanner.cs` result `Pieces` materialization only
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- `docs/plans/2026-08-12-curtain-opening-pieces-readonly.md`
- this claim file

## Contract

1. Frame and panel planner `Pieces` results reject structural/index mutation.
2. Piece ordering, values, counts, areas and interruption semantics stay unchanged.
3. Piece DTO property mutability is unchanged; this lane only protects the returned collection structure.
4. Input limits, opening subtraction math and native CAD integration are unchanged.

## Validation boundary

Focused deterministic Core smoke plus exact source/diff and moving-main ancestry review. No GitHub Actions dispatch and no licensed BricsCAD runtime PASS claim.
