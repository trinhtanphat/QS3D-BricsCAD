# Work claim — Beam longitudinal layout snapshot ownership

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:16:00+07:00`
- Baseline main SHA: `fa73d76c8d76de5c53ebaa458a492d4b1716f0f0`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`BeamLongitudinalRebarLayout` exposes top and bottom bar-center collections as `IReadOnlyList<Point2>` but stores caller-provided collection references directly. Mutating those source lists after construction changes a completed layout's bar centers and `Count` without recomputing its cached layer elevations.

## Reserved scope

Make the layout constructor own read-only snapshots of top/bottom bar-center collections. Preserve all cover/layer/spacing/overlap arithmetic, public property types, elevations and planner-generated outputs. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Rebar/BeamLongitudinalRebarPlanner.cs` (`BeamLongitudinalRebarLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/BeamLongitudinalLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to beam reinforcement engineering, bar-count/spacing rules, CAD generation, quantity calculation, UI, or BricsCAD V25 runtime.
- No new numeric validation beyond collection ownership.
- No GitHub Actions dispatch.

## Validation plan

- Construct from mutable top/bottom lists, mutate and clear both source lists afterward, and assert retained centers/count.
- Confirm a normal planner-generated layout retains expected counts/elevations.
- Re-fetch current `main` and exact source blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

No current/recent claim was found for `BeamLongitudinalRebarLayout` collection ownership. Existing beam/rebar engineering and native lanes are disjoint from this constructor-only result ownership scope.

## Completion condition

Current `main` owns immutable snapshots of top/bottom bar centers, focused regression coverage is present, and this claim is marked `COMPLETED`.
