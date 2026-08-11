# Work claim — finite geometry tolerance policy

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:02:00+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Claim commit: `5de2019e5e8b97f60b67b38c8e38bd1d8ae218b7`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect fixed

`GeometryTolerancePolicy` previously validated tolerance magnitudes only with relational comparisons. IEEE-754 `NaN` made those comparisons false and therefore passed the constructor; positive infinity also passed the positive/order checks. This admitted invalid policy states. An infinite boundary-gap tolerance could accept every finite gap as auto-closable, an infinite tiny-segment tolerance could classify every finite length as tiny, and a NaN point tolerance made ordinary `NearlyEqual` checks fail from invalid configuration rather than geometry.

The constructor now requires each configured tolerance to be finite and positive before applying the existing boundary-gap ordering rule. Default values and finite predicate semantics are unchanged.

## Implementation surfaces

- `src/QS3D.Core/Services/GeometryTolerancePolicy.cs`
- `tests/QS3D.Core.SmokeTests/GeometryToleranceFiniteSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GeometryToleranceFiniteRegistration.cs`
- this claim file

## Product commits

- `25fe5508dc49089fd29112c4fa4e998def3d6444` — `fix(core): reject non-finite geometry tolerances`
- `7e9a6dea6a30f33a0d2da2d94c593a6dd2254e49` — `test(core): cover finite geometry tolerance policy`
- `213ae0658d16942940c5f7539bda681b12116c53` — `test(core): register geometry tolerance finite smoke`

## Regression coverage

The dedicated Core smoke covers:

- NaN point tolerance rejection;
- positive-infinity point tolerance rejection;
- NaN/infinite boundary-gap tolerance rejection;
- NaN/infinite tiny-segment tolerance rejection;
- preservation of default finite `NearlyEqual`, `CanAutoClose` and `IsTiny` behavior.

A dedicated module initializer registers the smoke without editing the shared `SmokeTestRegistration.cs` surface.

## Coordination / exclusions respected

- No `Point2` changes; its active invariant-formatting lane remains untouched.
- No Room boundary, bulge, polygon, wall junction, rebar, Unit policy, reporting, persistence, updater, UI, installer, licensing or BricsCAD V25 surfaces were edited.
- No GitHub Actions were dispatched.

## Validation truth

The exact implementation and smoke commit diffs were re-read after push. Comparison from `213ae0658d16942940c5f7539bda681b12116c53` to then-current `main` `fead64e5acac427a05378b8ddcd37d874d5a1e01` reported `behind_by: 0` with the smoke-registration commit as merge base; intervening commits touched disjoint files, so the lane remained intact on `main`.

The hosted environment does not have the .NET SDK (`dotnet: command not found`), so the smoke suite was not executed locally in this session. No CI/Actions or BricsCAD V25 runtime PASS is claimed.

## Completion condition

Satisfied for remote/source scope: invalid non-finite geometry tolerance configurations now fail closed, focused regression coverage is registered on current `main`, concurrent work was not overwritten, and runtime/local qualification claims remain unchanged.