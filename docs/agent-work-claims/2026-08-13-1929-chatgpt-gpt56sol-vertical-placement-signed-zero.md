# Work claim — vertical placement signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-vertical-placement-signed-zero-20260813`
- Registered: `2026-08-13T19:29:00+07:00`
- Baseline main SHA: `0e128fe5ad1eefd46e8aaa951073a915f114d3d0`

## Confirmed defect

`ElementVerticalPlacement` validates finite bottom/top elevations but stores raw zero representations; `HostedOpeningVerticalPlacement` accepts non-negative `relativeSillM` and stores raw `-0d`; `OptionalFiniteProperty`/`ReadLevelOffset` parse and return raw `-0d`. These are already validated semantic numeric states where zero is valid, so equivalent zero values can retain non-canonical IEEE-754 sign bits.

## Reserved scope

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- `tests/QS3D.Core.SmokeTests/ElementVerticalPlacementSignedZeroSmoke.cs`
- this claim file

## Intended change

Canonicalize accepted finite zero to literal `+0d` at vertical-placement value boundaries and parsed level-offset output, preserving all finite/non-finite, positive-height, level-reference, hosting, tolerance and overflow/fail-closed behavior. Add focused bit-level smoke coverage.

## Excluded scope

No ProjectFloor mutation, persistence, UI/native BricsCAD, ModelHealth, Formula, CST/cost, geometry planner, CI/release or licensed runtime changes.

## Coordination

Exact recent searches for `vertical placement signed zero` and `ElementVerticalPlacement signed-zero` returned no competing lane before claim. Baseline source blob: `c91438254f208cf464bb0311aeb18c84fd5849d9`.

## Validation

Refresh moving `main` before writes, keep production diff normalization-only, add registered focused smoke, exact source/test readback before closeout, and never claim execution gates not actually run.
