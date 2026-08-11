# Work claim — Column tie layout snapshot ownership

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:09:00+07:00`
- Baseline main SHA: `1e4b6e44a4987835e2bc75abbf6de9092381886d`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`ColumnTieLayout` exposes `ClosedPath` and `ElevationsM` as `IReadOnlyList` values but its public constructor stores both caller-supplied collection references directly. A caller can therefore mutate or clear the original lists after construction and silently change a completed layout's path/elevation set.

## Reserved scope

Make `ColumnTieLayout` own read-only snapshots of both constructor collections. Preserve planner arithmetic, spacing/cover/axial-overlap rules, perimeter/spacing values, public property types and all planner-generated output. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Rebar/ColumnTieLayoutPlanner.cs` (`ColumnTieLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/ColumnTieLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to Column Tie CAD generation, quantity calculation, axial spacing policy, `RebarMath`, UI, or BricsCAD V25 runtime.
- No new engineering/numeric validation beyond collection ownership.
- No GitHub Actions dispatch.

## Validation plan

- Construct from mutable path/elevation lists, mutate and clear those lists after construction, and assert the layout retains original values/counts.
- Confirm a normal planner-generated layout remains deterministic.
- Re-fetch current `main` and exact source blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent Column Tie work covers axial overlap or native generated-rebar revision behavior. Those lanes are completed/disjoint; no current/recent claim was found for `ColumnTieLayout` collection ownership.

## Completion condition

Current `main` owns immutable snapshots of constructor path/elevations, focused regression coverage is present, and this claim is marked `COMPLETED`.
