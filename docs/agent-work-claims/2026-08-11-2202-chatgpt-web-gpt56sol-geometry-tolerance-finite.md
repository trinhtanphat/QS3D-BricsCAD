# Work claim — finite geometry tolerance policy

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:02:00+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Priority: evidence-driven remote-safe Core regression hardening

## Confirmed defect

`GeometryTolerancePolicy` validates tolerance magnitudes only with relational comparisons. IEEE-754 `NaN` makes those comparisons false and therefore passes the constructor; positive infinity also passes the positive/order checks. This admits invalid policy states. For example, an infinite boundary-gap tolerance accepts every finite gap as auto-closable, an infinite tiny-segment tolerance classifies every finite length as tiny, and a NaN point tolerance makes ordinary `NearlyEqual` checks fail unpredictably from configuration rather than geometry.

## Reserved scope

Harden construction of `GeometryTolerancePolicy` so all three configured tolerances are finite in addition to their existing positivity/order constraints. Preserve predicate behavior for ordinary finite inputs and do not redesign geometry algorithms.

## Expected surfaces

- `src/QS3D.Core/Services/GeometryTolerancePolicy.cs`
- `tests/QS3D.Core.SmokeTests/GeometryToleranceFiniteSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GeometryToleranceFiniteRegistration.cs`
- this claim file

## Excluded scope

- No `Point2` changes; the active invariant-formatting claim owns that file.
- No Room boundary engine, bulge tessellation, polygon topology, wall junction, rebar planner, Unit policy, reporting, persistence, updater, UI, installer, licensing, or BricsCAD V25 adapter/runtime changes.
- No change to default tolerance values or finite comparison semantics.
- No GitHub Actions dispatch.

## Validation plan

- Add deterministic Core smoke coverage proving `NaN` and positive infinity are rejected for point, boundary-gap and tiny-segment tolerances.
- Preserve default finite `NearlyEqual`, `CanAutoClose` and `IsTiny` behavior.
- Use a dedicated module-initializer registration to avoid the highly contended shared smoke registration file.
- Re-fetch current `main` and target blobs before every write; on stale writes re-evaluate rather than overwrite or force-push.
- Record source/static verification truthfully; do not claim a `dotnet` execution unless it is actually run.

## Coordination

Current active claims reviewed around this scope reserve Point2 formatting, reporting normalization, Core persistence/session atomicity, quantity settings/matrix diagnostics, updater, installer, licensing, browser workspace and UI work. This lane is limited to the standalone Services tolerance-policy constructor and new dedicated smoke files.

## Completion condition

Finite validation and focused regression coverage are present on current `main`, exact diffs are re-read, claim status is closed with resulting SHAs, and no concurrent work is overwritten.