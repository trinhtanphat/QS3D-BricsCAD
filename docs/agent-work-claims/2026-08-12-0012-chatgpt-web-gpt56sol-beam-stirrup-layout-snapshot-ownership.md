# Work claim — Beam stirrup layout snapshot ownership

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:12:00+07:00`
- Baseline main SHA: `8f6ec49c30f391c77aa2ca32a7184559d180136c`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`BeamStirrupLayout` exposes `StationOffsetsM` and `SectionLoop` as read-only lists but its constructor chain ultimately stores the caller-supplied references directly. A caller can mutate the source collections after construction, changing `Count` or the section path of an already-computed layout while cached spacing/length values remain unchanged.

## Reserved scope

Make the shared `BeamStirrupLayout` constructor own read-only snapshots of station offsets and section path. Preserve all station/cover/spacing/bend/hook/tessellation arithmetic, cached length values, public property types and planner-generated outputs. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Rebar/BeamStirrupLayoutPlanner.cs` (`BeamStirrupLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/BeamStirrupLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to beam stirrup engineering/spacing/hook/bend rules, tessellation, CAD generation, quantity calculation, UI, or BricsCAD V25 runtime.
- No new numeric validation beyond collection ownership.
- No GitHub Actions dispatch.

## Validation plan

- Construct a legacy layout from mutable station/path lists, mutate and clear both source lists afterward, and assert retained station/path counts and values.
- Confirm a normal planner-generated legacy stirrup layout remains deterministic.
- Re-fetch current `main` and exact source blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

No current/recent claim was found for `BeamStirrupLayout` collection ownership. Other beam-stirrup work concerns engineering/CAD behavior and is disjoint from this constructor-only result ownership lane.

## Completion condition

Current `main` owns immutable snapshots of constructor stations/section path, focused regression coverage is present, and this claim is marked `COMPLETED`.
