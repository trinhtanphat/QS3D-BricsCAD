# Work claim — Rebar shape path point aliasing

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:36:00+07:00`
- Baseline main SHA: `87413e90b991d6f224d832af2315ca937a5bcb40`
- Priority: evidence-driven remote-safe Core invariant hardening

## Reason

`RebarShapePath` exposed `Points` as an `IReadOnlyList<RebarShapePoint>` and enforced at construction that the path contained at least two points, but the constructor stored the caller-supplied list reference directly. A caller could therefore pass a mutable `List`, construct a valid path, then mutate/clear the original list and silently change the supposedly read-only path after validation, including violating the `>= 2 points` invariant.

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

## Completion

- Implementation commits:
  - `a9adbb248a3eb032971c0dfc065ab1c776537063` — copy caller-provided shape points into an owned list, validate the owned snapshot, and expose it read-only.
  - `a666eb92eb8ef288395de9f35858d60648ec8123` — add aliasing regression coverage plus the existing L-shape builder geometry check.
- Final observed `main` before claim close: `2839e2d5233e1142a3bcb7d2fa79a52b4dcec4bd`.
- Validation actually performed:
  - re-fetched the constructor from current `main` and confirmed validation applies to the copied snapshot rather than the caller collection;
  - re-fetched the new smoke and confirmed it mutates then clears the source `List` while requiring the path to retain the original two points;
  - confirmed builder-created L-shape coordinates remain covered within the existing numeric tolerance;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core value-object ownership hardening.

## Completion condition

Satisfied: current `main` owns an immutable snapshot of shape-path points, includes focused regression coverage, and this claim is released as `COMPLETED`.
