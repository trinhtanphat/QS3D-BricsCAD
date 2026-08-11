# Work claim — degenerate polyline finite validation

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:30:00+07:00`
- Baseline main SHA: `658b4c3251fbd77cd31505957db46104c2b3b5ea`
- Claim commit: `db0382255f86827cb807ee06fdfcb274affbe8f9`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect fixed

`PolylineMetrics` rejected non-finite point coordinates on normal metric paths, but its degenerate early returns bypassed those guards: `Length` returned `0` before touching coordinates when fewer than two points were supplied, and `SignedArea` returned `0` before validating coordinates when fewer than three points were supplied. A one-point length or one/two-point area containing `NaN`/`Infinity` was therefore accepted as the finite metric `0`.

Degenerate paths now validate every supplied point before returning their existing zero metric. Empty input remains a valid zero metric. Normal length/area arithmetic and the completed two-vertex closed-length behavior are unchanged.

## Implementation surfaces

- `src/QS3D.Core/Geometry/PolylineMetrics.cs`
- `tests/QS3D.Core.SmokeTests/PolylineDegenerateFiniteSmoke.cs`
- `tests/QS3D.Core.SmokeTests/PolylineDegenerateFiniteRegistration.cs`
- this claim file

## Product commits

- `7ea89d5759c662efc7fd571a76f22b49a71a93c1` — `fix(core): validate degenerate polyline coordinates`
- `46076d71b85bbfec581f0847a75c9e099169d21a` — `test(core): cover degenerate polyline finite validation`
- `061eec1781f3783688d5a5c6343e2e880d21038a` — `test(core): register degenerate polyline finite smoke`

## Regression coverage

Focused Core smoke source covers:

- empty length and signed area remain zero;
- finite one-point length remains zero;
- finite one/two-point signed area remains zero;
- NaN/infinite one-point length fails closed;
- NaN one-point and infinite two-point signed area fail closed.

Registration uses a dedicated module initializer and does not touch the shared smoke registration file.

## Coordination / validation truth

- The prior two-vertex closed-polyline claim was verified `COMPLETED` before this lane was registered.
- Exact implementation, smoke and registration diffs were re-read after push.
- An initial registration write hit a normal concurrent-head HTTP 409; current `main` was fetched, `46076d71...` was verified as an ancestor with `behind_by: 0`, and the registration was then committed on the advanced head without overwrite or force-push.
- After registration, comparison from `061eec1781f3783688d5a5c6343e2e880d21038a` to observed `main` `10f050d525d635a049b342bd4bdc5148605eb67d` again reported `behind_by: 0`; intervening files were disjoint.
- Hosted environment has no .NET SDK, so no `dotnet` run is claimed.
- No GitHub Actions were dispatched and no BricsCAD V25 runtime qualification is claimed.

## Exclusions respected

No `Point2`, wall, curtain, opening, room, bulge, tessellation, rebar, CAD adapter, UI, installer, reporting, persistence, licensing or native runtime files were changed.

## Completion condition

Satisfied for remote/source scope: non-finite coordinates are no longer silently accepted by degenerate polyline metric early returns, focused regression source is registered on `main`, concurrent work was preserved, and this claim is released as `COMPLETED`.