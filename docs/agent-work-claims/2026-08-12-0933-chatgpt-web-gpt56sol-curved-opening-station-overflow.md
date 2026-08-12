# Work claim — Curved opening station arithmetic overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curved-opening-station-overflow`
- Registered: `2026-08-12T09:33:00+07:00`
- Baseline main SHA: `c26a1423f1c6ccf224f00e799e26deae3a6321b7`
- Priority: P1 — finite curved-opening inputs must not produce non-finite station/tolerance bounds that bypass host-span validation.

## Reserved scope

Harden `CurvedOpeningFootprintPlanner` derived station/range arithmetic so finite inputs fail closed when ambiguity thresholds, opening start/end stations, or tolerance-expanded station bounds overflow instead of allowing `Infinity` to alter comparisons/clamping and defer failure into unrelated downstream footprint geometry.

## Expected surfaces

- `src/QS3D.Core/Geometry/CurvedOpeningFootprintPlanner.cs`
- `tests/QS3D.Core.SmokeTests/CurvedOpeningStationOverflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/CurvedOpeningStationOverflowRegistration.cs`
- this claim file

## Excluded scope

- No changes to projection direction/dot-product logic covered by the completed curved-opening projection-overflow lane.
- No `WallFootprintEngine` contract changes.
- No Door/WallOpening property-dimension policy, native boolean/materialization, V25/V26 command/UI, physical-cut ownership, or runtime qualification changes.
- No GitHub Actions dispatch.

## Validation plan

- Verify this claim is reachable from current `main`, then re-fetch the exact planner blob before implementation.
- Replace raw derived station/threshold arithmetic with finite-checked helpers while preserving normal-range behavior and exception ordering.
- Add focused module-initializer smoke coverage using only finite inputs that currently make both opening end station and tolerance-expanded host end overflow, proving the planner now rejects at station arithmetic rather than proceeding into downstream geometry.
- Review exact pushed diff and read back final source/tests from current `main`.
- Close this claim with exact commit SHAs and ancestry verification; do not claim local compile/BricsCAD runtime PASS unless actually executed.

## Coordination

The earlier `curved opening projection overflow` claim is completed. Current observed claims for Opening positive dimensions, reporting reference IDs, browser/interchange/audit/family lanes are disjoint from this exact Core station-arithmetic surface. If a new overlapping claim lands after registration, stop and reconcile before source writes.

## Completion condition

All finite-derived curved-opening station/tolerance calculations used for ambiguity/span validation fail closed on non-finite results, focused regression source is pushed to `main`, and this claim is marked `COMPLETED` with truthful validation notes.
