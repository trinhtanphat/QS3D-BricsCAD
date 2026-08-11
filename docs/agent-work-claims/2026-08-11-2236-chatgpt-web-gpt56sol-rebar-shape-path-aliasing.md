# Work claim — Rebar shape path point aliasing

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:36:00+07:00`
- Baseline main SHA: `87413e90b991d6f224d832af2315ca937a5bcb40`
- Priority: evidence-driven remote-safe Core invariant hardening

## Reason

`RebarShapePath` exposes `Points` as an `IReadOnlyList<RebarShapePoint>` and enforces at construction that the path contains at least two points, but the constructor stores the caller-supplied list reference directly. A caller can therefore pass a mutable `List`, construct a valid path, then mutate/clear the original list and silently change the supposedly read-only path after validation, including violating the `>= 2 points` invariant.

## Reserved scope

Snapshot constructor input into an owned read-only point collection so later caller mutations cannot alter an existing `RebarShapePath`. Preserve shape-code normalization, point values/order, builder geometry, leg/turn parsing, and public `IReadOnlyList` API. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Rebar/RebarShapePath.cs`
- `tests/QS3D.Core.SmokeTests/RebarShapePathAliasingSmoke.cs`
- this claim file

## Excluded scope

- No changes to BBS calculations, shape presets, CAD Solid3d generation, rebar ownership/replacement, placement transforms, or BricsCAD V25 runtime.
- No change to shape geometry or numeric tolerances.
- No GitHub Actions dispatch.

## Validation plan

- Construct a path from a mutable two-point list, mutate and clear the source list, and assert the constructed path retains the original two points and values.
- Confirm builder-created paths retain their existing point counts/coordinates.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent shape-rebar commits focus on native ownership/atomic replacement and are not active claims on the Core `RebarShapePath` constructor. No current claim or recent commit was found for point-list aliasing or constructor ownership.

## Completion condition

Current `main` owns an immutable snapshot of shape-path points, includes focused regression coverage, and this claim is marked `COMPLETED`.
