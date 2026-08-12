# Work claim — Shape rebar distribution result snapshot

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:25:00+07:00`
- Completed: `2026-08-12T07:28:00+07:00`
- Baseline main SHA: `8ed328fe918d57af8eeb5e10353f9cc6414e52ae`
- Priority: evidence-driven remote-safe rebar result integrity

## Reason

`ShapeRebarDistributionResult` was a public result type that stored the caller-supplied `IReadOnlyList<double>` reference directly. An `IReadOnlyList<T>` can still be backed by a mutable `List<T>`, so code could construct a result, then mutate or clear the source list and silently change the result's offsets after construction. Other rebar layout result types materialize owned snapshots, and a planning result must remain stable after it is returned.

## Reserved scope

Materialize an owned read-only snapshot of distribution offsets in `ShapeRebarDistributionResult`. Preserve planner math, ordering, centered/non-centered semantics, center clearance, public API types and all native integration behavior. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Rebar/ShapeRebarDistributionPlanner.cs` (`ShapeRebarDistributionResult` constructor only)
- `tests/QS3D.Core.SmokeTests/ShapeRebarDistributionResultSnapshotSmoke.cs`
- this claim file

## Excluded scope

- No changes to spacing/count/cover/radius formulas or planner limits.
- No validation-policy expansion beyond breaking the mutable collection alias.
- No CAD/native/UI behavior changes and no BricsCAD runtime claim.
- No GitHub Actions dispatch.

## Completion

- Claim commit: `a81321c068b806e32f59881e819d5e60e348be49`.
- Implementation commit: `9ced50f8b8889687b9e8061d164521c79af59b59` — copy caller offsets into an owned `List<double>` and expose it read-only.
- Regression commit: `75ad24258727b63527dbeb76fcaa4da79af3c604` — mutate/clear the source list after result construction and verify the result stays stable; also preserve a normal centered 3-bar planner case.
- Validation actually performed:
  - re-fetched current `ShapeRebarDistributionResult` and confirmed the constructor materializes an owned read-only list;
  - re-fetched the dedicated smoke and confirmed alias mutation plus expected centered offsets are covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

The latest shape-rebar distribution commits were older planner/native-placement hardening; no current/recent claim was found for result collection aliasing.

## Completion condition

Satisfied: current `main` returns stable shape-rebar distribution offsets independent of caller collection mutation, focused regression coverage is present, and this claim is released as `COMPLETED`.
