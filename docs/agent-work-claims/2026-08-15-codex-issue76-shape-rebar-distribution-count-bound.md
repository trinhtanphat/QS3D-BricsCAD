# Work claim — Shape Rebar distribution count bound

- Status: `COMPLETED`
- Agent: `codex/issue76-rebar-next-gap`
- Registered: `2026-08-15T09:50:38+07:00`
- Baseline main SHA: `017f96803b373955adda72239ad0b6b86cb9ca1b`
- Task branch: `codex/issue76-shape-distribution-count-claim-20260815`
- Related issue: `#76`

## Confirmed defect

`ShapeRebarDistributionPlanner.Plan(...)` rejects non-positive `Count` values but accepts every positive `Int32` value and then allocates `new double[input.Count]`. A finite otherwise-valid input with `Count = int.MaxValue` can therefore attempt a multi-gigabyte allocation instead of failing with a deterministic domain error. The neighboring linear and rectangular Rebar planners already cap generated bar counts at 10,000.

## Reserved contract

- Cap Shape Rebar distribution input at 10,000 bars and reject larger values before allocating the offset array.
- Preserve the current `ArgumentOutOfRangeException` family for non-positive counts and use it for over-bound counts.
- Preserve every accepted layout's center-clearance, offset ordering, centered/non-centered behavior, cover/radius math and public result API.
- Keep the change fabrication-standard neutral; it is a resource-safety boundary, not a structural-design or code-compliance rule.

## Exact scope

- `src/QS3D.Core/Rebar/ShapeRebarDistributionPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ShapeRebarDistributionCountBoundSmoke.cs`
- `scripts/preflight-shape-rebar-distribution-count-bound.py`
- this claim file for implementation evidence and closeout

## Explicit exclusions

- No native BricsCAD adapter, geometry builder, UI, BBS/export, generated-health, ownership, notation, fabrication qualification or other Rebar planner changes.
- No governing-standard inference, engineering approval, LOCAL qualification, private data, GitHub Actions, package, release or workflow changes.
- Issue `#76` remains open for the broader fabrication/engineering/native scope.

## Validation plan

- focused Shape Rebar count-bound preflight;
- Core Release build and complete Core smoke harness;
- aggregate repository preflight and diff checks;
- fresh-main collision/readback checks before implementation integration.

## Completion condition

The claim is visible on `main` before implementation starts; the bounded source, deterministic regression and focused gate merge normally; latest-main validation passes; then this narrow claim is marked `COMPLETED` with exact SHAs while issue `#76` stays open.

## Completion evidence

- Claim PR `#1517` merged as `d77139c56ab6abc5c999f9e440b02b617d06e105` before implementation.
- Source commit `39f3fe7a8249dfbd84541bafa34f7014dba72b52` merged through PR `#1531` as `763346cab1c0d15e7dc67f27e44abf8d3083fee2`.
- Exact descendant validation at `309dff81ed3d7f18b9b32ac60cbd44167a40fa8f`: focused count-bound gate PASS; Core/Smoke Release build 0 warnings/0 errors; full Core smoke `ALL PASS`.
- Aggregate discovered 813 gates: every #76/#81/#84 gate passed; the only remaining failure was the unrelated release-sync gate requiring an old release-helper token. No release, workflow, Actions, native runtime, or LOCAL surface was changed.
- Broad issue `#76` remains open.
