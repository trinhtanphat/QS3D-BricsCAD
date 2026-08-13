# Work claim — OpeningPropertySet sill-offset signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-opening-sill-offset-signed-zero-20260813`
- Registered: `2026-08-13T19:10:00+07:00`
- Baseline main SHA: `a5a4acaa70ecaf93c61c09606002ff65e76f7169`
- Priority: P0 deterministic Core opening-metric canonicality.

## Confirmed defect

`OpeningPropertySet.SillOffsetMm` is the one opening metric that legitimately accepts zero and negative values. Its setter delegates to `RequireFinite()`, which rejects NaN/infinity but returns the raw finite double. IEEE-754 `-0d` is therefore stored in the backing field and exposed unchanged by the getter.

`WidthMm`, `HeightMm`, and `ThicknessMm` delegate through the same finite helper but are subsequently required to be strictly greater than zero, so this lane does not change their acceptance contract.

## Reserved scope

- `src/QS3D.Core/Domain/OpeningPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/OpeningPropertySetSignedZeroSmoke.cs` (new focused registered smoke)
- this claim file for closeout

## Intended change

- canonicalize every accepted finite zero in the shared `RequireFinite()` helper to literal `+0d`;
- preserve NaN/infinity rejection;
- preserve strictly-positive dimension validation because `RequirePositiveFinite()` continues to reject canonical zero with `<= 0d`;
- preserve legal negative nonzero sill offsets;
- add bit-level regression for `SillOffsetMm = -0d`, plus negative-nonzero, positive dimensions and non-finite/refusal sanity cases.

## Excluded scope

- Width/Height/Thickness business constraints or defaults;
- opening host/rehost/cut geometry, Door/Opening commands, persistence/export/UI;
- QuantityMath, ElementInstance, ProjectElement and other signed-zero lanes already completed;
- the concurrent Formula evaluator signed-zero lane, which reserves only Formula source/test files;
- GitHub Actions, packaging, release and licensed BricsCAD runtime qualification.

## Coordination

- Exact recent commit searches for `OpeningPropertySet signed zero` and `SillOffset signed zero` returned no competing lane immediately before the first claim attempt.
- The first claim write was rejected with HTTP 409 because moving `main` advanced; no claim/source/test write occurred in that failed attempt.
- Refreshed HEAD showed the intervening `a5a4acaa70ecaf93c61c09606002ff65e76f7169` Formula claim is disjoint.
- The Core smoke project uses SDK default compile globs, so the new `[ModuleInitializer]` source file will be registered without csproj edits.
- Current Opening source blob before claim remained `5008a5ee699325c74add2d25c306fea642bc31af`.

## Validation plan

- refresh `main` after claim and recheck OpeningPropertySet history before source mutation;
- keep production change to shared finite zero canonicalization only;
- add the focused smoke with exact sign-bit assertion using `BitConverter.DoubleToInt64Bits`;
- re-fetch exact pushed source/test and reconcile moving-main ancestry before closeout;
- managed/native execution remains `NOT_RUN` when unavailable; do not fabricate PASS.

## Completion condition

`SillOffsetMm` stores/exposes canonical positive zero for zero-valued inputs, legal negative nonzero offsets and strict positive physical dimensions remain unchanged, focused source regression is on current `main`, and the claim closes with exact readback and truthful validation boundaries.
