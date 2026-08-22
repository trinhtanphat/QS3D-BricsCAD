# Work claim — Curved opening station arithmetic overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curved-opening-station-overflow`
- Registered: `2026-08-12T09:33:00+07:00`
- Completed: `2026-08-12T09:40:00+07:00`
- Baseline main SHA: `c26a1423f1c6ccf224f00e799e26deae3a6321b7`
- Claim commit: `4e4c515288a9a8484cd4d4cafffb9b805615237f`
- Source fix commit: `cdccbb5d12c9fdd446cef91dc2704a5756ab5ad5`
- Regression commit: `4ecb11f4e32e57a870bfcaa51a00f949fc3a9201`
- Registration commit: `8f1ab947cd991cca460ef1ca4d4d6ecfd6976378`
- Priority: P1 — finite curved-opening inputs must not produce non-finite station/tolerance bounds that bypass host-span validation.

## Reserved scope

Harden `CurvedOpeningFootprintPlanner` derived station/range arithmetic so finite inputs fail closed when ambiguity thresholds, opening start/end stations, or tolerance-expanded station bounds overflow instead of allowing `Infinity` to alter comparisons/clamping and defer failure into unrelated downstream footprint geometry.

## Implemented surfaces

- `src/QS3D.Core/Geometry/CurvedOpeningFootprintPlanner.cs`
- `tests/QS3D.Core.SmokeTests/CurvedOpeningStationOverflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/CurvedOpeningStationOverflowRegistration.cs`
- this claim file

## Implemented contract

- Reused the planner's finite-checked `Add(...)` for total centerline length, ambiguity threshold, opening end station, tolerance-expanded host end, slice interior start, vertex stations and segment end stations.
- Added finite-checked `Subtract(...)` for opening start station, slice interior end and point-at-station offsets.
- Finite inputs that overflow an opening station or tolerance-expanded bound now throw `OverflowException` before comparison/clamping instead of allowing `Infinity` to bypass the host-span check and fail later in unrelated footprint geometry.
- Normal-scale station planning behavior remains unchanged in focused smoke coverage.

## Excluded scope honored

- No changes to projection direction/dot-product logic covered by the completed curved-opening projection-overflow lane.
- No `WallFootprintEngine` contract changes.
- No Door/WallOpening property-dimension policy, native boolean/materialization, V25/V26 command/UI, physical-cut ownership, or runtime qualification changes.
- No GitHub Actions dispatch.

## Validation actually performed

- Claim commit was published before substantive source writes and verified as an ancestor of the then-current `main`.
- Re-fetched `CurvedOpeningFootprintPlanner.cs` after claim publication; source remained at blob `487d80888ebdca8d73c9b1dc2f749353f941ca8f` before the guarded update.
- Concurrent commits between claim and source write were inspected and did not touch the reserved planner/test paths.
- Two attempts to publish a coherent Git-data commit were rejected by the non-force fast-forward guard because `main` advanced; neither rejected commit was moved onto `main` and no overwrite/force was used.
- Switched to exact-SHA Contents API writes: source update succeeded only against the reviewed planner blob; focused smoke and module registration were then created on `main`.
- Reviewed exact source commit diff (`23` additions / `9` deletions) and re-read the final regression and registration files from current `main`.
- Regression uses only finite values (`6.5e307`, `1.3e308`, `1.7e308`, `5.0e307`) that force derived station overflow and expects fail-closed `OverflowException`; it also verifies normal-scale station outputs remain `2`, `3`, and `5` meters.
- No local .NET compile/test execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime qualification is claimed.
- No GitHub Actions were dispatched and no force-push was used.

## Coordination

The earlier `curved opening projection overflow` claim is completed. Observed concurrent Opening/reporting/browser/interchange/audit/family/regeneration/persistence lanes were disjoint from this exact Core station-arithmetic surface.

## Completion condition

Completed. Finite-derived curved-opening station/tolerance calculations used for ambiguity/span/slicing validation now fail closed on non-finite results, focused regression source is on `main`, and exact implementation/test SHAs are recorded above.
