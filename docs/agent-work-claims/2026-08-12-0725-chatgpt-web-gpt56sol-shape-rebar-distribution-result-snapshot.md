# Work claim — Shape rebar distribution result snapshot

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:25:00+07:00`
- Baseline main SHA: `8ed328fe918d57af8eeb5e10353f9cc6414e52ae`
- Priority: evidence-driven remote-safe rebar result integrity

## Reason

`ShapeRebarDistributionResult` is a public result type but stores the caller-supplied `IReadOnlyList<double>` reference directly. An `IReadOnlyList<T>` can still be backed by a mutable `List<T>`, so code can construct a result, then mutate or clear the source list and silently change the result's offsets after construction. Other rebar layout result types materialize owned snapshots, and a planning result must remain stable after it is returned.

## Reserved scope

Materialize an owned read-only snapshot of distribution offsets in `ShapeRebarDistributionResult`. Preserve planner math, ordering, centered/non-centered semantics, center clearance, public API types and all native integration behavior. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Rebar/ShapeRebarDistributionPlanner.cs` (`ShapeRebarDistributionResult` constructor only)
- `tests/QS3D.Core.SmokeTests/ShapeRebarDistributionResultSnapshotSmoke.cs`
- this claim file

## Excluded scope

- No changes to spacing/count/cover/radius formulas or planner limits.
- No validation-policy expansion beyond breaking the mutable collection alias.
- No CAD/native/UI behavior changes and no BricsCAD runtime claim.
- No GitHub Actions dispatch.

## Validation plan

- Construct a result from a mutable `List<double>`, mutate/clear the source list afterward, and assert the result retains the original offsets/order.
- Assert `ShapeRebarDistributionPlanner.Plan()` still returns the expected centered offsets for a normal multi-bar case.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The latest shape-rebar distribution commits are older planner/native-placement hardening; no current/recent claim was found for result collection aliasing.

## Completion condition

Current `main` returns stable shape-rebar distribution offsets independent of caller collection mutation, focused regression coverage is present, and this claim is marked `COMPLETED`.
