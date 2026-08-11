# Work claim — Linear rebar layout snapshot ownership

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:06:00+07:00`
- Baseline main SHA: `9e12cb7f1145659c84ed8fac4d033c8832007a68`
- Priority: evidence-driven remote-safe Core result ownership hardening

## Reason

`LinearRebarLayout` exposes `OffsetsM` as `IReadOnlyList<double>` but its public constructor stores the caller-supplied list reference directly. A caller can therefore construct a valid layout from a mutable `List<double>` and later mutate or clear that source list, changing `OffsetsM` and `Count` after construction. This breaks the result object's snapshot semantics and can invalidate a previously planned layout without going through the planner.

## Reserved scope

Make `LinearRebarLayout` take an owned read-only snapshot of constructor offsets. Preserve all planner arithmetic, spacing/cover/bar-count rules, public property types, valid offsets and generated layout values. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs` (`LinearRebarLayout` constructor only)
- `tests/QS3D.Core.SmokeTests/LinearRebarLayoutSnapshotOwnershipSmoke.cs`
- this claim file

## Excluded scope

- No changes to linear rebar spacing, physical overlap rules, `RebarMath`, CAD generation, quantity calculation, UI, or BricsCAD V25 runtime.
- No new numeric/engineering validation beyond ownership of the supplied offsets collection.
- No GitHub Actions dispatch.

## Validation plan

- Construct a layout from a mutable list, mutate and clear the source list afterward, and assert the layout retains the original count and offset values.
- Confirm a normal planner-generated layout still exposes the expected deterministic offsets/count.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The prior linear-rebar physical-spacing claim is `COMPLETED` and addressed spacing/overlap semantics. No current/recent claim was found for `LinearRebarLayout` collection ownership/aliasing.

## Completion condition

Current `main` owns an immutable snapshot of constructor offsets, focused regression coverage is present, and this claim is marked `COMPLETED`.
